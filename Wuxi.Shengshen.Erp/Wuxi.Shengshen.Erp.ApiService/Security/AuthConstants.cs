namespace Wuxi.Shengshen.Erp.ApiService.Security;

/// <summary>
/// 鉴权常量（对应 Java AuthConstants / LoginServiceImpl 内部常量）。
/// </summary>
public static class AuthConstants
{
    /// <summary>后台登录类型标识（写入 JWT 的 loginType 声明）。</summary>
    public const string LoginType = "wuxi-erp";

    /// <summary>Redis token 键前缀。</summary>
    public const string TokenKeyPrefix = "wuxi:token:";

    /// <summary>验证码 Redis 键前缀。</summary>
    public const string CaptchaKeyPrefix = "wuxi:captcha:";
}