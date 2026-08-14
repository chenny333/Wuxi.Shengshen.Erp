using System.ComponentModel.DataAnnotations.Schema;

namespace Wuxi.Shengshen.Erp.ApiService.Domain;

/// <summary>
/// 实体基类：雪花 ID（对应 Java BaseEntity，非自增）。
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 主键，雪花算法 ID。
    /// </summary>
    [Column("id")]
    public long Id { get; set; }
}

/// <summary>
/// 审计实体基类：创建/更新/租户信息（对应 Java BaseAuditEntity，INSERT/UPDATE 时由仓储层填充）。
/// </summary>
public abstract class BaseAuditEntity : BaseEntity
{
    /// <summary>创建人 ID。</summary>
    [Column("creator")]
    public long? Creator { get; set; }

    /// <summary>创建人名称。</summary>
    [Column("create_by")]
    public string? CreateBy { get; set; }

    /// <summary>创建时间。</summary>
    [Column("create_time")]
    public DateTime? CreateTime { get; set; }

    /// <summary>更新人 ID。</summary>
    [Column("updater")]
    public long? Updater { get; set; }

    /// <summary>更新人名称。</summary>
    [Column("update_by")]
    public string? UpdateBy { get; set; }

    /// <summary>更新时间。</summary>
    [Column("update_time")]
    public DateTime? UpdateTime { get; set; }

    /// <summary>租户 ID（隔离字段）。</summary>
    [Column("tenant_id")]
    public long? TenantId { get; set; }
}

/// <summary>
/// 领域实体基类：再加禁用与逻辑删除标记（对应 Java DomainBaseEntity）。
/// </summary>
public abstract class DomainBaseEntity : BaseAuditEntity
{
    /// <summary>禁用标记。</summary>
    [Column("is_disable")]
    public bool IsDisable { get; set; }

    /// <summary>逻辑删除标记（1=删, 0=未删）。</summary>
    [Column("is_delete")]
    public bool IsDelete { get; set; }
}