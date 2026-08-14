using Dapper;
using Wuxi.Shengshen.Erp.ApiService.Domain.User;
using Wuxi.Shengshen.Erp.ApiService.Infrastructure.Data;

namespace Wuxi.Shengshen.Erp.ApiService.Repository;

/// <summary>
/// 用户仓储实现（Dapper + MySql）。
/// </summary>
public sealed class UserRepository : RepositoryBase<User>, IUserRepository
{
    /// <summary>
    /// 注入连接工厂。
    /// </summary>
    public UserRepository(MySqlConnectionFactory factory) : base(factory) { }

    /// <summary>表名固定为 user。</summary>
    protected override string TableName => "user";

    /// <summary>
    /// 按账号查询单个用户（未逻辑删除）。
    /// </summary>
    public async Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken = default)
    {
        await using var conn = await ConnAsync(cancellationToken);
        var sql = $"SELECT * FROM {TableName} WHERE account = @account{NotDeletedWhere()} LIMIT 1";
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { account });
    }

    /// <summary>
    /// 按 ID 查询单个用户（未逻辑删除），复写以保证列名映射到 User 表。
    /// </summary>
    public override async Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var conn = await ConnAsync(cancellationToken);
        var sql = $"SELECT * FROM {TableName} WHERE id = @id{NotDeletedWhere()}";
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { id });
    }
}