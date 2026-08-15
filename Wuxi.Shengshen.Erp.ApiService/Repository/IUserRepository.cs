using Wuxi.Shengshen.Erp.ApiService.Domain.User;

namespace Wuxi.Shengshen.Erp.ApiService.Repository;

/// <summary>
/// 用户仓储接口。
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 按账号查询单个用户（未逻辑删除），返回 null 表示不存在。
    /// </summary>
    /// <param name="account">登录账号。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<User?> GetByAccountAsync(string account, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 查询单个用户（未逻辑删除）。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}