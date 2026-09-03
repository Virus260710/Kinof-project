using System.Text;
using System.Threading.RateLimiting;
using System.Security.Claims;
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
builder.Services.Configure<FaceServiceOptions>(builder.Configuration.GetSection("FaceService"));
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddHttpClient<IFaceEmbeddingClient, FaceEmbeddingClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<FaceServiceOptions>>()
        .Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BookingService>();
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
auth.MapPost("/refresh", (
    RefreshTokenRequest request,
    AuthService service,
    CancellationToken cancellationToken) =>
    service.RefreshAsync(request, cancellationToken));
auth.MapPost("/forgot-password", (
    ForgotPasswordRequest request,
    AuthService service,
    CancellationToken cancellationToken) =>
    service.ForgotPasswordAsync(request, cancellationToken));
auth.MapPost("/reset-password", (
    ResetPasswordRequest request,
    AuthService service,
    CancellationToken cancellationToken) =>
    service.ResetPasswordAsync(request, cancellationToken));
auth.MapGet("/me", (
    ClaimsPrincipal user,
    AuthService service,
    CancellationToken cancellationToken) =>
    service.GetMeAsync(user, cancellationToken)).RequireAuthorization();
auth.MapPost("/register/face", (
    RegisterFaceRequest request,
    ClaimsPrincipal user,
    AuthService service,
    CancellationToken cancellationToken) =>
{
    var userId = AuthService.GetUserId(user);
    return userId is null
        ? Task.FromResult(Results.Unauthorized())
        : service.RegisterFaceAsync(userId.Value, request, cancellationToken);
}).RequireAuthorization();

var bookings = app.MapGroup("/api/bookings").RequireAuthorization();
bookings.MapGet("/me", (
    ClaimsPrincipal user,
    BookingService service,
    CancellationToken cancellationToken) =>
{
    var userId = AuthService.GetUserId(user);
    return userId is null
        ? Task.FromResult(Results.Unauthorized())
        : service.GetMyBookingsAsync(userId.Value, cancellationToken);
});
bookings.MapPost("/", (
    CreateBookingRequest request,
    ClaimsPrincipal user,
    BookingService service,
    CancellationToken cancellationToken) =>
{
    var userId = AuthService.GetUserId(user);
    return userId is null
        ? Task.FromResult(Results.Unauthorized())
        : service.CreateBookingAsync(userId.Value, request, cancellationToken);
});

var rooms = app.MapGroup("/api/rooms").RequireAuthorization();
rooms.MapGet("/", (
    BookingService service,
    CancellationToken cancellationToken) =>
    service.GetRoomsAsync(cancellationToken));
rooms.MapGet("/available", (
    DateTime startTime,
    DateTime endTime,
    BookingService service,
    CancellationToken cancellationToken) =>
    service.GetAvailableRoomsAsync(startTime, endTime, cancellationToken));

app.Run();
