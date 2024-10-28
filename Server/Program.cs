using Server.Services;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<UploaderService>();
app.MapGrpcService<NotificationService>();


app.Run();