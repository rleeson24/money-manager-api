var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MoneyManager_API>("moneymanager-api");

#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.AddViteApp("money-manager-client", "../../money-manager-client")
	.WithHttpsEndpoint(env: "PORT") // Sets the PORT env var for Vite
	.WithHttpsDeveloperCertificate();
#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.Build().Run();
