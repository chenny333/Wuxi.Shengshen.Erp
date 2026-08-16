namespace Wuxi.Shengshen.Erp.ApiService.Constants.Auth;

/// <summary>
/// 登录模块错误消息常量（经业务异常信封返回，前端 toast 直接展示；措辞与 Java 端保持一致）。
/// </summary>
public static class AuthErrorMessages
{
    /// <summary>登录防重提交锁未获取到（同一账号 2 秒内仅允许一次尝试）。</summary>
    public const string LoginTooFrequent = "登录请求过于频繁，请稍后重试";

    /// <summary>验证码校验失败或已过期（验证码校验即删，不可重放）。</summary>
    public const string CaptchaInvalidOrExpired = "验证码错误或已过期";

    /// <summary>账号不存在或已逻辑删除（不区分两种情形，防账号枚举）。</summary>
    public const string UserNotFound = "用户不存在";

    /// <summary>账号已被禁用。</summary>
    public const string UserDisabled = "用户已封禁，请联系管理员";

    /// <summary>密码校验失败。</summary>
    public const string PasswordWrong = "密码错误";
}
