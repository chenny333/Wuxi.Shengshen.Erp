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
    /// <summary>允许前端排序的列（白名单；orderField 是前端原样回传的列名，必须校验后才能拼 SQL）。</summary>
    private static readonly HashSet<string> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "name", "account", "email", "create_time"
    };

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

    /// <summary>
    /// 分页查询用户列表（姓名/账号/邮箱 模糊 + 部门精确；排序字段经白名单校验）。
    /// </summary>
    /// <param name="name">姓名模糊关键字（为空不过滤）。</param>
    /// <param name="account">账号模糊关键字（为空不过滤）。</param>
    /// <param name="email">邮箱模糊关键字（为空不过滤）。</param>
    /// <param name="departmentId">部门 ID（为空不过滤）。</param>
    /// <param name="orderField">前端回传的排序列名（白名单外回落默认）。</param>
    /// <param name="orderSort">排序方向（"asc" 升序，其余倒序）。</param>
    /// <param name="current">页码（从 1 开始）。</param>
    /// <param name="size">每页条数（-1 取全部）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前页记录与总条数。</returns>
    public Task<(List<User> Records, long Total)> PageAsync(
        string? name,
        string? account,
        string? email,
        long? departmentId,
        string? orderField,
        string? orderSort,
        int current,
        int size,
        CancellationToken cancellationToken = default)
    {
        var sortColumn = orderField is not null && SortableColumns.Contains(orderField)
            ? orderField
            : "create_time";
        var ascending = string.Equals(orderSort, "asc", StringComparison.OrdinalIgnoreCase);

        return PageAsync(q =>
        {
            if (!string.IsNullOrWhiteSpace(name)) q.WhereLike("name", $"%{name}%");
            if (!string.IsNullOrWhiteSpace(account)) q.WhereLike("account", $"%{account}%");
            if (!string.IsNullOrWhiteSpace(email)) q.WhereLike("email", $"%{email}%");
            if (departmentId.HasValue) q.Where("department_id", departmentId.Value);
            return ascending ? q.OrderBy(sortColumn) : q.OrderByDesc(sortColumn);
        }, current, size, cancellationToken);
    }

    /// <summary>
    /// 查询下拉列表数据（仅启用中且未删除的记录，按 create_time 倒序全量返回）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>用户实体列表。</returns>
    public Task<List<User>> GetDownListAsync(CancellationToken cancellationToken = default) =>
        FindAsync(q => q.WhereFalse("is_disable").OrderByDesc("create_time"), cancellationToken);
}