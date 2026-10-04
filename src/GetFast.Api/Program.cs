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

var app = builder.Build();

if (string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException(
        "Укажите ConnectionStrings:DefaultConnection через переменную окружения ConnectionStrings__DefaultConnection.");
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<GetFastDbContext>().Database.MigrateAsync();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthEndpoint();
app.MapAuthEndpoints();
app.Run();

public partial class Program;
