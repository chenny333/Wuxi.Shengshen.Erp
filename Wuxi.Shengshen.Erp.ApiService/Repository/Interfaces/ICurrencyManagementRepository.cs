using Wuxi.Shengshen.Erp.ApiService.Domain.Currency;

namespace Wuxi.Shengshen.Erp.ApiService.Repository.Interfaces;

/// <summary>
/// 币种管理仓储。
/// </summary>
public interface ICurrencyManagementRepository
{
    /// <summary>按 ID 查询（未逻辑删除）。</summary>
    Task<CurrencyManagement?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>分页查询：按币种名称模糊匹配；排序字段经白名单校验，非法值回落 create_time desc。</summary>
    Task<(List<CurrencyManagement> Records, long Total)> PageByNameAsync(
        string? name,
        string? orderField,
        string? orderSort,
        int current,
        int size,
        CancellationToken cancellationToken = default);

    /// <summary>下拉列表：全部未逻辑删除记录（对齐 Java getListByEntity 行为，不过滤禁用）。</summary>
    Task<List<CurrencyManagement>> GetDownListAsync(CancellationToken cancellationToken = default);

    /// <summary>新增（基类自动填充雪花 ID 与审计字段）。</summary>
    Task<int> InsertAsync(CurrencyManagement entity, CancellationToken cancellationToken = default);

    /// <summary>按 ID 全字段更新（基类自动填充更新审计字段）。</summary>
    Task<int> UpdateAsync(CurrencyManagement entity, CancellationToken cancellationToken = default);

    /// <summary>逻辑删除。</summary>
    Task<bool> LogicDeleteAsync(long id, CancellationToken cancellationToken = default);
}
