using System.Text;
using System.Threading.RateLimiting;
using Kinof.Api.Data;
using Kinof.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, app.Configuration, app.Environment);
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

var auth = app.MapGroup("/api/auth").RequireRateLimiting("auth");
auth.MapPost("/register", (
    RegisterRequest request,
    AuthService service,
    CancellationToken cancellationToken) =>
    service.RegisterAsync(request, cancellationToken));
auth.MapPost("/login", (
    LoginRequest request,
    AuthService service,
    CancellationToken cancellationToken) =>
    service.LoginAsync(request, cancellationToken));
auth.MapPost("/verify-email-otp", (
    VerifyEmailOtpRequest request,
    AuthService service,
    CancellationToken cancellationToken) =>
    service.VerifyOtpAsync(request, cancellationToken));
auth.MapPost("/resend-email-otp", (
    ResendEmailOtpRequest request,
    AuthService service,
    CancellationToken cancellationToken) =>
    service.ResendOtpAsync(request, cancellationToken));

app.Run();
