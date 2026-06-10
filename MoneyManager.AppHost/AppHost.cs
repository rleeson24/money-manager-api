var builder = DistributedApplication.CreateBuilder(args);

// Ephemeral DB: persistent volume can leave SQL in a bad state after abrupt Docker stops.
var sql = builder.AddSqlServer("sql")
	.AddDatabase("DefaultConnection");

builder.AddProject<Projects.MoneyManager_API>("moneymanager-api")
	.WithReference(sql)
	.WithEnvironment("Data__AspireOrchestrated", "true");

#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
// 6548 is in Windows excluded range 6454-6553 (Hyper-V/Docker); binding it causes EACCES. Use 5173 (Vite default).
builder.AddViteApp("money-manager-client", "../../money-manager-client")
	.WithEndpoint("http", ep =>
	{
		ep.Port = 5173;
		ep.TargetPort = 5173;
		ep.IsProxied = false;
	})
	.WithHttpsDeveloperCertificate();
#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.Build().Run();
