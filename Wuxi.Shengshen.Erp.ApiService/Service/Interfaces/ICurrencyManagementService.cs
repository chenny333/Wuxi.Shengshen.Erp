using KingV.Core.Web;
using Wuxi.Shengshen.Erp.ApiService.Data.Requests.Currency;
using Wuxi.Shengshen.Erp.ApiService.Data.Responses.Currency;

namespace Wuxi.Shengshen.Erp.ApiService.Service.Interfaces;

/// <summary>
/// 币种管理服务。
/// </summary>
public interface ICurrencyManagementService
{
    /// <summary>创建币种。</summary>
    Task CreateAsync(CreateCurrencyManagementRequest request, CancellationToken cancellationToken = default);

    /// <summary>编辑币种。</summary>
    Task EditAsync(EditCurrencyManagementRequest request, CancellationToken cancellationToken = default);

    /// <summary>逻辑删除币种。</summary>
    Task RemoveAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>启用/禁用切换。</summary>
    Task ToggleEnabledAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>详情。</summary>
    Task<CurrencyManagementDetailResponse> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>分页列表。</summary>
    Task<PageResult<CurrencyManagementListItemResponse>> GetListAsync(
        GetCurrencyManagementListRequest request, CancellationToken cancellationToken = default);

    /// <summary>下拉列表。</summary>
    Task<List<CurrencyManagementDownListItemResponse>> GetDownListAsync(CancellationToken cancellationToken = default);
}
