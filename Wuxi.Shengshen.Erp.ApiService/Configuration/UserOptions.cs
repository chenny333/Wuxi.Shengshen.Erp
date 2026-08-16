namespace Wuxi.Shengshen.Erp.ApiService.Configuration;

/// <summary>
/// 用户模块配置（对应 Java DefaultPasswordParam 等可调参数；Java 端硬编码在代码里，
/// 迁移时改为 appsettings 可配，避免改默认值需要重新发版）。
/// 绑定 appsettings.json 的 <c>User</c> 节。
/// </summary>
public sealed class UserOptions
{
    /// <summary>配置节名（appsettings.json 顶层 "User" 节）。</summary>
    public const string SectionName = "User";

    /// <summary>
    /// 创建用户时的默认初始密码明文（对齐 Java DefaultPasswordParam.defaultPwd = "qwer1234"）；
    /// 入库前由 Service 层按 <c>PasswordUtil.Encode(用户雪花ID, 明文)</c> 计算 BCrypt 哈希，明文本身不落库。
    /// </summary>
    public string DefaultPassword { get; set; } = "qwer1234";
}
