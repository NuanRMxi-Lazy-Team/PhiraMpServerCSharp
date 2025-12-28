using Microsoft.AspNetCore.Cors.Infrastructure;
using PhiraMpServerCSharpWebApi.Configuration;
using PhiraMpServerCSharpWebApi.Services;
using WebSocketManager = PhiraMpServerCSharpWebApi.Services.WebSocketManager;

var builder = WebApplication.CreateBuilder(args);

// Configure PhiraMpServers options
builder.Services.Configure<PhiraMpServersOptions>(options =>
{
    var servers = builder.Configuration.GetSection("PhiraMpServers").Get<List<PhiraMpServerOption>>();
    if (servers != null)
    {
        options.Servers = servers;
    }
});

// Register PhiraMpClientService as singleton and hosted service
builder.Services.AddSingleton<PhiraMpClientService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PhiraMpClientService>());

// Register WebSocket manager and message forwarder
builder.Services.AddSingleton<WebSocketManager>();
builder.Services.AddSingleton<MessageForwarderService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MessageForwarderService>());

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure WebSocket options
builder.Services.Configure<WebSocketOptions>(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(120);
    // options.ReceiveBufferSize = 4096; // This property is obsolete
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors();
}

// Enable WebSockets
app.UseWebSockets();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

