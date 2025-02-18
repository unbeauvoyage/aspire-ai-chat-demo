var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddChatClient("llm");

builder.AddCosmosDbContext<AppDbContext>("conversations", "db");

builder.Services.AddSingleton<ChatStreamingCoordinator>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapChatApi();

app.Run();
