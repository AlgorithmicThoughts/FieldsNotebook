using System.Text.Json.Serialization;
using Managers;
using Database;
using FieldsNotebook.Controllers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Supply the value through user-secrets or an environment variable; never commit it.
var connectionString = builder.Configuration.GetConnectionString("FieldNotebook")
    ?? throw new InvalidOperationException("Connection string 'FieldNotebook' is not configured.");
builder.Services.AddDbContext<FieldNotebookContext>(options => options.UseSqlServer(connectionString));

ManagerFactory.RegisterServices(builder.Services);

// Development only: lets the Vite dev server call the API from the browser.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).WithMethods("GET", "POST").WithHeaders(DemoIdentity.HeaderName, "Content-Type")));

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
// Strict numbers keep integer parameters typed as "integer" in OpenAPI; Swagger UI cannot validate ["integer","string"].
// OpenAPI reads the HTTP JSON options and controllers read the MVC ones, so set both.
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FieldsNotebook API v1");
    });
}

app.UseCors();

if (!app.Environment.IsDevelopment())
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
