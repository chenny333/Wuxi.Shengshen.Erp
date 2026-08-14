using Dapper;
using MySqlConnector;
using Wuxi.Shengshen.Erp.ApiService.Domain;
using Wuxi.Shengshen.Erp.ApiService.Infrastructure.Data;
using Wuxi.Shengshen.Erp.ApiService.Infrastructure.IdGen;
using Wuxi.Shengshen.Erp.ApiService.Security;

namespace Wuxi.Shengshen.Erp.ApiService.Repository;

/// <summary>
/// Dapper 仓储基类：提供连接、审计字段填充、逻辑删除公共行为。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public abstract class RepositoryBase<TEntity> where TEntity : BaseEntity
{
    private readonly MySqlConnectionFactory _factory;

    /// <summary>
    /// 注入连接工厂。
    /// </summary>
    protected RepositoryBase(MySqlConnectionFactory factory) => _factory = factory;

    /// <summary>表名（由子类提供）。</summary>
    protected abstract string TableName { get; }

    /// <summary>创建并打开连接。</summary>
    protected async Task<MySqlConnection> ConnAsync(CancellationToken ct = default) =>
        await _factory.CreateOpenConnectionAsync(ct);

    /// <summary>INSERT 前填充：雪花 ID + 审计字段。</summary>
    protected void FillForInsert(TEntity entity)
    {
        if (entity.Id == 0) entity.Id = SnowflakeId.NextId();
        if (entity is BaseAuditEntity audit)
        {
            var now = DateTime.Now;
            var user = UserContext.GetUser();
            audit.Creator ??= user?.Id;
            audit.CreateBy ??= user?.UserName;
            audit.CreateTime ??= now;
            audit.Updater = user?.Id;
            audit.UpdateBy = user?.UserName;
            audit.UpdateTime = now;
            audit.TenantId ??= user?.TenantId;
        }
        if (entity is DomainBaseEntity domain) domain.IsDelete = false;
    }

    /// <summary>UPDATE 前填充：更新审计字段。</summary>
    protected void FillForUpdate(TEntity entity)
    {
        if (entity is BaseAuditEntity audit)
        {
            var user = UserContext.GetUser();
            audit.Updater = user?.Id;
            audit.UpdateBy = user?.UserName;
            audit.UpdateTime = DateTime.Now;
        }
    }

    /// <summary>按 ID 查询（未逻辑删除）。</summary>
    public virtual async Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await ConnAsync(ct);
        var where = "id = @id" + NotDeletedWhere();
        return await conn.QuerySingleOrDefaultAsync<TEntity>(
            $"SELECT * FROM {TableName} WHERE {where}", new { id });
    }

    /// <summary>逻辑删除（DomainBaseEntity 子类更新 is_delete=1）。</summary>
    public virtual async Task<bool> LogicDeleteAsync(long id, CancellationToken ct = default)
    {
        await using var conn = await ConnAsync(ct);
        var affected = await conn.ExecuteAsync(
            $"UPDATE {TableName} SET is_delete = 1 WHERE id = @id", new { id });
        return affected > 0;
    }

    /// <summary>拼接未删除条件（DomainBaseEntity 子类才有 is_delete 列）。</summary>
    protected static string NotDeletedWhere() =>
        typeof(DomainBaseEntity).IsAssignableFrom(typeof(TEntity)) ? " AND is_delete = 0" : string.Empty;
}