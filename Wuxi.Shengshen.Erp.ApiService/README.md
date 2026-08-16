# Wuxi.Shengshen.Erp.ApiService

无锡燊晟 ERP 后端服务（Minimal API）。**只写业务代码**；基础设施全部来自 `KingV.Core`（参照其 README）。

## 模块模板（四层）

每个业务模块按四层落地，**参照实现 = 币别 `CurrencyManagement`**（对应 Java `CurrencyManagementWebController`）：

```
Domain/{模块}/Xxx.cs            实体，继承 KingV.Core.Data.DomainEntity（只写业务字段；
                                审计/禁用/逻辑删除/雪花ID 全在基类）；
                                老库表缺 create_by/update_by/tenant_id，类上标
                                [AuditIgnore(AuditFields.CreateBy | AuditFields.UpdateBy | AuditFields.TenantId)]
Repository/Interfaces/          仓储接口（IXxxRepository）
Repository/Impl/                仓储实现，继承 RepositoryBase<Xxx>，TableName 指定表名（snake_case）；
                                查询用 Query() 链式构造（自动带 is_delete = 0）
Service/Interfaces/             服务接口（IXxxService）
Service/Impl/                   服务实现：存在性检查（throw XxxErrorMessages.NotFound.NotFound()）、
                                enable ↔ is_disable 取反等规则；实体 → 响应映射走 Facet（ToFacet/SelectFacets）
Endpoint/XxxEndpoint.cs         静态类 + MapXxxEndpoint(this RouteGroupBuilder)，
                                路由/HTTP 方法与 Java controller 完全一致
Data/Requests/{模块}/           请求 class（DataAnnotations 校验；写操作标 [Facet] 反向映射）
Data/Responses/{模块}/          响应 class（partial + [Facet] 声明式映射；需表头的字段标 [TableHeader]）；
                                同目录 XxxMappers.cs 放 IFacetMapConfiguration 补充映射（Enable 取反等）
Constants/{模块}/               XxxErrorMessages 错误消息常量（throw 处只许引用常量，禁硬编码）
```

**接口与实现必须分离**：接口放 `Interfaces/` 子目录、实现放 `Impl/` 子目录，一个文件一个类型（命名空间随目录，如 `...Repository.Interfaces` / `...Repository.Impl`）。禁止接口与实现同文件。

### DTO 规范

- **Create**：`CreateXxxRequest : partial class`（**禁止 record**——无主构造的 record 会被 Facet 生成为 positional record，继承/手工声明成员时主构造参数 unread，反序列化与映射全丢值且不报错），字段级 DataAnnotations；标 `[Facet(typeof(Xxx), nameof(实体非入参字段)..., GenerateToSource = true, ToSourceConfiguration = typeof(CreateXxxToSourceMapper))]`（排除字段走构造函数位置参数——6.6.8 的 Exclude 属性只读，不能当命名参数用）；手工声明的入参字段标 `[MapFrom(nameof(Xxx.字段), Reversible = true)]`（声明为"用户自声明成员"避免与生成属性撞名；`Reversible = true` 才进反向映射），校验特性挂在手工声明上；同目录 `XxxRequestMappers.cs` 放反向映射补充（实现 `IFacetToSourceConfiguration<TRequest, TEntity>`——接口在 Facet.Mapping 包、`Facet.Mapping` 命名空间——做 Enable 取反等）；Service 用生成的 `request.ToSource()`。
- **Edit**：`EditXxxRequest : CreateXxxRequest, IIdRequest`，`Id` 上标 `[Required]`（DataAnnotations 不读接口特性）；同样标 `[Facet]`（Id 在排除列表、不参与映射），Service 加载实体后用生成的 `request.ApplyToSource(entity)` 覆盖（只写可逆成员 + 调用配置，Id 与审计字段保留）；**禁用 Facet.Extensions 的 `ApplyFacet`**（运行时反射按名匹配，不走取反配置）。
- **响应基类链**（镜像实体侧 `AuditableEntity → DomainEntity`）：`IdResponse`（Id）→ `BaseResponse`（+审计字段，CreateTime 自带"创建时间"表头列，其余审计字段只回数据）→ `EnableResponse`（+Enable，由 is_disable 取反，自带"是否启用"表头列）。详情与列表行继承 `EnableResponse`、下拉项继承 `IdResponse`——禁止在响应类里重复声明 Id/Enable/审计字段。
- **实体→响应映射走 Facet**：响应 DTO 声明 `partial class`（禁止 record，原因同上）并标 `[Facet(typeof(Xxx), nameof(Xxx.IsDisable), nameof(Xxx.IsDelete), Configuration = typeof(XxxMapper))]`（KingV.Core 持有的源生成器，编译期生成，禁止手工逐字段 `new Dto { ... }`）；需要 `[TableHeader]` 的字段在 DTO 里手工声明时**必须同时标 `[MapFrom(nameof(Xxx.字段))]`**（声明为"用户自声明成员"，Facet 只生成映射代码、不重复生成属性，否则撞名 CS0102）；Enable 取反等定制逻辑写在 `Data/Responses/{模块}/XxxMappers.cs` 的 `IFacetMapConfiguration` 实现里（接口在 `Facet.Mapping` 命名空间）；Service 侧单条 `entity.ToFacet<S, D>()`、集合 `records.SelectFacets<S, D>()`（`using Facet.Extensions`）。
- **列表查询**：`GetXxxListRequest : PageRequest`（自带 current/size/orderField/orderSort）。
- **字符串字段**一律 `[StringLength(n, ErrorMessage = "...不能超过{0}")]`：n 对齐 DB 列宽（Java actable VARCHAR 未写长度默认 255）；数值用 `{0}` 占位符，禁止硬编码；模糊搜索字段同样要加。
- 校验消息与 Java 端文案保持一致（如 "币种名称不能为空"）。

### 端点规范

- 全部挂 `/api` 前缀（Program.cs 的 `api` 分组，已启用 ApiResponse 信封过滤器）。
- 端点首行 `RequestValidator.Validate(request)`（Minimal API 不自动跑 DataAnnotations）。
- 匿名端点 `.WithMetadata(new AllowAnonymousAttribute())`；业务端点默认要求登录，不做额外标记。
- **无数据成功响应**统一 `Results.Ok<object?>(null)`（参照 `CurrencyManagementEndpoint.EmptyOk()`）；禁止 `Results.Ok()`——无参版不实现 `IValueHttpResult`，信封过滤器会放行，前端拿不到 `{status:"200"}`。
- 查询参数用 `[FromQuery]`，body 用 `[FromBody]`；路由名、参数名与 Java 端一致（前端零改动）。

### 仓储规范

- 排序：`orderField` 必须经模块内 `HashSet<string>` 白名单校验，非法值回落默认排序（参照币别 `SortableColumns`）。
- 模糊匹配用 `q.WhereLike("col", $"%{kw}%")`；等值用 `q.Where("col", value)`；不要写 `WhereRaw` 拼接。
- 逻辑删除走基类 `LogicDeleteAsync`；物理删除禁止。
- `size = -1` 语义为"取全部"（对齐 Java 导出参数），由 `PageRequest.NormalizeSize` 在 `RepositoryBase.PageAsync` 入口统一归一化——Service/仓储业务代码不做任何 size 转换。

### DI 与接线（Program.cs）

```csharp
builder.Services.AddScoped<IXxxRepository, XxxRepository>();
builder.Services.AddScoped<IXxxService, XxxService>();
// ...
api.MapXxxEndpoint();
```

## 迁移流程（Java → .NET）

1. 读 Java 侧 controller（路由/方法/出入参）→ service impl（业务规则）→ mapper XML（实际 SQL：过滤、排序、分页）。
2. 按四层模板落地，路由与 HTTP 方法逐一勾对。
3. 验收：登录拿 token → Scalar 带 token 调通增删改查 + 分页 + 下拉。

## 注释规范

所有类、字段、属性、方法必须有 XML 文档注释（中文 `<summary>`；公共方法补 `<param>`/`<returns>`）。**禁止 `<inheritdoc />`**——实现类成员同样写完整注释，读源码时不应翻接口才能看懂。存量文件不强制立即回填，但凡是扫到或改到的文件，发现缺注释的成员必须当场补上。

## 其他

- 配置：连接串与密钥走 user-secrets（`wuxi-erp-apiservice-secrets`），appsettings 只放占位。
- 调试：根路径自动重定向 `/scalar/v1`；`/health`、`/alive` 由 ServiceDefaults 提供，勿重复注册。
