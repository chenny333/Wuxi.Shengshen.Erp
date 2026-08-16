using System.ComponentModel.DataAnnotations;
using Facet;
using KingV.Core.Web;
using Wuxi.Shengshen.Erp.ApiService.Domain.Currency;

namespace Wuxi.Shengshen.Erp.ApiService.Data.Requests.Currency;

/// <summary>
/// 创建币种管理请求（对应 Java CreateCurrencyManagementParam）。
/// 请求 → 实体由 Facet 反向映射（GenerateToSource）：Name / Remark 标 [MapFrom(Reversible = true)]
/// 声明为"用户自声明成员"——Facet 只生成映射代码、不重复生成属性，校验特性挂在手工声明上；
/// Enable → IsDisable 取反见 <see cref="CreateCurrencyManagementToSourceMapper"/>；
/// 排除字段以构造函数位置参数列出（6.6.8 的 Exclude 属性只读，不能当命名参数用）。
/// </summary>
[Facet(typeof(CurrencyManagement),
    nameof(CurrencyManagement.Id),
    nameof(CurrencyManagement.Creator), nameof(CurrencyManagement.CreateBy), nameof(CurrencyManagement.CreateTime),
    nameof(CurrencyManagement.Updater), nameof(CurrencyManagement.UpdateBy), nameof(CurrencyManagement.UpdateTime),
    nameof(CurrencyManagement.TenantId),
    nameof(CurrencyManagement.IsDisable), nameof(CurrencyManagement.IsDelete),
    GenerateToSource = true,
    ToSourceConfiguration = typeof(CreateCurrencyManagementToSourceMapper))]
public partial class CreateCurrencyManagementRequest
{
    /// <summary>币种名称。</summary>
    [MapFrom(nameof(CurrencyManagement.Name), Reversible = true)]
    [Required(ErrorMessage = "币种名称不能为空")]
    [StringLength(30, ErrorMessage = "币种名称最大长度不能超过{0}")]
    public string Name { get; set; } = string.Empty;

    /// <summary>备注。</summary>
    [MapFrom(nameof(CurrencyManagement.Remark), Reversible = true)]
    [StringLength(255, ErrorMessage = "备注最大长度不能超过{0}")]
    public string? Remark { get; set; }

    /// <summary>是否启用（实体侧无同名属性，不参与自动映射；由反向配置取反落 is_disable）。</summary>
    [Required(ErrorMessage = "是否启用不能为空")]
    public bool? Enable { get; set; }
}

/// <summary>
/// 编辑币种管理请求（对应 Java EditCurrencyManagementParam）。
/// Id 经 <see cref="IIdRequest"/> 接口叠加（基类槽位被 Create 请求占用），
/// 且不参与 Facet 映射（Id 在排除列表；编辑走 ApplyToSource 覆盖到已加载实体，Id 由实体自身携带）。
/// </summary>
[Facet(typeof(CurrencyManagement),
    nameof(CurrencyManagement.Id),
    nameof(CurrencyManagement.Creator), nameof(CurrencyManagement.CreateBy), nameof(CurrencyManagement.CreateTime),
    nameof(CurrencyManagement.Updater), nameof(CurrencyManagement.UpdateBy), nameof(CurrencyManagement.UpdateTime),
    nameof(CurrencyManagement.TenantId),
    nameof(CurrencyManagement.IsDisable), nameof(CurrencyManagement.IsDelete),
    GenerateToSource = true,
    ToSourceConfiguration = typeof(EditCurrencyManagementToSourceMapper))]
public partial class EditCurrencyManagementRequest : CreateCurrencyManagementRequest, IIdRequest
{
    /// <summary>主键 ID。</summary>
    [Required(ErrorMessage = "ID不能为空")]
    public long? Id { get; set; }
}

/// <summary>
/// 币种管理列表查询请求（对应 Java GetCurrencyManagementListParam）。
/// </summary>
public record GetCurrencyManagementListRequest : PageRequest
{
    /// <summary>币种名称（模糊匹配）。</summary>
    [StringLength(30, ErrorMessage = "币种名称最大长度不能超过{0}")]
    public string? Name { get; set; }
}
