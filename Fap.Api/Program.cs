using Fap.Api.Filters;
using Fap.Api.Interfaces;
using Fap.Api.Mappings;
using Fap.Api.Services;
using Fap.Domain.Repositories;
using Fap.Domain.Settings;
using Fap.Infrastructure.Data;
using Fap.Infrastructure.Data.Seed;
using Fap.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==================================================
// 🔹 CONTROLLERS & SWAGGER
// ==================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ Swagger cấu hình JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UAP API",
        Version = "v1",
        Description = "University Academic & Student Management on Blockchain"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.OperationFilter<SwaggerAuthorizeOperationFilter>();
});

// ==================================================
// 🔹 DATABASE & REPOSITORIES
// ==================================================
builder.Services.AddDbContext<FapDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository pattern
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();  
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();  
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();  
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();  // ✅ NEW
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ==================================================
// 🔹 SETTINGS & SERVICES
// ==================================================
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStudentService, StudentService>();  
builder.Services.AddScoped<ITeacherService, TeacherService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();  // ✅ NEW
builder.Services.AddScoped<ISemesterService, SemesterService>();  // ✅ NEW

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(AutoMapperProfile)));

// ==================================================
// 🔹 JWT AUTHENTICATION
// ==================================================
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

// ==================================================
// 🔹 BUILD APP
// ==================================================
var app = builder.Build();

// Apply migrations & seed data safely
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FapDbContext>();
    try
    {
        Console.WriteLine("🔄 Applying migrations...");
        await db.Database.MigrateAsync();
        await DataSeeder.SeedAsync(db);
        Console.WriteLine("✅ Database migration & seeding done!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Database migration failed: {ex.Message}");
        Console.WriteLine("➡️ Skipping migration, continuing app startup...");
    }
}

// ==================================================
// 🔹 MIDDLEWARE PIPELINE
// ==================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
