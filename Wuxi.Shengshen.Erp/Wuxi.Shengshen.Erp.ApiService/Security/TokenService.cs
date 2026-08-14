using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Wuxi.Shengshen.Erp.ApiService.Security;

/// <summary>
/// 令牌服务（对应 Java TokenUtil）：JWT(HS256) + Redis 单点会话。
/// JWT 不设 exp，过期完全由 Redis TTL 控制；单点效果靠 Redis 同 key 覆盖 + 字符串比对。
/// </summary>
public sealed class TokenService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly SecurityOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();

    /// <summary>
    /// 注入 Redis 与安全配置。
    /// </summary>
    public TokenService(IConnectionMultiplexer redis, IOptions<SecurityOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    /// <summary>
    /// 签发令牌并写入 Redis（覆盖旧 token 实现单点）。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    /// <returns>JWT 字符串（不含前缀）。</returns>
    public async Task<string> IssueTokenAsync(long userId)
    {
        var claims = new List<Claim>
        {
            new("id", userId.ToString()),
            new("loginType", AuthConstants.LoginType)
        };
        if (_options.SingleSession)
        {
            claims.Add(new Claim("nbf", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(claims: claims, signingCredentials: creds);
        var tokenString = _handler.WriteToken(token);

        var db = _redis.GetDatabase();
        await db.StringSetAsync(
            AuthConstants.TokenKeyPrefix + userId,
            tokenString,
            TimeSpan.FromHours(_options.ExpireHours));
        return tokenString;
    }

    /// <summary>
    /// 从请求头解析并校验令牌，返回用户 ID；未认证/被踢返回 null。
    /// </summary>
    /// <param name="request">HTTP 请求。</param>
    public async Task<long?> GetAuthenticatedUserIdAsync(HttpRequest request)
    {
        var header = request.Headers[_options.Header].FirstOrDefault();
        if (string.IsNullOrEmpty(header)) return null;

        var prefix = _options.Prefix + " ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var tokenString = header[prefix.Length..];

        // 验签（不校验过期，过期由 Redis TTL 控制）
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        try
        {
            _handler.ValidateToken(tokenString, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out var validated);

            if (validated is not JwtSecurityToken jwt) return null;
            var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (!long.TryParse(idClaim, out var userId)) return null;

            // Redis 单点会话：比对存储 token 与请求 token 是否完全一致
            var db = _redis.GetDatabase();
            var stored = await db.StringGetAsync(AuthConstants.TokenKeyPrefix + userId);
            if (stored.IsNullOrEmpty || stored.ToString() != tokenString) return null;
            return userId;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 移除令牌（踢下线）。
    /// </summary>
    /// <param name="userId">用户 ID。</param>
    public async Task RemoveTokenAsync(long userId) =>
        await _redis.GetDatabase().KeyDeleteAsync(AuthConstants.TokenKeyPrefix + userId);
}