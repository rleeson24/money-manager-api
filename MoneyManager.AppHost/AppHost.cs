var builder = DistributedApplication.CreateBuilder(args);

// Ephemeral DB: persistent volume can leave SQL in a bad state after abrupt Docker stops.
var sql = builder.AddSqlServer("sql")
	.AddDatabase("DefaultConnection");

builder.AddProject<Projects.MoneyManager_API>("moneymanager-api")
	.WithReference(sql)
	.WithEnvironment("Data__UseMockData", "false")
	.WithEnvironment("Data__AspireOrchestrated", "true");

#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.AddViteApp("money-manager-client", "../../money-manager-client")
	.WithHttpsEndpoint(port: 6548, env: "PORT") // Fixed host port; PORT is injected for Vite binding
	.WithHttpsDeveloperCertificate();
#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.Build().Run();
