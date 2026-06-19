using BreastCancer.Community;
using BreastCancer.Community.Hubs;
using BreastCancer.Community.Services.Implementation;
using BreastCancer.Community.Services.Interface;
using BreastCancer.Context;
using BreastCancer.Hubs;
using BreastCancer.Mapping;
using BreastCancer.Models;
using BreastCancer.Options;
using BreastCancer.Repository.Interface;
using BreastCancer.Repository.Repositories;
using BreastCancer.Seeding;
using BreastCancer.Service.Implementation;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Rehla.Repository.Interface;
using Rehla.Repository.Repositories;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace BreastCancer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                       new System.Text.Json.Serialization.JsonStringEnumConverter()
                   );
                });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSignalR();

            #region Identity 
            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password Setting
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;

                // User Email Setting
                options.User.RequireUniqueEmail = true;

                // Email Confirm
                options.SignIn.RequireConfirmedEmail = true;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;

                // Lockout Settings
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

            }).AddEntityFrameworkStores<BreastCancerDB>()
            .AddDefaultTokenProviders();
            #endregion

            builder.Services.AddDbContext<BreastCancerDB>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("BreastCancer"));
            }, ServiceLifetime.Scoped);

            builder.Services.Configure<JwtOptions>
                (builder.Configuration.GetSection(JwtOptions.JwtOptionsKey));

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IPatientRepository, PatientRepository>();
            builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
            builder.Services.AddScoped<ICaregiverRepository, CaregiverRepository>();
            builder.Services.AddScoped<ITreatmentPlanRepository, TreatmentPlanRepository>();
            builder.Services.AddScoped<ICaregiverService, CaregiverService>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IAuthTokenService, AuthTokenService>();
            builder.Services.AddScoped<IPatientService, PatientService>();
            builder.Services.AddScoped<IDoctorService, DoctorService>();
            builder.Services.AddScoped<ITreatmentPlanService, TreatmentPlanService>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddHttpClient<IChatbotService, ChatbotService>();
            builder.Services.AddScoped<IPostVisibilityService, PostVisibilityService>();
            builder.Services.AddScoped<ICommentRepository,CommentRepository>();
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            builder.Services.AddCommunityModule(builder.Configuration);
            // Add CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy
                        .AllowAnyOrigin()    // or set specific frontend URL like "http://localhost:3000"
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            builder.Services.AddAuthorization();

            #region JWT Configuration
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {

                var jwtOptions = builder.Configuration.GetSection(JwtOptions.JwtOptionsKey)
                    .Get<JwtOptions>() ?? throw new ArgumentException(nameof(JwtOptions));

                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // Secret Key
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments(NotificationHub.HubRoute) ||
                             path.StartsWithSegments(CommunityHub.HubRoute)))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };

            });
            #endregion

            #region Swagger
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Breast Cancer API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
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

                // Enable annotations to show SwaggerResponse and SwaggerOperation descriptions
                c.EnableAnnotations();
                c.CustomSchemaIds(type => type.ToString());
            });
            #endregion



            var app = builder.Build();


            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BreastCancerDB>();
                await dbContext.Database.MigrateAsync();
            }

            await DataSeeder.SeedAsync(app.Services);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseCors("AllowAll");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationHub>(NotificationHub.HubRoute);
            app.MapCommunityModule();

            await app.RunAsync();
        }
    }
}