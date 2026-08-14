using System.Text.Json.Serialization;

namespace Wuxi.Shengshen.Erp.ApiService.Web;

/// <summary>
/// 动态表头（对应 Java HeaderVo）。
/// </summary>
public sealed class HeaderVo
{
    /// <summary>字段名（camelCase 属性名）。</summary>
    [JsonPropertyName("prop")]
    public string Prop { get; set; } = string.Empty;

    /// <summary>列标题。</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>排序方式（true/false/"custom"）。</summary>
    [JsonPropertyName("sortable")]
    public object Sortable { get; set; } = true;

    /// <summary>排序字段（snake_case）。</summary>
    [JsonPropertyName("sortBy")]
    public string SortBy { get; set; } = string.Empty;

    /// <summary>枚举映射（id→描述），仅枚举字段生成。</summary>
    [JsonPropertyName("enumMap")]
    public Dictionary<int, string>? EnumMap { get; set; }

    /// <summary>是否自定义插槽列。</summary>
    [JsonPropertyName("slot")]
    public bool Slot { get; set; }

    /// <summary>是否显示（页面配置回填，可为 null）。</summary>
    [JsonPropertyName("show")]
    public bool? Show { get; set; }

    /// <summary>列宽（页面配置回填，可为 null）。</summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }
}

/// <summary>
/// 分页响应（对应 Java PageVo）。headers 由 VO 反射生成，voClassName 供前端页面配置。
/// </summary>
/// <typeparam name="T">行 VO 类型。</typeparam>
public sealed class PageVo<T>
{
    /// <summary>数据行。</summary>
    [JsonPropertyName("records")]
    public List<T> Records { get; set; } = new();

    /// <summary>总条数。</summary>
    [JsonPropertyName("total")]
    public long Total { get; set; }

    /// <summary>动态表头。</summary>
    [JsonPropertyName("headers")]
    public List<HeaderVo> Headers { get; set; } = new();

    /// <summary>VO 全类名（前端页面配置键）。</summary>
    [JsonPropertyName("voClassName")]
    public string VoClassName { get; set; } = string.Empty;

    /// <summary>
    /// 构造分页响应并按 T 上的 <see cref="TableHeaderAttribute"/> 生成动态表头。
    /// </summary>
    public static PageVo<T> Of(List<T> records, long total)
    {
        return new PageVo<T>
        {
            Records = records,
            Total = total,
            Headers = HeaderBuilder.BuildHeaders(typeof(T)),
            VoClassName = typeof(T).FullName ?? typeof(T).Name
        };
    }
}