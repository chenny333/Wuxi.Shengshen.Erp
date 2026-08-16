using KingV.Core.Data;

namespace Wuxi.Shengshen.Erp.ApiService.Domain.User;

/// <summary>
/// 用户-角色关联实体（对应 Java UserRoleMp，表 user_role_mp）。
/// 老库表只有 creator / create_time / updater / update_time 四个审计列，故以 <see cref="AuditIgnoreAttribute"/> 声明缺失。
/// 重复绑定由 Service 层显式查重拦截（对齐 Java <c>UserRoleMpRepositoryImpl.createToBefore</c> 的 pre-check）；
/// 不挂 <see cref="UniqueConstraintAttribute"/> 是因为全量替换语义会先清后插，逻辑删除后旧行仍占用联合唯一，
/// 复用 RepositoryBase 自动查重会与"先清"路径冲突，故由 Service 层预查重更可控。
/// </summary>
[AuditIgnore(AuditFields.CreateBy | AuditFields.UpdateBy | AuditFields.TenantId)]
public class UserRoleMp : DomainEntity
{
    /// <summary>用户 ID。</summary>
    public long UserId { get; set; }

    /// <summary>角色 ID。</summary>
    public long RoleId { get; set; }
}