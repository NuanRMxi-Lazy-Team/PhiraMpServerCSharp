using PhiraMpServerCSharpWebApi.Configuration;
using PhiraMpServerCSharpWebApi.Services;

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

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

