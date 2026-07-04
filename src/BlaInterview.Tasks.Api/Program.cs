using BlaInterview.Application;
using BlaInterview.Infrastructure;
using BlaInterview.Infrastructure.Seeding;
using BlaInterview.Api.Shared;
using BlaInterview.Api.Shared.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedWebApi();
builder.Services.AddTasksApplication();
builder.Services.AddTasksInfrastructure(builder.Configuration);
builder.Services.ConfigureSwaggerJwtBearer(
    "Simple Tasks API",
    "Personal Kanban task board — CRUD and workflow endpoints (JWT required).");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    await TaskDatabaseSeeder.SeedAsync(app.Services);
}

app.UseSharedWebApi();
app.Run();

public partial class TasksApiProgram;
