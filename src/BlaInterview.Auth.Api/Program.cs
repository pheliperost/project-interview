using BlaInterview.Application;
using BlaInterview.Infrastructure;
using BlaInterview.Infrastructure.Seeding;
using BlaInterview.Api.Shared;
using BlaInterview.Api.Shared.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedWebApi();
builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration);
builder.Services.ConfigureSwaggerJwtBearer(
    "Simple Tasks Auth API",
    "User registration, login, and JWT issuance.");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    await UserDatabaseSeeder.SeedAsync(app.Services);
}

app.UseSharedWebApi();
app.Run();

public partial class AuthApiProgram;
