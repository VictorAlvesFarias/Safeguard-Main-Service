using Microsoft.EntityFrameworkCore;
using ASP.NET_Core_Template.Ioc;
using Infrastructure.Context;
using PicEnfermagem.Api.Extensions;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using App.Extensions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddSwagger();

builder.Services.RegisterServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedCorsOrigins",
    builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

builder.Services.AddAuthentication(builder.Configuration);

builder.Services.AddDbContext<ApplicationContext>(opt =>
{
    //var connectionString = $"{configuration.GetConnectionString("DefaultConnection")}Password={Environment.GetEnvironmentVariable("DEVELOPMENT_DATABASE_KEY")};";
    var connectionString = $"{builder.Configuration.GetConnectionString("DefaultConnection")};";

    opt.UseNpgsql(connectionString);
});

builder.WebHost.ConfigureKestrel(options =>
{
    var httpPort = builder.Configuration.GetValue<int>("Ports:Http");
    var httpsPort = builder.Configuration.GetValue<int>("Ports:Https");

    // In a container the API must listen on every interface over plain HTTP: there is no
    // development certificate and localhost is unreachable from outside (Ports__ListenAnyIp=true).
    if (builder.Configuration.GetValue<bool>("Ports:ListenAnyIp"))
    {
        options.ListenAnyIP(httpPort);
        return;
    }

    options.ListenLocalhost(httpsPort, configure =>
    {
        configure.UseHttps();
    });
    options.ListenLocalhost(httpPort);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ApplicationContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowedCorsOrigins");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

namespace App.Program
{
    public partial class Program { }
}
