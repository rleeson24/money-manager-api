var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MoneyManager_API>("moneymanager-api");

builder.Build().Run();
