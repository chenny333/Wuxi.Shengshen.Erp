using KingV.Core.Data;
using KingV.Core.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Wuxi.Shengshen.Erp.ApiService.Domain.User;
using Wuxi.Shengshen.Erp.ApiService.Repository.Interfaces;

namespace Wuxi.Shengshen.Erp.ApiService.Repository.Impl;

/// <summary>
/// 用户-角色关联仓储（Dapper + SqlKata）。
/// 对应 Java <c>user_role_mp</c> 中间表。
/// 角色绑定走全量替换语义：先逻辑删除该用户全部已绑定关联，再批量插入新集合。
/// </summary>
public sealed class UserRoleMpRepository : RepositoryBase<UserRoleMp>, IUserRoleMpRepository
{
    /// <summary>
    /// 注入连接工厂、Redis 连接与日志工厂。
    /// </summary>
    /// <param name="factory">MySQL 连接工厂。</param>
    /// <param name="redis">Redis 连接复用器。</param>
    /// <param name="loggerFactory">日志工厂。</param>
    public UserRoleMpRepository(
        MySqlConnectionFactory factory,
        IConnectionMultiplexer redis,
        ILoggerFactory loggerFactory) : base(factory, redis, loggerFactory) { }

    /// <summary>表名固定为 user_role_mp（对齐 Java Mapper XML 中的 SQL 表名）。</summary>
    protected override string TableName => "user_role_mp";

    /// <summary>
    /// 按用户 ID 查询其绑定的角色 ID 列表（未逻辑删除）。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>角色 ID 集合。</returns>
    public async Task<List<long>> GetRoleIdsByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var records = await GetByUserIdAsync(userId, cancellationToken);
        return [.. records.Select(r => r.RoleId)];
    }

    /// <summary>
    /// 按用户 ID 查询其绑定的关联记录（未逻辑删除）。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>UserRoleMp 关联实体列表。</returns>
    public Task<List<UserRoleMp>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        FindAsync(q => q.Where("user_id", userId), cancellationToken);

    /// <summary>
    /// 按角色 ID 与用户 ID 列表查询已存在的关联记录（未逻辑删除），用于重复绑定的预检查。
    /// 对齐 Java <c>UserRoleMpRepositoryImpl.selectUserRoleMp</c>。
    /// </summary>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="userIds">用户 ID 列表（去重）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已存在的关联记录列表。</returns>
    public Task<List<UserRoleMp>> SelectExistingBindingsAsync(
        long roleId, IReadOnlyList<long> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return Task.FromResult(new List<UserRoleMp>());
        return FindAsync(q => q
            .Where("role_id", roleId)
            .WhereIn("user_id", userIds), cancellationToken);
    }

    /// <summary>
    /// 按角色 ID 与用户 ID 列表批量逻辑删除关联记录。
    /// 对齐 Java <c>UserRoleMpRepositoryImpl.userUnbindRole</c> 的 DELETE WHERE role_id=? AND user_id IN (...) 语义。
    /// 本实现按 (roleId, userId) 维度先查再逐条逻辑删除（仓储层无 SQL 通道写中间表，等价 Java 端软删除语义）。
    /// </summary>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="userIds">用户 ID 列表（去重）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>受影响行数。</returns>
    public async Task<int> BulkUnbindAsync(
        long roleId, IReadOnlyList<long> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return 0;

        // 先按 (roleId + userIds) 取出全部待删记录，再逐条逻辑删除。
        var matched = await FindAsync(q => q
            .Where("role_id", roleId)
            .WhereIn("user_id", userIds), cancellationToken);

        var count = 0;
        await matched.ForEachAsync(async mp =>
        {
            if (await LogicDeleteAsync(mp.Id, cancellationToken)) count++;
        });
        return count;
    }

    /// <summary>
    /// 逻辑删除指定用户下的全部关联记录。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>受影响行数。</returns>
    public async Task<int> LogicDeleteByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var records = await GetByUserIdAsync(userId, cancellationToken);
        var count = 0;
        await records.ForEachAsync(async item =>
        {
            if (await LogicDeleteAsync(item.Id, cancellationToken)) count++;
        });
        return count;
    }
}