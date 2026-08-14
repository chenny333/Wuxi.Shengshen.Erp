var builder = DistributedApplication.CreateBuilder(args);

// Redis：会话/分布式锁/验证码。
var cache = builder.AddRedis("Redis");

// MySQL：业务主库（连接串名固定为 MySql，与 MySqlConnectionFactory 读取的 key 对齐）。
var database = builder.AddMySql("MySql")
    .WithDatabase("wuxi_erp");

var apiService = builder.AddProject<Projects.Wuxi_Shengshen_Erp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.Wuxi_Shengshen_Erp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();