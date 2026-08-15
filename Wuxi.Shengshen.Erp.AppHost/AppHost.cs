var builder = DistributedApplication.CreateBuilder(args);

// Redis / MySQL：直接消费 ApiService user-secrets/appsettings 里的 ConnectionStrings，
// AppHost 不再创建本地容器（外网已有阿里云 Redis 与远程 MySQL）。
var redis = builder.AddConnectionString("Redis");
var mysql = builder.AddConnectionString("MySql");

var apiService = builder.AddProject<Projects.Wuxi_Shengshen_Erp_ApiService>("apiservice", "Wuxi.Shengshen.Erp.ApiService")
    .WithHttpHealthCheck("/health")
    .WithReference(redis)
    .WithReference(mysql);

//builder.AddProject<Projects.Wuxi_Shengshen_Erp_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithHttpHealthCheck("/health")
//    .WithReference(apiService)
//    .WaitFor(apiService);

builder.Build().Run();