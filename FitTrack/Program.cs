using System.Text;
using FitTrack.Data;
using FitTrack.Data.Seed;
using FitTrack.DTOs.Common;
using FitTrack.Mapping;
using FitTrack.Model;
using FitTrack.Services;
using FitTrack.Services.Interfaces;
using FitTrack.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(entry => entry.Key, entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrEmpty(error.ErrorMessage)
                        ? "Invalid request value."
                        : error.ErrorMessage)
                    .ToArray());
            return new BadRequestObjectResult(new ErrorResponse(400, "Validation failed", errors));
        };
    });
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBodyMetricsService, BodyMetricsService>();
builder.Services.AddAutoMapper(options => options.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"], typeof(BodyMetricsProfile));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["Key"];
if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
{
    throw new InvalidOperationException("Set Jwt:Key to a secret of at least 32 characters.");
}

var tokenValidation = new TokenValidationParameters
{
    ValidIssuer = jwt["Issuer"],
    ValidAudience = jwt["Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateIssuerSigningKey = true,
    ValidateLifetime = true,
    RoleClaimType = "role",
    ClockSkew = TimeSpan.Zero
};
builder.Services.AddSingleton(tokenValidation);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = tokenValidation;
    });
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    await DbSeeder.SeedTrainerAsync(scope.ServiceProvider, app.Configuration);
}

app.Run();
