using KingV.Core.Data;

namespace Wuxi.Shengshen.Erp.ApiService.Domain.User;

/// <summary>
/// 用户实体（对应 Java User）。基座阶段只覆盖登录所需最小字段，
/// 业务模块启用后按需扩展（EnglishName / Phone / Sex / Email / IsSystem / BirthDay...）。
/// 老库表缺 create_by / update_by / tenant_id（对齐 Java BaseAuditEntity），写入侧自动跳过。
/// </summary>
[AuditIgnore(AuditFields.CreateBy | AuditFields.UpdateBy | AuditFields.TenantId)]
public class User : DomainEntity
{
    /// <summary>姓名。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>账号（与邮箱相同）。</summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>密码（BCrypt 哈希）。</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>所属部门 ID。</summary>
    public long? DepartmentId { get; set; }

    /// <summary>邮箱。</summary>
    public string Email { get; set; } = string.Empty;
}