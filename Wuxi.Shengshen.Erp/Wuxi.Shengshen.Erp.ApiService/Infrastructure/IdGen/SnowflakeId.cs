namespace Wuxi.Shengshen.Erp.ApiService.Infrastructure.IdGen;

/// <summary>
/// 全局雪花 ID 生成器（对应 Java 侧 MyBatis-Plus ASSIGN_ID）。进程内单例，ID 非自增。
/// </summary>
public static class SnowflakeId
{
    private static readonly KingV.Core.Snowflake.IdWorker Worker = new(workerId: 1, datacenterId: 1);

    /// <summary>
    /// 生成下一个全局唯一 ID。
    /// </summary>
    public static long NextId() => Worker.NextId();
}