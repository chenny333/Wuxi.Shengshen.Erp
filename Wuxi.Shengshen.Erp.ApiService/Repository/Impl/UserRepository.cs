using KingV.Core.Data;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wuxi.Shengshen.Erp.ApiService.Domain.User;
using Wuxi.Shengshen.Erp.ApiService.Repository.Interfaces;

namespace Wuxi.Shengshen.Erp.ApiService.Repository.Impl;

/// <summary>
/// 用户仓储（Dapper + SqlKata）。业务查询通过 <see cref="RepositoryBase{TEntity}.Query"/> 链式构造。
/// </summary>
public sealed class UserRepository : RepositoryBase<User>, IUserRepository
{
    /// <summary>
    /// 注入连接工厂、Redis 连接与日志工厂。
    /// </summary>
    /// <param name="factory">MySQL 连接工厂。</param>
    /// <param name="redis">Redis 连接复用器（实体缓存池）。</param>
    /// <param name="loggerFactory">日志工厂。</param>
    public UserRepository(
        MySqlConnectionFactory factory,
        IConnectionMultiplexer redis,
        ILoggerFactory loggerFactory) : base(factory, redis, loggerFactory) { }

    /// <summary>表名固定为 user。</summary>
    protected override string TableName => "user";

    /// <summary>
    /// 按账号查询单个用户（未逻辑删除）。
    /// </summary>
    /// <remarks>
    /// SqlKata 4.x 的 <c>Where(column, value)</c> 编译后会得到 <c>WHERE column = ?</c> + 单元素 bindings。
    /// 由 <see cref="KingV.Core.Data.RepositoryBase{TEntity}.FirstOrDefaultAsync"/> 统一通过
    /// <c>DynamicParameters</c> 把 Bindings 转给 Dapper，避免 Dapper 的"enumerable sequence not allowed"异常。
    /// </remarks>
    public Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken = default) =>
        FirstOrDefaultAsync(q => q.Where("account", account), cancellationToken);

    /// <summary>
    /// 按 ID 查询单个用户（未逻辑删除）。基类已用 SqlKata 实现。
    /// </summary>
    public override Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        base.GetByIdAsync(id, cancellationToken);
}