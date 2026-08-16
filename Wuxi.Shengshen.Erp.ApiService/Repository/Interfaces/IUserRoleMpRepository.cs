using Wuxi.Shengshen.Erp.ApiService.Domain.User;

namespace Wuxi.Shengshen.Erp.ApiService.Repository.Interfaces;

/// <summary>
/// 用户-角色关联仓储接口（对应 Java UserRoleMpRepository）。
/// </summary>
public interface IUserRoleMpRepository
{
    /// <summary>
    /// 按用户 ID 查询其绑定的角色 ID 列表（未逻辑删除）。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>角色 ID 集合。</returns>
    Task<List<long>> GetRoleIdsByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按用户 ID 查询其绑定的关联记录（未逻辑删除）。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>UserRoleMp 关联实体列表。</returns>
    Task<List<UserRoleMp>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按角色 ID 与用户 ID 列表查询已存在关联（用于重复绑定的预检查，对齐 Java <c>UserRoleMpRepository.selectUserRoleMp</c>）。
    /// </summary>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="userIds">用户 ID 列表（去重）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已存在的关联记录列表。</returns>
    Task<List<UserRoleMp>> SelectExistingBindingsAsync(long roleId, IReadOnlyList<long> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按角色 ID 与用户 ID 列表批量逻辑删除关联记录（对齐 Java <c>UserRoleMpRepository.userUnbindRole</c>）。
    /// </summary>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="userIds">用户 ID 列表（去重）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>受影响行数。</returns>
    Task<int> BulkUnbindAsync(long roleId, IReadOnlyList<long> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 逻辑删除指定用户下的全部关联记录（全量替换语义：先清后插）。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>受影响行数。</returns>
    Task<int> LogicDeleteByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增一条关联记录（基类自动填充雪花 ID 与审计字段）。
    /// </summary>
    /// <param name="entity">关联实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<int> InsertAsync(UserRoleMp entity, CancellationToken cancellationToken = default);
}