using Wuxi.Shengshen.Erp.ApiService.Domain.User;

namespace Wuxi.Shengshen.Erp.ApiService.Repository.Interfaces;

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

    /// <summary>
    /// 新增用户（基类自动填充雪花 ID 与审计字段）。
    /// </summary>
    /// <param name="entity">用户实体（密码已 BCrypt 哈希）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<int> InsertAsync(User entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 ID 全字段更新（基类自动填充更新审计字段）。
    /// </summary>
    /// <param name="entity">用户实体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<int> UpdateAsync(User entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 逻辑删除用户。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task<bool> LogicDeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询：按姓名 / 账号 / 邮箱模糊匹配 + 部门精确匹配；
    /// 排序字段经白名单校验，非法值回落默认排序（create_time desc）。
    /// </summary>
    /// <param name="name">姓名模糊关键字。</param>
    /// <param name="account">账号模糊关键字。</param>
    /// <param name="email">邮箱模糊关键字。</param>
    /// <param name="departmentId">部门 ID（精确匹配）。</param>
    /// <param name="orderField">前端回传的排序列名（白名单外回落默认）。</param>
    /// <param name="orderSort">排序方向（"asc" 升序，其余倒序）。</param>
    /// <param name="current">页码（从 1 开始）。</param>
    /// <param name="size">每页条数（-1 取全部）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前页记录与总条数。</returns>
    Task<(List<User> Records, long Total)> PageAsync(
        string? name,
        string? account,
        string? email,
        long? departmentId,
        string? orderField,
        string? orderSort,
        int current,
        int size,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询下拉列表数据：未逻辑删除用户全部返回（与 Java 端 getListByEntity 行为一致，不过滤禁用）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户实体列表。</returns>
    Task<List<User>> GetDownListAsync(CancellationToken cancellationToken = default);
}