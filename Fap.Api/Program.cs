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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==================================================
// CONTROLLERS & SWAGGER
// ==================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger cấu hình JWT
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
// DATABASE & REPOSITORIES
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
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ISubjectOfferingRepository, SubjectOfferingRepository>();
builder.Services.AddScoped<ITimeSlotRepository, TimeSlotRepository>();
builder.Services.AddScoped<IEnrollRepository, EnrollRepository>();
builder.Services.AddScoped<IGradeRepository, GradeRepository>();
builder.Services.AddScoped<IGradeComponentRepository, GradeComponentRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ==================================================
// SETTINGS & SERVICES
// ==================================================
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));
builder.Services.Configure<BlockchainSettings>(builder.Configuration.GetSection("BlockchainSettings"));

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
builder.Services.AddScoped<IBlockchainService, BlockchainService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IGradeComponentService, GradeComponentService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IStudentRoadmapService, StudentRoadmapService>();

// Register AutoMapper - scan all Profile classes in the assembly
builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);


// ==================================================
// CORS CONFIGURATION
// ==================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
      .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // PRODUCTION: Chỉ cho phép domain cụ thể
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
       "http://localhost:3000",
     "http://localhost:4200",
     "http://localhost:5173",
   "http://localhost:8080",
          "https://yourdomain.com",
   "https://www.yourdomain.com"
   )
        .AllowAnyMethod()
    .AllowAnyHeader()
 .AllowCredentials();
    });
});

// ==================================================
// JWT AUTHENTICATION
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
      Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key configuration is missing")))
        };
    });

builder.Services.AddAuthorization();

// ==================================================
// BUILD APP
// ==================================================
var app = builder.Build();

// Apply migrations & seed data safely
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FapDbContext>();
    try
    {
        Console.WriteLine("==============================================");
        Console.WriteLine("🔄 Database initialization started...");
        Console.WriteLine("==============================================");
        
        // Check if --force-seed argument is provided
        bool forceSeed = args.Contains("--force-seed");
        
        if (forceSeed)
        {
            Console.WriteLine("⚠️  --force-seed detected: Dropping database...");
            await db.Database.EnsureDeletedAsync();
  Console.WriteLine("✅ Database dropped successfully");
        }
  
        Console.WriteLine("🔄 Applying migrations...");
        await db.Database.MigrateAsync();
        Console.WriteLine("✅ Migrations applied successfully");
        
  Console.WriteLine("");
        await DataSeeder.SeedAsync(db);
        Console.WriteLine("");
        
     Console.WriteLine("==============================================");
        Console.WriteLine("✅ Database initialization completed!");
        Console.WriteLine("==============================================");
    }
    catch (Exception ex)
    {
        Console.WriteLine("==============================================");
      Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
Console.WriteLine($"Stack trace: {ex.StackTrace}");
        Console.WriteLine("==============================================");
    Console.WriteLine("⚠️  Continuing app startup without seeding...");
    }
}

// ==================================================
// MIDDLEWARE PIPELINE
// ==================================================

// ✅ BẬT SWAGGER CHO CẢ PRODUCTION (Azure)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UAP API v1");
    c.RoutePrefix = "swagger"; // URL: /swagger
});

// ==================================================
// USE CORS (MUST BE BEFORE Authentication & Authorization)
// ==================================================
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("Production");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
