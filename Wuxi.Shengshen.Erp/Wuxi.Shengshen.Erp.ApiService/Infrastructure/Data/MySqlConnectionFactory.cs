using MySqlConnector;

namespace Wuxi.Shengshen.Erp.ApiService.Infrastructure.Data;

/// <summary>
/// MySQL 连接工厂。优先读取 <c>ConnectionStrings:MySql</c>，找不到再回退 <c>ConnectionStrings:MySQL</c>。
/// </summary>
public sealed class MySqlConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// 从配置读取连接串（优先 <c>ConnectionStrings:MySql</c>）。
    /// </summary>
    /// <param name="configuration">应用配置。</param>
    public MySqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MySql")
            ?? configuration.GetConnectionString("MySQL")
            ?? throw new InvalidOperationException("未配置连接串 ConnectionStrings:MySql");
    }

    /// <summary>
    /// 创建一个已打开的 MySQL 连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}