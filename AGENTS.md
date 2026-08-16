# AGENTS.md — Wuxi.Shengshen.Erp

> 本文件为 AI 助手的项目级记忆。每次会话开始时阅读，以继承项目规范与偏好。
> 本文与代码不一致时，**以代码为准并回写本文**（用户明确要求文档随实现同步）。

## 0. 硬性规定（MUST，违反即返工）

1. **架构边界**：`KingV.Core`（`E:\Kreakin\king-v.core`）承载全部基础设施；`ApiService` 只写业务代码。禁止在 ApiService 里写中间件、仓储基类、鉴权、JSON/校验等框架能力。
2. **纯 .NET 10 风格**：一切代码以 .NET 10 为基准（含 KingV.Core 已有代码持续按 .NET 10 风格调整）。禁止把 Java 习惯带进业务代码：不写 `ApiResult.ok()` 式包装、不定义 `ErrorCode` 枚举/固定码。
3. **注释强制**：所有类、字段、属性、方法必须有 XML 文档注释（中文 `<summary>`；公共方法补 `<param>`/`<returns>`），**禁止 `<inheritdoc />`**（实现类成员也要写完整注释，源码内直接可读）。无注释的新代码不予合入；存量文件不强制立即回填，但**凡是扫到或改到的文件，发现缺注释的成员必须当场补上**。
4. **线协议**：`/api` 下所有响应必须是 `{ status, message, data, requestId }` 信封（status 为字符串，成功恒 `"200"`）。信封由 `ApiResponseEndpointFilter`（/api 分组）与 `ExceptionMiddleware` 自动产出，**业务代码禁止手工构造信封**。
5. **无数据成功响应**：直接 `Results.Ok()`（无参）即可——信封过滤器会把一切 2xx 结果包进信封，空结果输出 `{status:"200", message:"OK", data:null, requestId:"..."}`（对齐 Java：data 恒输出、requestId 为字符串，204 统一改写为 200）。禁止手工构造信封（同第 4 条）。
6. **校验约定**：请求 DTO 的所有字符串字段必须 `[StringLength(n)]`（n 对齐 DB 列宽，Java actable VARCHAR 未写长度默认 255）；ErrorMessage 中数值一律用 `{0}` 占位符，禁止硬编码数字；列表查询的模糊搜索字段同样要加。
7. **排序白名单**：前端回传的 `orderField` 必须经模块内白名单校验后才可拼入 ORDER BY，非法值回落默认排序。禁止直接拼接。
8. **DTO 继承约定**：Edit 请求 = `CreateXxxRequest` + `IIdRequest`（基类槽位被 Create 请求占用，Id 只能用接口叠加；`[Required]` 标在实现类属性上，DataAnnotations 不读接口特性）；列表请求继承 `PageRequest`；响应侧基类槽位空闲，用抽象基类链 `IdResponse`（Id）→ `BaseResponse`（+审计字段，CreateTime 自带"创建时间"表头列，其余审计字段只回数据）→ `EnableResponse`（+Enable，自带"是否启用"表头列），镜像实体侧 `AuditableEntity → DomainEntity`——详情与列表行继承 `EnableResponse`、下拉项继承 `IdResponse`，**禁止在响应类里重复声明 Id/Enable/审计字段**。
9. **路由对齐**：端点路径、HTTP 方法与 Java controller 完全一致（前端零改动），统一挂 `/api` 前缀。
10. **匿名端点**：统一 `.WithMetadata(new AllowAnonymousAttribute())`；禁止在中间件里写路径前缀白名单。
11. **凭据**：appsettings 只放 localhost 占位；真实连接串/密钥走 `dotnet user-secrets`（AppHost 与 ApiService 各自独立 UserSecretsId）或环境变量，绝不提交。
12. **老库表缺列**：老库表只有 creator/create_time/updater/update_time 四个审计列（对齐 Java BaseAuditEntity），缺 create_by/update_by/tenant_id。实体类必须标注 `[AuditIgnore(AuditFields.CreateBy | AuditFields.UpdateBy | AuditFields.TenantId)]`，由 `RepositoryBase` 在 INSERT/UPDATE 列映射与审计填充时自动跳过——**禁止改动 `AuditableEntity` 继承结构去适配老表**（参照 `CurrencyManagement` / `User` 实体）。
13. **映射工具**：实体 ↔ DTO 一律用 Facet 源生成器（DTO 标 `[Facet(typeof(实体), nameof(排除字段)..., ...)]` 并声明 `partial` **class**——**禁止用 record**：Facet 6.6.8 会把无主构造的 record 生成为 positional record，继承基类/手工声明的成员会让主构造参数 unread，所有值丢失且运行时不报错），编译期生成、零反射），**禁止手工逐字段 `new Dto { ... }` / 逐属性赋值**。实体 → 响应：排除字段 + `IFacetMapConfiguration` 处理取反（`Facet.Mapping` 命名空间），调用 `ToFacet`/`SelectFacets`。请求 → 实体：`GenerateToSource = true` + `IFacetToSourceConfiguration` 处理取反，Create 用生成的 `request.ToSource()`，Edit 用生成的 `request.ApplyToSource(entity)` 覆盖已加载实体（排除列表里的 Id/审计字段不受影响）。**DTO 上手工声明的字段**（挂 `[TableHeader]` / 校验特性时）必须同时标 `[MapFrom(nameof(实体.字段))]`（请求侧还要 `Reversible = true` 才进反向映射），否则与生成属性撞名（CS0102）。**禁用 Facet.Extensions 的 `ApplyFacet`**（运行时反射按名匹配，不走配置取反）。
14. **错误消息常量**：throw 业务异常的消息一律取自 `Constants/{模块}/XxxErrorMessages.cs` 常量类（措辞与 Java 端一致），**禁止在 throw 处硬编码中文字符串**；同一模块多处共用的消息只定义一次。
15. **分页语义**：`size = -1`（取全部）等 size 归一化由 `PageRequest.NormalizeSize` 在 `RepositoryBase.PageAsync` 入口统一处理，**禁止服务层/业务代码自行转换 size**。
16. **唯一性校验**：字段防重复一律在实体类上标 `[UniqueConstraint(nameof(属性), ErrorMessage = XxxErrorMessages.常量)]`（KingV.Core.Data；单特性多属性名 = 联合唯一，多组约束重复标注），由 `RepositoryBase` 写入前自动查重，**禁止服务层/仓储手写查重 SQL**。错误消息必须来自模块常量类（同第 14 条）。
17. **遍历风格**：循环遍历一律用 KingV.Core.Extensions 的集合扩展——顺序异步 `ForEachAsync`、并发异步 `LoopAsync`、同步遍历数组/List 用 `LoopSpan`（<1w 行）或 `LoopUnsafe`（>1w 行）；需提前退出/过滤的改用 LINQ（`Any`/`Select` 等）组合表达，**禁止裸写 `foreach`**。

## 1. 项目定位

- 基于 **.NET 10 + Aspire 13.4.6** 的无锡燊晟 ERP 后端，对应 Java 实现 `E:\Kreakin\wuxi-erp-api`。
- 模块：`Wuxi.Shengshen.Erp.AppHost`（Aspire 编排）/ `Wuxi.Shengshen.Erp.ServiceDefaults`（横切共享）/ `Wuxi.Shengshen.Erp.ApiService`（后端 Minimal API）/ `Wuxi.Shengshen.Erp.Web`（Blazor 前端，当前在 AppHost 中注释停用）。
- **业务事实来源**：Java 后端（91 个 WebController + 10 个 PDA + 152 个实体 + ~50 张表），迁移时逐一对照。
- **真实构建路径是本目录**；`E:\Kreakin\wuxi-erp-dotnet\Wuxi.Shengshen.Erp` 是旧副本，勿改。

## 2. 技术栈

- .NET 10 / C#（ImplicitUsings + Nullable 开启）
- Aspire AppHost SDK 13.4.6（`Aspire.AppHost.Sdk` 与 `Aspire.Hosting.AppHost` 版本必须一致）
- Minimal API（无 Controller）+ Scalar API 文档（`/scalar/v1`，根路径自动重定向）
- **Dapper 2.1.79 + SqlKata 4.0.1 + MySqlConnector 2.6.2**（由 KingV.Core 持有；ApiService 不直接引用）
- **Facet 6.6.8 + Facet.Extensions 6.6.8**（实体↔DTO 编译期映射源生成器，由 KingV.Core 持有并透传到 ApiService）
- **System.IdentityModel.Tokens.Jwt 8.22.0**（JWT HS256）+ **BCrypt.Net-Next 4.2.0**（兼容 Java `$2a$` 哈希）
- StackExchange.Redis 3.1.13 + DistributedLock 2.8.3（登录防重提交）
- Aspire 编排不依赖 Docker：`AddConnectionString("Redis")` / `AddConnectionString("MySql")` + `WithReference` + `WithHttpHealthCheck("/health")`

## 3. 分层与目录约定

```
KingV.Core/          核心组件框架（独立类库，FrameworkReference Microsoft.AspNetCore.App）
  Data/              EntityBase/AuditableEntity/DomainEntity、AuditIgnoreAttribute（老库表缺失审计列声明）、
                     MySqlConnectionFactory、SqlKataFactory(MySqlCompiler 单例)、
                     RepositoryBase<T>（CRUD+分页+审计填充+逻辑删除）
  Security/          TokenService(JWT+Redis单点+refreshToken)、AuthMiddleware、ILoginUserResolver、
                     LoginUser/UserContext、PasswordUtil(BCrypt)、AllowAnonymousAttribute、AuthConstants、SecurityOptions
  Web/               ApiResponse 信封 + ApiResponseEndpointFilter、PageResult<T>/TableHeaderAttribute、
                     PageRequest、IIdRequest、ResponseBases.cs（响应基类链 IdResponse→BaseResponse→EnableResponse）
  Middleware/        ExceptionMiddleware（异常→信封）
  Json/              JsonOptionsFactory（camelCase+忽略null+枚举按数值+long按字符串读写，对齐 Java Long→String）
  Validation/        RequestValidator（DataAnnotations 手动触发）
  Exceptions/        BusinessException（message + HTTP status）
  Extensions/        BusinessException 扩展（.NotFound()/.ParameterError() 等）、LoopSpan 等
  Captcha/ Helpers/ Snowflake/   验证码、ImageHelper.CreateVerifyImage、IdWorker

AppHost/             编排：AddConnectionString + AddProject + WithReference + WithHttpHealthCheck
ServiceDefaults/     共享横切：健康检查（/health /alive，已 AllowAnonymous）/ OTel / 服务发现 / 弹性
ApiService/          纯业务（详见 ApiService/README.md）
  Endpoint/          端点分组，静态类 + MapXxxEndpoint(this RouteGroupBuilder)
  Data/Requests/     请求 class（DataAnnotations，中文错误消息；写操作标 [Facet] 反向映射）
  Data/Responses/    响应 class（partial + [Facet] 声明式映射；需表头的字段标 [TableHeader]）
  Constants/         按模块分包的 XxxErrorMessages 错误消息常量（throw 处禁硬编码）
  Domain/            实体（继承 KingV.Core.Data.DomainEntity，按模块分包）
  Repository/        仓储：Interfaces/（接口）+ Impl/（继承 RepositoryBase<T>）
  Service/           业务服务：Interfaces/（接口）+ Impl/（实现）
  Security/          仅业务侧 ILoginUserResolver 实现
```

## 4. 关键实现约定（踩坑记录）

- **线协议信封**：成功由 `/api` 分组的 `ApiResponseEndpointFilter` 自动包装一切 2xx 结果——带值结果包 `data`，无参 `Results.Ok()` 等空结果统一 200、包 `data:null` 信封（对齐 Java：成功消息恒 `"OK"`、`data` 字段恒输出含 null、`requestId` 为字符串）；文件流/验证码/重定向等其余 IResult 原样放行；失败由 `ExceptionMiddleware` 输出同构信封——业务错误 HTTP 200（对齐 Java，前端判 `status != "200"` 弹 message），401/403 保留真实状态码，未知异常 HTTP 503 + `status:"5000"`。
- **Long 线格式**：Java 端 JacksonConfig 全局 Long→String（雪花 ID 超 JS 安全整数），`JsonOptionsFactory` 已注册 long/long? 字符串转换器全局对齐——响应里所有 long（含实体 id）按字符串下发，请求侧字符串/数字两种写法都能反序列化。
- **SqlKata 参数桥**：编译产物 `Bindings` 必须经 `RepositoryBase.ToDapperParameters` 转成命名 `DynamicParameters`（p0,p1,...），否则 Dapper 报 "enumerable sequence not allowed"。已封装在基类全部查询方法中，业务层正常写 `q.Where("account", account)`。
- **Dapper 列映射**：Program.cs 已设 `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true`；Insert/Update 列名由基类反射（`[Column]` 优先，否则 PascalCase→snake_case，按类型缓存）。
- **验证码**：`KingV.Core.Helpers` 的 `CaptchaOptions.CreateVerifyImage(out code)`（需 `using KingV.Core.Helpers;`）；**校验即删**（修正 Java 可重放）。
- **密码**：`PasswordUtil.Encode(id, pwd)=BCrypt(id+pwd)`，兼容 Java `$2a$` 数据。
- **JWT**：不设 exp，过期由 Redis `wuxi:token:{id}` TTL 控制；单点靠同 key 覆盖 + 字符串比对；登录返回 token + refreshToken（`TokenService.IssuePairAsync`，refresh 带 isRefresh 声明且不可当访问令牌）。
- **雪花 ID**：`SnowflakeId.NextId()`，INSERT 时 `FillForInsert` 自动填充。
- **请求校验**：Minimal API 不自动跑 DataAnnotations，端点首行 `RequestValidator.Validate(request)`。
- **登录用户**：`AuthMiddleware` 解析 JWT 后经业务侧 `ILoginUserResolver` 加载完整 LoginUser（ApiService 的 `LoginUserResolver` 实现）。
- **健康检查**：`/health`、`/alive` 由 ServiceDefaults `MapDefaultEndpoints()` 注册且已 AllowAnonymous——**禁止重复注册**（曾因此 AmbiguousMatchException）。
- **模板模块**：币别 `CurrencyManagement` 是标准模板（详见 ApiService/README.md），新模块照其四层结构迁移。

## 5. 配置约定

- 连接串：`ConnectionStrings:Redis` / `ConnectionStrings:MySql`，AppHost 与 ApiService **各自需要**（AppHost 注入给 ApiService 前自己也要能解析，否则 Dashboard 里 apiservice URL 为 0）。
- user-secrets ID：AppHost = `wuxi-erp-apphost-secrets`；ApiService = `wuxi-erp-apiservice-secrets`。
- 业务配置：`CaptchaOptions` / `Security`（Secret/Header/Prefix/ExpireHours/RefreshExpireHours/SingleSession）。

## 6. 协作偏好（继承自用户）

- **回复用中文**，简洁专业。用户为 C# / .NET 后端工程师（熟悉 MyBatis-Plus 思路），从后端视角切入。
- 业务模块按 Java controller 一对一映射；迁移时先读 Java 侧 controller/service/mapper XML 对齐语义，再落 .NET 代码。
- 迭代节奏：每步改完等用户验收测试后再进下一步。

## 7. 常用命令

```bash
# 启动整套服务（AppHost 编排 ApiService，自动打开 Aspire Dashboard）
dotnet run --project Wuxi.Shengshen.Erp.AppHost

# 仅启动后端
dotnet run --project Wuxi.Shengshen.Erp.ApiService

# 构建
dotnet build Wuxi.Shengshen.Erp.slnx
```
