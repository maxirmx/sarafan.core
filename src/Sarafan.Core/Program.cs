// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using Sarafan.Core;
using Sarafan.Core.Authentication;
using Sarafan.Core.Data;
using Sarafan.Core.Services;

var builder = WebApplication.CreateBuilder(args);
var migrateOnly = args.Contains("--migrate-only", StringComparer.Ordinal);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required");
var authentication = builder.Configuration
    .GetSection(AuthenticationOptions.SectionName)
    .Get<AuthenticationOptions>()
    ?? throw new InvalidOperationException("Authentication configuration is required");

authentication.Validate();

builder.Services
    .AddOptions<AuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(AuthenticationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<SarafanProblemDetailsFactory>();
builder.Services.AddExceptionHandler<SarafanExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = true;
    options.InvalidModelStateResponseFactory = context => context.HttpContext
        .RequestServices
        .GetRequiredService<SarafanProblemDetailsFactory>()
        .CreateValidationResult(context.HttpContext, context.ModelState);
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<VerificationAttemptStore>();
builder.Services.AddSingleton<IPhoneNormalizer, PhoneNormalizer>();
builder.Services.AddSingleton<IVerificationCodeProvider, PhoneSuffixVerificationCodeProvider>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthenticationService>();

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authentication.SigningKey));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = JwtTokenService.CreateValidationParameters(authentication, signingKey);
        options.Events = new SarafanJwtBearerEvents();
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Sarafan Core Api", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization token. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    ForwardedHeadersConfiguration.Configure(options, builder.Configuration));

var app = builder.Build();

if (migrateOnly || builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await database.Database.MigrateAsync();
}

if (migrateOnly)
{
    return;
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    var httpContext = context.HttpContext;
    var factory = httpContext.RequestServices.GetRequiredService<SarafanProblemDetailsFactory>();
    await factory.WriteAsync(
        httpContext,
        httpContext.Response.StatusCode,
        SarafanProblemDetailsFactory.CodeForStatus(httpContext.Response.StatusCode),
        httpContext.RequestAborted);
});
app.UseAuthentication();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/api/v1/status/status"));

app.MapControllers();

app.Run();

public partial class Program;
