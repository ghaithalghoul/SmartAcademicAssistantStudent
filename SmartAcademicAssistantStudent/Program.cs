using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ──────────────────────────────────────────────
builder.Services.AddControllers();

// ─── OpenAPI ──────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ─── Database ─────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Services ─────────────────────────────────────────────────
builder.Services.AddScoped<ISmartChatService, SmartChatService>();
builder.Services.AddScoped<ICourseAdvisorService, CourseAdvisorService>();
builder.Services.AddSingleton<ICVMLService, CVMLService>();
builder.Services.AddScoped<ICVExtractorService, CVExtractorService>();
builder.Services.AddScoped<ICVEvaluationService, CVEvaluationService>();
builder.Services.AddScoped<StudyPlanService>();

// ─── JWT Authentication ───────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["AppSettings:Issuer"],
            ValidAudience = builder.Configuration["AppSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["AppSettings:Token"]!))
        };
    });

builder.Services.AddAuthorization();

// ─── CORS (اختياري — لو عندك Frontend) ───────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000",
                            "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ─── Pipeline ─────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication(); // ← لازم تكون قبل Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();