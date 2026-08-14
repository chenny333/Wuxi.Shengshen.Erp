using Wuxi.Shengshen.Erp.ApiService.Endpoint;
using Wuxi.Shengshen.Erp.ApiService.Infrastructure.Data;
using Wuxi.Shengshen.Erp.ApiService.Infrastructure.Json;
using Wuxi.Shengshen.Erp.ApiService.Repository;
using Wuxi.Shengshen.Erp.ApiService.Security;
using KingV.Core.Captcha;
using KingV.Core.Middleware;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Aspire 共享：服务发现/健康检查/OTel/弹性 HTTP。
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

// 接口文档（Scalar）：开发环境暴露。
builder.Services.AddOpenApi();

// Redis：客户端 + 分布式锁（登录防重提交）。
builder.AddRedisClient("Redis");
builder.Services.AddKeyedSingleton<IDistributedLockProvider>(
    "redis",
    (provider, key) =>
    {
        var connection = provider.GetRequiredService<IConnectionMultiplexer>();
        return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
    });

// 配置绑定
builder.Services.Configure<CaptchaOptions>(builder.Configuration.GetSection("CaptchaOptions"));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));

// 数据访问（连接工厂 + Dapper snake_case 自动映射）。
builder.Services.AddSingleton<MySqlConnectionFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// 统一 JSON（camelCase + 忽略 null + 枚举按数值）。
builder.Services.ConfigureHttpJsonOptions(options =>
{
    var o = WuxiJson.Create();
    options.SerializerOptions.PropertyNamingPolicy = o.PropertyNamingPolicy;
    options.SerializerOptions.PropertyNameCaseInsensitive = o.PropertyNameCaseInsensitive;
    options.SerializerOptions.DefaultIgnoreCondition = o.DefaultIgnoreCondition;
    foreach (var c in o.Converters) options.SerializerOptions.Converters.Add(c);
});

// 安全服务
builder.Services.AddSingleton<TokenService>();

var app = builder.Build();

// 业务异常统一走 ExceptionMiddleware 输出 ApiResult 契约。
app.UseMiddleware<ExceptionMiddleware>();

// RBAC 鉴权（JWT + Redis 单点会话）。
app.UseMiddleware<AuthMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

// 业务端点统一挂 /api 前缀（对齐 Java 契约）。
var api = app.MapGroup("/api");
api.MapLoginEndpoint();

app.Run();