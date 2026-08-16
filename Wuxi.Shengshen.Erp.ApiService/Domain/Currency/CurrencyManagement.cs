using KingV.Core.Data;
using Wuxi.Shengshen.Erp.ApiService.Constants.Currency;

namespace Wuxi.Shengshen.Erp.ApiService.Domain.Currency;

/// <summary>
/// 币种管理实体（对应 Java CurrencyManagement，表 currency_management）。
/// 审计 / 禁用 / 逻辑删除字段由 <see cref="DomainEntity"/> 承载。
/// 老库表只有 creator / create_time / updater / update_time 四个审计列（对齐 Java BaseAuditEntity），
/// 缺 create_by / update_by / tenant_id，故以 <see cref="AuditIgnoreAttribute"/> 声明缺失，写入侧自动跳过。
/// <see cref="Name"/> 字段按 <see cref="UniqueConstraintAttribute"/> 声明唯一，新增/编辑写入前由 RepositoryBase 自动查重，禁止业务层手写查重 SQL。
/// <see cref="RedisCacheableAttribute"/> 声明启用实体缓存（30 天过期）：单条/列表查询先走 Redis 缓存池，新增/编辑写池、逻辑删除移出。
/// </summary>
[AuditIgnore(AuditFields.CreateBy | AuditFields.UpdateBy | AuditFields.TenantId)]
[UniqueConstraint(nameof(Name), ErrorMessage = CurrencyErrorMessages.NameDuplicate)]
[RedisCacheable(30, RedisExpireType.Day)]
public class CurrencyManagement : DomainEntity
{
    /// <summary>币种名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>备注。</summary>
    public string? Remark { get; set; }
}
