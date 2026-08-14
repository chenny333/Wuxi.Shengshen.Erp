using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wuxi.Shengshen.Erp.ApiService.Infrastructure.Json;

/// <summary>
/// 统一 JSON 序列化选项：camelCase 属性名、忽略 null、枚举按数值，与前端契约保持一致。
/// </summary>
public static class WuxiJson
{
    /// <summary>
    /// 生成一份新的序列化选项（供 Program 注册到 ASP.NET，避免共享实例被并发修改）。
    /// </summary>
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        // 枚举按数值序列化（对齐 Java @JsonValue 的 id 语义），并兼容字符串读取
        options.Converters.Add(new JsonNumberEnumConverterFactory());
        return options;
    }

    /// <summary>
    /// 枚举按数值序列化的转换器工厂（写整数、读整数/字符串均可）。
    /// </summary>
    private sealed class JsonNumberEnumConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(NumberEnumConverter<>).MakeGenericType(typeToConvert))!;
    }

    private sealed class NumberEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var num))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), num);
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                var text = reader.GetString();
                if (int.TryParse(text, out var parsed)) return (TEnum)Enum.ToObject(typeof(TEnum), parsed);
                if (Enum.TryParse<TEnum>(text, ignoreCase: true, out var named)) return named;
            }
            throw new JsonException($"无法将值反序列化为枚举 {typeof(TEnum).Name}");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(Convert.ToInt32(value));
    }
}