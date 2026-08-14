using Wuxi.Shengshen.Erp.ApiService.Repository;

namespace Wuxi.Shengshen.Erp.ApiService.Security;

/// <summary>
/// 鉴权中间件（对应 Java AuthInterceptor）。
/// 放行带 <see cref="AllowAnonymousAttribute"/> 的端点；其余校验 JWT + Redis 单点会话，
/// 成功后通过 <see cref="IUserRepository"/> 加载用户上下文（包含禁用/删除校验）。
/// </summary>
public sealed class AuthMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 构造中间件。
    /// </summary>
    public AuthMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// 处理请求。
    /// </summary>
    public async Task InvokeAsync(HttpContext context, TokenService tokenService, IUserRepository userRepository)
    {
        UserContext.Clear();

        var endpoint = context.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() is not null
            || endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() is not null;

        if (!allowAnonymous)
        {
            var userId = await tokenService.GetAuthenticatedUserIdAsync(context.Request);
            if (userId is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var user = await userRepository.GetByIdAsync(userId.Value);
            if (user is null || user.IsDelete || user.IsDisable)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            UserContext.SetUser(new LoginUser
            {
                Id = user.Id,
                UserName = user.Name,
                OrganizationId = user.DepartmentId,
                TenantId = user.TenantId
            });
        }

        await _next(context);
    }
}