namespace Wuxi.Shengshen.Erp.ApiService.Security;

/// <summary>
/// 密码工具（对应 Java PasswordUtil）：BCrypt，输入 = 用户 id 与明文密码拼接，兼容现有 $2a$ 哈希数据。
/// </summary>
public static class PasswordUtil
{
    /// <summary>
    /// 加密密码：BCrypt(id + 明文)。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="password">明文密码。</param>
    public static string Encode(long id, string password) =>
        BCrypt.Net.BCrypt.HashPassword($"{id}{password}");

    /// <summary>
    /// 校验密码：BCrypt.matches(id + 明文, 哈希)。
    /// </summary>
    /// <param name="id">用户 ID。</param>
    /// <param name="password">明文密码。</param>
    /// <param name="hash">BCrypt 哈希。</param>
    public static bool Matches(long id, string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify($"{id}{password}", hash);
        }
        catch
        {
            return false;
        }
    }
}