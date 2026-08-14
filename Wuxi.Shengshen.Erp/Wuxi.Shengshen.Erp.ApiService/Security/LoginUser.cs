namespace Wuxi.Shengshen.Erp.ApiService.Security;

/// <summary>
/// 登录用户基类（对应 Java LoginUser）。
/// </summary>
public class LoginUser
{
    /// <summary>用户 ID。</summary>
    public long Id { get; set; }

    /// <summary>用户名。</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>部门 ID。</summary>
    public long? OrganizationId { get; set; }

    /// <summary>角色 ID 列表。</summary>
    public List<long> RoleIdList { get; set; } = new();

    /// <summary>租户 ID。</summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 当前登录用户上下文（对应 Java SecurityContext / UserContext，AsyncLocal 承载）。
/// </summary>
public static class UserContext
{
    private static readonly AsyncLocal<LoginUser?> Current = new();

    /// <summary>设置当前登录用户。</summary>
    public static void SetUser(LoginUser user) => Current.Value = user;

    /// <summary>获取当前登录用户（未登录返回 null）。</summary>
    public static LoginUser? GetUser() => Current.Value;

    /// <summary>获取当前登录用户，未登录抛异常。</summary>
    public static LoginUser GetUserRequired() =>
        Current.Value ?? throw new InvalidOperationException("当前无登录用户");

    /// <summary>清理当前用户。</summary>
    public static void Clear() => Current.Value = null;
}