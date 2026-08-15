using KingV.Core.Captcha;
using KingV.Core.Data;
using KingV.Core.Json;
using KingV.Core.Middleware;
using KingV.Core.Security;
using KingV.Core.Web;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Scalar.AspNetCore;
using StackExchange.Redis;
using Wuxi.Shengshen.Erp.ApiService.Endpoint;
using Wuxi.Shengshen.Erp.ApiService.Repository;
using Wuxi.Shengshen.Erp.ApiService.Security;

var builder = WebApplication.CreateBuilder(args);

// Aspire 共享：服务发现/健康检查/OTel/弹性 HTTP。
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Redis 客户端 + 分布式锁（登录防重提交）。
builder.AddRedisClient("Redis");
builder.Services.AddKeyedSingleton<IDistributedLockProvider>(
    "redis",
    (provider, key) =>
    {
        var connection = provider.GetRequiredService<IConnectionMultiplexer>();
        return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
    });

// 配置绑定。
builder.Services.Configure<CaptchaOptions>(builder.Configuration.GetSection("CaptchaOptions"));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));

// 数据访问。
builder.Services.AddKingVCoreData();

// /health 端点：Aspire 健康检查由 AppHost 通过 WithHttpHealthCheck 编排（统一管理 Redis/MySQL 依赖检查）。
// 这里只提供一个简单的存活探针（永远 200），避免与 AppHost 配置的复合检查冲突。
builder.Services.AddHealthChecks();

// Dapper snake_case → PascalCase 自动映射（仅一次，进程级）。
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// 统一 JSON（camelCase + 忽略 null + 枚举按数值）。
builder.Services.ConfigureHttpJsonOptions(options =>
{
    var o = JsonOptionsFactory.Create();
    options.SerializerOptions.PropertyNamingPolicy = o.PropertyNamingPolicy;
    options.SerializerOptions.PropertyNameCaseInsensitive = o.PropertyNameCaseInsensitive;
    options.SerializerOptions.DefaultIgnoreCondition = o.DefaultIgnoreCondition;
    foreach (var c in o.Converters) options.SerializerOptions.Converters.Add(c);
});

// 鉴权与业务模块服务。
builder.Services.AddSingleton<TokenService>();
builder.Services.AddWuxiErpLoginUserResolver();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

// 业务异常统一走 KingV.Core 异常中间件转 ApiResponse 信封（对齐既有前端契约）。
app.UseMiddleware<ExceptionMiddleware>();

// JWT 鉴权：解析 token、Redis 单点会话、写 UserContext（业务模块的 LoginUserResolver 补全字段）。
app.UseMiddleware<AuthMiddleware>();

if (app.Environment.IsDevelopment())
{
    // OpenAPI 文档 + Scalar UI 均设为匿名，避免根路径访问时被 JWT 拦截。
    app.MapOpenApi().WithMetadata(new AllowAnonymousAttribute());
    app.MapScalarApiReference().WithMetadata(new AllowAnonymousAttribute());
}

app.MapDefaultEndpoints();

// 根路径重定向到 Scalar API 文档（先放行匿名）。
app.MapGet("/", () => Results.Redirect("/scalar/v1", permanent: false))
    .WithMetadata(new AllowAnonymousAttribute());
app.MapGet("/scalar", () => Results.Redirect("/scalar/v1", permanent: false))
    .WithMetadata(new AllowAnonymousAttribute());

// 业务端点统一挂 /api 前缀（对齐 Java 契约），并启用 ApiResponse 信封包装（对齐既有前端契约）。
var api = app.MapGroup("/api").WithApiResponse();
api.MapLoginEndpoint();

app.Run();