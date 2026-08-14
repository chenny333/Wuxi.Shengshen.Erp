using System.ComponentModel.DataAnnotations.Schema;
using Wuxi.Shengshen.Erp.ApiService.Domain;

namespace Wuxi.Shengshen.Erp.ApiService.Domain.User;

/// <summary>
/// 用户实体（对应 Java User）。基座阶段只覆盖登录所需的最小字段集，
/// 业务模块启用后再按需扩展（englishName / phone / number / departmentId / email / isSystem / sex / birthDay...）。
/// </summary>
[Table("user")]
public class User : DomainBaseEntity
{
    /// <summary>姓名。</summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>账号（与邮箱相同）。</summary>
    [Column("account")]
    public string Account { get; set; } = string.Empty;

    /// <summary>密码（BCrypt 哈希）。</summary>
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>所属部门 ID。</summary>
    [Column("department_id")]
    public long? DepartmentId { get; set; }

    /// <summary>邮箱。</summary>
    [Column("email")]
    public string Email { get; set; } = string.Empty;
}