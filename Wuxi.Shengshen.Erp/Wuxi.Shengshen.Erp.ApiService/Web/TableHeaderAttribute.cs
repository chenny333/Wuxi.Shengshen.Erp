using System.Reflection;
using System.Text;

namespace Wuxi.Shengshen.Erp.ApiService.Web;

/// <summary>
/// 表头字段元数据（对应 Java @TableHeader 注解）。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class TableHeaderAttribute : Attribute
{
    /// <summary>列标题。</summary>
    public string Label { get; }

    /// <summary>排序方式（默认前端排序）。</summary>
    public SortableEnum Sortable { get; set; } = SortableEnum.Front;

    /// <summary>排序字段（默认属性名转 snake_case）。</summary>
    public string? SortBy { get; set; }

    /// <summary>是否自定义插槽列。</summary>
    public bool Slot { get; set; }

    /// <summary>构造表头元数据。</summary>
    public TableHeaderAttribute(string label) => Label = label;
}

/// <summary>
/// 排序方式（对应 Java SortableEnum）。
/// </summary>
public enum SortableEnum
{
    /// <summary>前端排序（true）。</summary>
    Front,
    /// <summary>不排序（false）。</summary>
    False,
    /// <summary>后端排序（"custom"）。</summary>
    Back
}

/// <summary>
/// 表头构建器：反射 VO 属性上的 <see cref="TableHeaderAttribute"/> 生成 <see cref="HeaderVo"/> 列表（按类型缓存）。
/// </summary>
public static class HeaderBuilder
{
    private static readonly Dictionary<Type, List<HeaderVo>> Cache = new();

    /// <summary>构建指定类型的表头。</summary>
    public static List<HeaderVo> BuildHeaders(Type voType)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(voType, out var cached)) return cached;
            var headers = Build(voType);
            Cache[voType] = headers;
            return headers;
        }
    }

    private static List<HeaderVo> Build(Type voType)
    {
        var list = new List<HeaderVo>();
        foreach (var prop in voType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<TableHeaderAttribute>();
            if (attr is null) continue;

            var sortable = attr.Sortable switch
            {
                SortableEnum.Front => (object)true,
                SortableEnum.False => false,
                SortableEnum.Back => "custom",
                _ => true
            };

            list.Add(new HeaderVo
            {
                Prop = ToCamelCase(prop.Name),
                Label = attr.Label,
                Sortable = sortable,
                SortBy = attr.SortBy ?? ToSnakeCase(prop.Name),
                EnumMap = BuildEnumMap(prop.PropertyType),
                Slot = attr.Slot,
                Show = null,
                Width = null
            });
        }
        return list;
    }

    private static Dictionary<int, string>? BuildEnumMap(Type type)
    {
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (!enumType.IsEnum) return null;
        var map = new Dictionary<int, string>();
        foreach (var name in Enum.GetNames(enumType))
        {
            var member = enumType.GetMember(name).FirstOrDefault();
            var desc = member?.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? name;
            map[Convert.ToInt32(Enum.Parse(enumType, name))] = desc;
        }
        return map;
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static string ToSnakeCase(string name)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }
}