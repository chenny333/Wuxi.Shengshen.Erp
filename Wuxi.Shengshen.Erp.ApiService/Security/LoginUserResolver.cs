using KingV.Core.Security;
using Wuxi.Shengshen.Erp.ApiService.Repository.Interfaces;

namespace Wuxi.Shengshen.Erp.ApiService.Security;

/// <summary>
/// 业务模块的 
/// <see cref="ILoginUserResolver"/>：按用户 ID 加载完整登录用户。
/// </summary>
public sealed class LoginUserResolver : ILoginUserResolver
{
    /// <summary>用户仓储。</summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 注入用户仓储。
    /// </summary>
    public LoginUserResolver(IUserRepository userRepository) => _userRepository = userRepository;

    /// <summary>
    /// 加载完整登录用户（id / userName / organizationId / tenantId）；用户不存在或被禁用返回 null。
    /// </summary>
    public async Task<LoginUser?> ResolveAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.IsDelete || user.IsDisable) return null;

        return new LoginUser
        {
            Id = user.Id,
            UserName = user.Name,
            OrganizationId = user.DepartmentId,
            TenantId = user.TenantId
        };
    }
}

/// <summary>
/// DI 扩展：注册业务模块实现的 LoginUserResolver。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册登录用户解析器（按用户表加载完整用户上下文）。
    /// </summary>
    public static IServiceCollection AddWuxiErpLoginUserResolver(this IServiceCollection services)
    {
        services.AddScoped<ILoginUserResolver, LoginUserResolver>();
        return services;
    }
}