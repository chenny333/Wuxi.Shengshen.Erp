# AGENTS.md — Wuxi.Shengshen.Erp

> 本文件为 AI 助手的项目级记忆。每次会话开始时阅读，以继承项目规范与偏好。

## 1. 项目定位

- 基于 **.NET 10 + Aspire 13.1** 的无锡燊晟 ERP 后端，对应 Java 实现 `E:\Kreakin\wuxi-erp-api`。
- 模块：`Wuxi.Shengshen.Erp.AppHost`（Aspire 编排）/ `Wuxi.Shengshen.Erp.ServiceDefaults`（横切共享）/ `Wuxi.Shengshen.Erp.ApiService`（后端 Minimal API）/ `Wuxi.Shengshen.Erp.Web`（Blazor 前端）。
- **业务事实来源**：Java 后端（91 个 WebController + 10 个 PDA + 152 个实体 + ~50 张表）；架构骨架对齐 Mio `E:\Kreakin\wuxi-erp-dotnet\Mio` 的 RBAC 样板。

## 2. 技术栈

- .NET 10 / C#（ImplicitUsings + Nullable 开启）
- Aspire AppHost SDK 13.1.0
- Minimal API（无 Controller），Scalar API 文档（`/scalar/v1`）
- **Dapper 2.1.66 + MySqlConnector 2.4.0**（轻量 ORM，手写 SQL）
- **System.IdentityModel.Tokens.Jwt 8.14.0**（JWT HS256）+ **BCrypt.Net-Next 4.0.3**（兼容 Java 侧 `$2a$` 哈希）
- StackExchange.Redis + DistributedLock 2.8.0
- 外部业务库 `king-v.core`（相对引用，见 slnx）：ApiResult / ExceptionMiddleware / Captcha / IdWorker / BusinessException / CaptchaOptions

## 3. 分层与目录约定

```
AppHost/          编排：AddRedis/AddMySql + AddProject + WithReference/WaitFor
ServiceDefaults/  共享横切：健康检查 / OTel / 服务发现 / 弹性
ApiService/
  Endpoint/       端点分组，静态类 + MapXxxEndpoint(this RouteGroupBuilder)
  Data/Requests/  请求 record，DataAnnotations 校验（中文错误消息）
  Domain/         实体（BaseEntity/BaseAuditEntity/DomainBaseEntity + 业务子包）
  Repository/     Dapper 仓储（RepositoryBase<T> 含审计填充 + 逻辑删除）
  Security/       鉴权：UserContext/TokenService/PasswordUtil/AuthMiddleware/AuthConstants/SecurityOptions/AllowAnonymousAttribute
  Infrastructure/ 数据(MySqlConnectionFactory)/IdGen(SnowflakeId)/Json(WuxiJson)/Validation(RequestValidator)
  Web/            分页与动态表头（PageVo<T>/HeaderVo/TableHeaderAttribute/HeaderBuilder）
```

- 端点统一挂 `/api` 前缀（对齐 Java 契约），匿名端点用 `AllowAnonymousAttribute` 元数据。
- 业务异常抛 `KingV.Core.Exceptions.BusinessException`，由 `ExceptionMiddleware` 输出统一响应。
- **统一返回 `KingV.Core.Results.ApiResult<T>`**。
- 分页响应用 `PageVo<T>.Of(records, total)`，自动反射 `[TableHeader]` 生成 headers + voClassName。

## 4. 关键实现约定（踩坑记录）

- **Dapper 不认 `[Column]` 特性**：已在 Program.cs 设 `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true`，snake_case 列自动映射 PascalCase 属性。实体保留 `[Column]` 仅作文档。
- **验证码**：用 `KingV.Core.Helpers` 的 `CaptchaOptions.CreateVerifyImage(out code)` 扩展方法（需 `using KingV.Core.Helpers;`）；**校验即删**（修正 Java 可重放）。
- **密码**：BCrypt.Net，`Encode(id, pwd)=BCrypt(id+pwd)`，兼容 Java 现有 `$2a$` 哈希数据。
- **JWT**：不设 exp，过期由 Redis `wuxi:token:{id}` TTL 控制；单点靠同 key 覆盖 + 字符串比对。
- **雪花 ID**：`SnowflakeId.NextId()`（KingV IdWorker，workerId=1/datacenterId=1），INSERT 时 `RepositoryBase.FillForInsert` 自动填充。
- **请求校验**：Minimal API 不自动跑 DataAnnotations，端点需手动 `RequestValidator.Validate(request)`，失败抛 BusinessException(400)。
- **基座阶段 AuthMiddleware**：仅从 JWT claims 构建 LoginUser（id+userName）；业务模块启用后可注入 `ICurrentUserEnricher` 补全角色/部门。

## 5. 配置约定

- 连接串：`ConnectionStrings:Redis` / `ConnectionStrings:MySql`（MySql 由 AppHost 的 `AddMySql("MySql")` 注入，固定名）。
- 业务配置：`CaptchaOptions` / `Security`（Secret/Header/Prefix/ExpireHours/SingleSession）。
- **绝不提交真实凭据**：本地用 `localhost` 占位；开发共享 Redis 用真实地址（密码见 env 注入，prod 改用 Vault/环境变量）。
- **JWT Secret 占位**：至少 32 字符随机值，prod 必须从密钥管理服务注入。

## 6. 协作偏好（继承自用户）

- **回复用中文**，简洁专业。用户为 C# / .NET 后端工程师（熟悉 MyBatis-Plus 思路），从后端视角切入。
- 新增/修改公共方法、字段需补 **XML 文档注释**（`<summary>` 一行中文 + `<param>`/`<returns>`）。
- 命名沿用 Mio 风格：业务异常用 `BusinessException`、表名小写下划线、端点静态类 + 扩展方法挂载。
- 业务模块按 Java 端 controller 一对一映射，路由前缀与 Java 端对齐（如 `/currency/web`）。

## 7. 常用命令

```bash
# 启动整套服务（Redis + MySQL + ApiService + Web），自动打开 Aspire Dashboard
dotnet run --project Wuxi.Shengshen.Erp.AppHost

# 仅启动后端
dotnet run --project Wuxi.Shengshen.Erp.ApiService

# 构建
dotnet build Wuxi.Shengshen.Erp.slnx
```