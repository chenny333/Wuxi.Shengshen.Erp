namespace Wuxi.Shengshen.Erp.ApiService.Security;

/// <summary>
/// 安全配置项（对应 Java security.* 配置）。
/// </summary>
public sealed class SecurityOptions
{
    /// <summary>JWT HS256 密钥。</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>token 请求头名。</summary>
    public string Header { get; set; } = "Authorization";

    /// <summary>token 前缀（后随一个空格）。</summary>
    public string Prefix { get; set; } = "Bearer";

    /// <summary>Redis token 有效期（小时）。</summary>
    public int ExpireHours { get; set; } = 2400;

    /// <summary>单设备登录开关（true 时 JWT 写 nbf）。</summary>
    public bool SingleSession { get; set; }
}