using KingV.Core.Data;
using Wuxi.Shengshen.Erp.ApiService.Constants.User;

namespace Wuxi.Shengshen.Erp.ApiService.Domain.User;

/// <summary>
/// 用户实体（对应 Java User，表 user）。
/// 审计 / 禁用 / 逻辑删除字段由 <see cref="DomainEntity"/> 承载；
/// 老库表缺 create_by / update_by / tenant_id，故以 <see cref="AuditIgnoreAttribute"/> 声明缺失，写入侧自动跳过。
/// <see cref="Account"/> 字段按 <see cref="UniqueConstraintAttribute"/> 声明唯一，新增/编辑写入前由 RepositoryBase 自动查重，禁止业务层手写查重 SQL。
/// 已绑定角色 ID 列表（user_role_mp 关联）由 Service 层独立查询后附加到 <see cref="Data.Responses.User.UserDetailResponse"/>，
/// 不放在实体上以避免 RepositoryBase 反射时误识别为数据库列。
/// </summary>
[AuditIgnore(AuditFields.CreateBy | AuditFields.UpdateBy | AuditFields.TenantId)]
[UniqueConstraint(nameof(Account), ErrorMessage = UserErrorMessages.AccountDuplicate)]
public class User : DomainEntity
{
    /// <summary>姓名。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>账号（与邮箱相同）。</summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>密码（BCrypt 哈希）。</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>英文名。</summary>
    public string? EnglishName { get; set; }

    /// <summary>手机号。</summary>
    public string? Phone { get; set; }

    /// <summary>性别（1 男 / 2 女，枚举见 <see cref="Domain.User.Sex"/>；DB 存数值，JSON 按数值读写）。</summary>
    public Sex? Sex { get; set; }

    /// <summary>邮箱。</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>所属部门 ID。</summary>
    public long? DepartmentId { get; set; }

    /// <summary>出生日期（Unix 毫秒时间戳）。</summary>
    public long? BirthDay { get; set; }

    /// <summary>是否系统内置（系统用户不允许删除/禁用）。</summary>
    public bool? IsSystem { get; set; }
}