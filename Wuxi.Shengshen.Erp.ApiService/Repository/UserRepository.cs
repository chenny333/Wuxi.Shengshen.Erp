using KingV.Core.Data;
using Wuxi.Shengshen.Erp.ApiService.Domain.User;

namespace Wuxi.Shengshen.Erp.ApiService.Repository;

/// <summary>
/// 用户仓储（Dapper + SqlKata）。业务查询通过 <see cref="RepositoryBase{TEntity}.Query"/> 链式构造。
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
    public Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken = default) =>
        FirstOrDefaultAsync(q => q.Where("account", account), cancellationToken);

    /// <summary>
    /// 按 ID 查询单个用户（未逻辑删除）。基类已用 SqlKata 实现。
    /// </summary>
    public override Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        base.GetByIdAsync(id, cancellationToken);
}