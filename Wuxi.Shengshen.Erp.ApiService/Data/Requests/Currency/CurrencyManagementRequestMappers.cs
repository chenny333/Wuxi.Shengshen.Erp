using Facet.Mapping;
using Wuxi.Shengshen.Erp.ApiService.Domain.Currency;

namespace Wuxi.Shengshen.Erp.ApiService.Data.Requests.Currency;

/// <summary>
/// 创建币种请求 → 实体反向映射补充：Enable 取反落 is_disable（Enable 缺省视为启用）。
/// 在 Facet 自动反向映射（同名属性）完成后调用，只处理取反逻辑。
/// </summary>
public sealed class CreateCurrencyManagementToSourceMapper
    : IFacetToSourceConfiguration<CreateCurrencyManagementRequest, CurrencyManagement>
{
    /// <summary>补充反向映射 IsDisable（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">创建请求。</param>
    /// <param name="target">币种实体。</param>
    public static void Map(CreateCurrencyManagementRequest source, CurrencyManagement target) =>
        target.IsDisable = !(source.Enable ?? true);
}

/// <summary>
/// 编辑币种请求 → 实体反向映射补充：Enable 取反落 is_disable（Enable 缺省视为启用）。
/// 在 Facet 自动反向映射（同名属性）完成后调用，只处理取反逻辑。
/// </summary>
public sealed class EditCurrencyManagementToSourceMapper
    : IFacetToSourceConfiguration<EditCurrencyManagementRequest, CurrencyManagement>
{
    /// <summary>补充反向映射 IsDisable（其余字段由 Facet 按同名自动映射）。</summary>
    /// <param name="source">编辑请求。</param>
    /// <param name="target">币种实体（已加载的持久化实体，ApplyToSource 覆盖其上）。</param>
    public static void Map(EditCurrencyManagementRequest source, CurrencyManagement target) =>
        target.IsDisable = !(source.Enable ?? true);
}
