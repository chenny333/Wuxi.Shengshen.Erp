using Facet.Extensions;
using KingV.Core.Exceptions;
using KingV.Core.Web;
using Wuxi.Shengshen.Erp.ApiService.Constants.Currency;
using Wuxi.Shengshen.Erp.ApiService.Data.Requests.Currency;
using Wuxi.Shengshen.Erp.ApiService.Data.Responses.Currency;
using Wuxi.Shengshen.Erp.ApiService.Domain.Currency;
using Wuxi.Shengshen.Erp.ApiService.Repository.Interfaces;
using Wuxi.Shengshen.Erp.ApiService.Service.Interfaces;

namespace Wuxi.Shengshen.Erp.ApiService.Service.Impl;

/// <summary>
/// 币种管理服务（对应 Java CurrencyManagementServiceImpl；enable 与实体 is_disable 恒为取反关系）。
/// </summary>
public sealed class CurrencyManagementService : ICurrencyManagementService
{
    /// <summary>币种仓储。</summary>
    private readonly ICurrencyManagementRepository _repository;

    /// <summary>注入仓储。</summary>
    /// <param name="repository">币种仓储。</param>
    public CurrencyManagementService(ICurrencyManagementRepository repository) => _repository = repository;

    /// <summary>新增币种（请求 → 实体由 Facet 生成的 ToSource 反向映射，Enable 取反落 is_disable）。</summary>
    /// <param name="request">创建请求（名称/备注/是否启用）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task CreateAsync(CreateCurrencyManagementRequest request, CancellationToken cancellationToken = default) =>
        _repository.InsertAsync(request.ToSource(), cancellationToken);

    /// <summary>编辑币种（Facet 生成的 ApplyToSource 把请求值覆盖到已加载实体，Enable 取反落 is_disable）；记录不存在抛"币种管理不存在"。</summary>
    /// <param name="request">编辑请求（Id + 名称/备注/是否启用）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task EditAsync(EditCurrencyManagementRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(request.Id!.Value, cancellationToken)
            ?? throw CurrencyErrorMessages.NotFound.NotFound();
        request.ApplyToSource(entity);
        await _repository.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>逻辑删除币种（is_delete = 1）。</summary>
    /// <param name="id">币种 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task RemoveAsync(long id, CancellationToken cancellationToken = default) =>
        _repository.LogicDeleteAsync(id, cancellationToken);

    /// <summary>切换启用状态（is_disable 取反）；记录不存在抛"币种管理不存在"。</summary>
    /// <param name="id">币种 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ToggleEnabledAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw CurrencyErrorMessages.NotFound.NotFound();
        entity.IsDisable = !entity.IsDisable;
        await _repository.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>按 ID 查询币种详情；记录不存在抛"币种管理不存在"。</summary>
    /// <param name="id">币种 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>币种详情（Enable 由 is_disable 取反）。</returns>
    public async Task<CurrencyManagementDetailResponse> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw CurrencyErrorMessages.NotFound.NotFound();
        return entity.ToFacet<CurrencyManagement, CurrencyManagementDetailResponse>();
    }

    /// <summary>分页查询币种列表（名称模糊 + 排序字段白名单；size = -1 取全部由仓储层统一归一化）。</summary>
    /// <param name="request">列表查询请求（分页参数 + 名称模糊）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页结果（Enable 由 is_disable 取反，审计字段整体带出）。</returns>
    public async Task<PageResult<CurrencyManagementListItemResponse>> GetListAsync(
        GetCurrencyManagementListRequest request, CancellationToken cancellationToken = default)
    {
        var (records, total) = await _repository.PageByNameAsync(
            request.Name, request.OrderField, request.OrderSort,
            (int)request.Current, (int)request.Size, cancellationToken);

        var list = records
            .SelectFacets<CurrencyManagement, CurrencyManagementListItemResponse>()
            .ToList();

        return PageResult<CurrencyManagementListItemResponse>.Of(list, total);
    }

    /// <summary>查询币种下拉列表（create_time 倒序全量）。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下拉项列表（Id/名称/备注）。</returns>
    public async Task<List<CurrencyManagementDownListItemResponse>> GetDownListAsync(
        CancellationToken cancellationToken = default)
    {
        var records = await _repository.GetDownListAsync(cancellationToken);
        return [.. records.SelectFacets<CurrencyManagement, CurrencyManagementDownListItemResponse>()];
    }
}
