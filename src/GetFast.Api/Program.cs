using System.Text.Json.Serialization;
using GetFast.Api.Auth;
using GetFast.Api.Data;
using GetFast.Api.Endpoints;
using GetFast.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GetFastDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IConfiguration>()
        .GetConnectionString("DefaultConnection")));
builder.Services.AddIdentityCore<AppUser>(options => options.User.RequireUniqueEmail = true)
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<GetFastDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.AddSwaggerGen();
builder.Services.AddGetFastAuthentication();

var app = builder.Build();

if (string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException(
        "Укажите ConnectionStrings:DefaultConnection через переменную окружения ConnectionStrings__DefaultConnection.");
}

_ = app.Services.GetRequiredService<JwtSettings>();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<GetFastDbContext>().Database.MigrateAsync();
    await DevelopmentSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.MapHealthEndpoint();
app.MapAuthEndpoints();
app.Run();

public partial class Program;
