var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MoneyManager_API>("moneymanager-api");

builder.AddNpmApp("money-manager-client", "../../money-manager-client", "dev")
    .WithHttpEndpoint(port: 5173, name: "http", env: "PORT");

builder.Build().Run();
