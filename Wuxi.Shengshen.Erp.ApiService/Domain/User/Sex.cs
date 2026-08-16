using System.ComponentModel;

namespace Wuxi.Shengshen.Erp.ApiService.Domain.User;

/// <summary>
/// 性别（对应 Java pojo.enums.user.Sex；枚举值对齐 Java @EnumValue：MAN(1) / WOMAN(2)，
/// Java 端未定义"未知"，禁止揣测新增成员）。
/// DB 存数值；JSON 按数值读写（对齐 Java @JsonValue Integer id）；
/// 表头 EnumMap 由 TableHeaderBuilder 自动读取 <see cref="DescriptionAttribute"/> 描述。
/// </summary>
public enum Sex
{
    /// <summary>男（1）。</summary>
    [Description("男")]
    Man = 1,

    /// <summary>女（2）。</summary>
    [Description("女")]
    Woman = 2
}
