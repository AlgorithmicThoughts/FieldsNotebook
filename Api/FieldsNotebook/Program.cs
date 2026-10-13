using System.Text.Json.Serialization;
using Managers;
using FieldsNotebook.Controllers;
using FieldsNotebook.ErrorHandling;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Supply ApplicationInsights:ConnectionString through user-secrets or APPLICATIONINSIGHTS_CONNECTION_STRING; never commit it.
// Without one, ILogger output stays on the console so local runs do not need an Application Insights resource.
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
    ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (string.IsNullOrWhiteSpace(appInsightsConnectionString) is false)
{
    builder.Services.AddApplicationInsightsTelemetry(options => options.ConnectionString = appInsightsConnectionString);
}

// Managers register every layer below them, including the database and the storage and speech services.
ManagerFactory.RegisterServices(builder.Services, builder.Configuration);

// Development only: lets the Vite dev server call the API from the browser.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).WithMethods("GET", "POST", "PUT", "PATCH").WithHeaders(DemoIdentity.HeaderName, "Content-Type")));

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
// Strict numbers keep integer parameters typed as "integer" in OpenAPI; Swagger UI cannot validate ["integer","string"].
// OpenAPI reads the HTTP JSON options and controllers read the MVC ones, so set both.
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddControllers(options =>
    {
        options.Filters.Add(new ProducesAttribute("application/json"));
    })
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ApiErrorResponse.FromInvalidModelState)
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

if (app.Environment.IsDevelopment() is false)
    app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
