using KingV.Core.Captcha;
using KingV.Core.Exceptions;
using KingV.Core.Helpers;
using KingV.Core.Security;
using KingV.Core.Validation;
using Medallion.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Wuxi.Shengshen.Erp.ApiService.Data.Requests.Login;
using Wuxi.Shengshen.Erp.ApiService.Repository;

namespace Wuxi.Shengshen.Erp.ApiService.Endpoint;

/// <summary>
/// 登录端点（对应 Java LoginController，全匿名可访问）。
/// 验证码 + 账号密码 + JWT 签发（Redis 单点会话）。
/// 响应由 KingV.Core.Web.ApiResponseEndpointFilter 自动包成 { status, message, data, requestId } 信封；
/// 失败由 KingV.Core 异常中间件输出同构信封，前端契约与 Java 版完全一致。
/// </summary>
public static class LoginEndpoint
{
    /// <summary>验证码盐值响应头名称（前端从响应头读取后回传）。</summary>
    private const string SaltHeader = "auth_code_salt";

    /// <summary>
    /// 映射登录相关端点（挂 /api 前缀）。
    /// </summary>
    public static RouteGroupBuilder MapLoginEndpoint(this RouteGroupBuilder app)
    {
        var group = app.MapGroup("/login").WithTags("登录").WithSummary("登录管理");

        group.MapGet("/web/getAuthCode", GetAuthCode)
            .WithMetadata(new AllowAnonymousAttribute())
            .WithName("GetAuthCode")
            .WithDescription("生成并返回图片验证码")
            .WithSummary("获取验证码");

        group.MapPost("/web/accountLogin", AccountLogin)
            .WithMetadata(new AllowAnonymousAttribute())
            .WithName("AccountLogin")
            .WithDescription("PC端账号密码登录")
            .WithSummary("账号登录");

        return group;
    }

    private static async Task<IResult> GetAuthCode(
        HttpContext context,
        IConnectionMultiplexer multiplexer,
        IOptions<CaptchaOptions> options)
    {
        var captchaOptions = options.Value;
        using var image = captchaOptions.CreateVerifyImage(out var code);

        var salt = Guid.NewGuid().ToString("N");
        var db = multiplexer.GetDatabase();
        await db.StringSetAsync(
            AuthConstants.CaptchaKeyPrefix + salt,
            code.ToLowerInvariant(),
            TimeSpan.FromMinutes(captchaOptions.ExpiryMinutes));

        context.Response.Headers.Append(SaltHeader, salt);
        context.Response.Headers.Append("Pragma", "no-cache");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Expires", "Thu, 01 Jan 1970 00:00:00 GMT");
        context.Response.Headers.Append("Access-Control-Expose-Headers", SaltHeader);

        image.Position = 0;
        context.Response.ContentType = "image/jpeg";
        await image.CopyToAsync(context.Response.Body);
        await context.Response.Body.FlushAsync();
        return Results.Empty;
    }

    private static async Task<IResult> AccountLogin(
        [FromBody] LoginRequest request,
        [FromKeyedServices("redis")] IDistributedLockProvider distributedLockProvider,
        IConnectionMultiplexer multiplexer,
        IUserRepository userRepository,
        TokenService tokenService,
        CancellationToken cancellationToken)
    {
        RequestValidator.Validate(request);

        // 同一账号 2 秒内只允许一次登录尝试，避免暴力提交。
        var lockKey = $"Login:Lock:{request.Account.ToLowerInvariant()}";
        var @lock = distributedLockProvider.CreateLock(lockKey);
        await using var handle = await @lock.TryAcquireAsync(
            timeout: TimeSpan.FromSeconds(2),
            cancellationToken: cancellationToken) ?? throw "登录请求过于频繁，请稍后重试".ParameterError();

        var db = multiplexer.GetDatabase();
        var captchaKey = AuthConstants.CaptchaKeyPrefix + request.Salt;
        var expected = await db.StringGetAsync(captchaKey);

        // 校验即删（修正 Java 侧可重放的安全债）
        if (!expected.IsNullOrEmpty) await db.KeyDeleteAsync(captchaKey);

        if (expected.IsNullOrEmpty
            || !string.Equals(expected.ToString(), request.AuthCode, StringComparison.OrdinalIgnoreCase))
        {
            throw "验证码错误或已过期".CaptchaError();
        }

        var user = await userRepository.GetByAccountAsync(request.Account, cancellationToken);
        if (user is null) throw "用户不存在".NotFound();
        if (user.IsDelete) throw "用户不存在".NotFound();
        if (user.IsDisable) throw "用户已封禁，请联系管理员".ParameterError();

        if (!PasswordUtil.Matches(user.Id, request.Password, user.Password))
        {
            throw "密码错误".ParameterError();
        }

        var (token, refreshToken) = await tokenService.IssuePairAsync(user.Id);
        return Results.Ok(new
        {
            token,
            refreshToken,
            userId = user.Id,
            userName = user.Name,
            account = user.Account
        });
    }
}