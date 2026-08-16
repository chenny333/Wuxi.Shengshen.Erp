using Facet;
using KingV.Core.Web;
using Wuxi.Shengshen.Erp.ApiService.Domain.Currency;

namespace Wuxi.Shengshen.Erp.ApiService.Data.Responses.Currency;

/// <summary>
/// 币种管理详情（对应 Java GetCurrencyManagementVo）。
/// Id / Enable / 审计字段由 <see cref="EnableResponse"/> 承载（Facet 自动映射，不重复生成）；
/// Name / Remark 由 Facet 从实体生成；Enable 取反见 <see cref="CurrencyManagementDetailResponseMapper"/>。
/// </summary>
[Facet(typeof(CurrencyManagement),
    nameof(CurrencyManagement.IsDisable), nameof(CurrencyManagement.IsDelete),
    Configuration = typeof(CurrencyManagementDetailResponseMapper))]
public partial class CurrencyManagementDetailResponse : EnableResponse;

/// <summary>
/// 币种管理列表行（对应 Java GetCurrencyManagementListVo）。
/// Id / Enable / 审计字段由 <see cref="EnableResponse"/> 承载（CreateTime / Enable 自带表头列）；
/// Name / Remark 手工声明并挂表头列（标 [MapFrom] 声明为"用户自声明成员"，Facet 只做映射不重复生成属性）；
/// Enable 取反见 <see cref="CurrencyManagementListItemResponseMapper"/>。
/// </summary>
[Facet(typeof(CurrencyManagement),
    nameof(CurrencyManagement.IsDisable), nameof(CurrencyManagement.IsDelete),
    Configuration = typeof(CurrencyManagementListItemResponseMapper))]
public partial class CurrencyManagementListItemResponse : EnableResponse
{
    /// <summary>币种名称。</summary>
    [MapFrom(nameof(CurrencyManagement.Name))]
    [TableHeader("币种名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>备注。</summary>
    [MapFrom(nameof(CurrencyManagement.Remark))]
    [TableHeader("备注", Sortable = SortMode.False)]
    public string? Remark { get; set; }
}

/// <summary>
/// 币种管理下拉项（对应 Java GetCurrencyManagementDownListVo）。
/// Id 由 <see cref="IdResponse"/> 承载；Name / Remark 由 Facet 从实体生成。
/// </summary>
[Facet(typeof(CurrencyManagement),
    nameof(CurrencyManagement.IsDisable), nameof(CurrencyManagement.IsDelete),
    nameof(CurrencyManagement.Creator), nameof(CurrencyManagement.CreateBy), nameof(CurrencyManagement.CreateTime),
    nameof(CurrencyManagement.Updater), nameof(CurrencyManagement.UpdateBy), nameof(CurrencyManagement.UpdateTime),
    nameof(CurrencyManagement.TenantId))]
public partial class CurrencyManagementDownListItemResponse : IdResponse;
