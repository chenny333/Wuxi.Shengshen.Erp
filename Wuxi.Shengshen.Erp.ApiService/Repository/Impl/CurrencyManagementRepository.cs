using KingV.Core.Data;
using Wuxi.Shengshen.Erp.ApiService.Domain.Currency;
using Wuxi.Shengshen.Erp.ApiService.Repository.Interfaces;

namespace Wuxi.Shengshen.Erp.ApiService.Repository.Impl;

/// <summary>
/// 币种管理仓储（Dapper + SqlKata）。
/// 分页 SQL 对齐 Java mapper：name 模糊 + create_time desc；排序字段走白名单，防注入。
/// </summary>
public sealed class CurrencyManagementRepository : RepositoryBase<CurrencyManagement>, ICurrencyManagementRepository
{
    /// <summary>允许前端排序的列（白名单；orderField 是前端原样回传的列名，必须校验后才能拼 SQL）。</summary>
    private static readonly HashSet<string> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "name", "create_time"
    };

    /// <summary>
    /// 注入连接工厂。
    /// </summary>
    public CurrencyManagementRepository(MySqlConnectionFactory factory) : base(factory) { }

    /// <summary>表名固定为 currency_management。</summary>
    protected override string TableName => "currency_management";

    /// <summary>按名称模糊分页查询（排序字段经白名单校验，非法值回落 create_time 倒序）。</summary>
    /// <param name="name">名称模糊关键字（为空不过滤）。</param>
    /// <param name="orderField">前端回传的排序列名（白名单外回落默认）。</param>
    /// <param name="orderSort">排序方向（"asc" 升序，其余倒序）。</param>
    /// <param name="current">页码（从 1 开始）。</param>
    /// <param name="size">每页条数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前页记录与总条数。</returns>
    public Task<(List<CurrencyManagement> Records, long Total)> PageByNameAsync(
        string? name,
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
            return ascending ? q.OrderBy(sortColumn) : q.OrderByDesc(sortColumn);
        }, current, size, cancellationToken);
    }

    /// <summary>查询下拉列表数据（未删除记录按 create_time 倒序全量返回）。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>币种实体列表。</returns>
    public Task<List<CurrencyManagement>> GetDownListAsync(CancellationToken cancellationToken = default) =>
        FindAsync(q => q.OrderByDesc("create_time"), cancellationToken);
}
