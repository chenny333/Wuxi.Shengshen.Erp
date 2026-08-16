using Facet.Mapping;
using Wuxi.Shengshen.Erp.ApiService.Domain.Currency;

namespace Wuxi.Shengshen.Erp.ApiService.Data.Responses.Currency;

/// <summary>
/// 币种详情映射补充：Enable 由实体 is_disable 取反得到。
/// 在 Facet 自动映射（同名属性）完成后调用，只处理取反逻辑。
/// </summary>
public sealed class CurrencyManagementDetailResponseMapper
    : IFacetMapConfiguration<CurrencyManagement, CurrencyManagementDetailResponse>
{
    /// <summary>补充映射 Enable（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">币种实体。</param>
    /// <param name="target">详情响应。</param>
    public static void Map(CurrencyManagement source, CurrencyManagementDetailResponse target) =>
        target.Enable = !source.IsDisable;
}

/// <summary>
/// 币种列表行映射补充：Enable 由实体 is_disable 取反得到。
/// 在 Facet 自动映射（同名属性）完成后调用，只处理取反逻辑。
/// </summary>
public sealed class CurrencyManagementListItemResponseMapper
    : IFacetMapConfiguration<CurrencyManagement, CurrencyManagementListItemResponse>
{
    /// <summary>补充映射 Enable（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">币种实体。</param>
    /// <param name="target">列表行响应。</param>
    public static void Map(CurrencyManagement source, CurrencyManagementListItemResponse target) =>
        target.Enable = !source.IsDisable;
}
