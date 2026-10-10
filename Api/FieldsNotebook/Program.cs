using System.Text.Json.Serialization;
using Accessors.Users;
using Database;
using Managers.Users;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Supply the value through user-secrets or an environment variable; never commit it.
var connectionString = builder.Configuration.GetConnectionString("FieldNotebook")
    ?? throw new InvalidOperationException("Connection string 'FieldNotebook' is not configured.");
builder.Services.AddDbContext<FieldNotebookContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IUserAccessor, UserAccessor>();
builder.Services.AddScoped<IUserManager, UserManager>();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
