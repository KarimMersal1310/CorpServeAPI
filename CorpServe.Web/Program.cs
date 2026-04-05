using AutoMapper;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Presistence.Data.DataSeed;
using CorpServe.Presistence.Data.DbContext;
using CorpServe.Services;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Mapping;
using CorpServe.Domain.Contracts;
using CorpServe.Web.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ToDoManagementAPI.CustomMiddleWare;
using ToDoManagementAPI.Factories;
using CorpServe.Presistence.Repository;
using CorpServe.Web.Hubs;
using CorpServe.Web.RealTime;

namespace CorpServe.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy
                            .SetIsOriginAllowed(origin =>
                            {
                                var uri = new Uri(origin);

                                return uri.Host == "localhost"
                                    || origin == "https://corp-serve-frontend.vercel.app";
                            })
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });
            builder.Services.AddDbContext<CorpServeDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services.AddKeyedScoped<IDataInitializer, DataInitializer>("Default");
            builder.Services.AddKeyedScoped<IDataInitializer, IdentityDataInitializer>("Identity");
            builder.Services.AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<CorpServeDbContext>()
                .AddDefaultTokenProviders();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IVendorVerifyService, VendorVerifyService>();
            builder.Services.AddScoped<IAdminVendorService, AdminVendorService>();
            builder.Services.AddHttpClient<IAIEstimationService, AIEstimationService>();
            builder.Services.AddScoped<IRequestService, RequestService>();
            builder.Services.AddScoped<IProposalService, ProposalService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
            builder.Services.AddHostedService<SLAStatusMonitorBackgroundService>();
            builder.Services.AddSingleton(_ =>
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<VendorVerifyProfile>();
                    cfg.AddProfile<CategoryProfile>();
                    cfg.AddProfile<RequestProfile>();
                    cfg.AddProfile<ProposalProfile>();
                });
                return config.CreateMapper();
            });

            builder.Services.Configure<ApiBehaviorOptions>(opt =>
            {
                opt.InvalidModelStateResponseFactory = ApiResponseFactory.GenerateValidationResponse;
            });

            var jwtSecretKey = builder.Configuration["JWTOptions:SecretKey"];
            if (string.IsNullOrWhiteSpace(jwtSecretKey))
                throw new InvalidOperationException("Missing JWT secret key. Configure 'JWTOptions:SecretKey' (or environment variable 'JWTOptions__SecretKey').");

            builder.Services.AddAuthentication(Options =>
            {
                Options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                Options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(Options =>
            {
                Options.SaveToken = true;
                Options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/notifications"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
                Options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["JWTOptions:Issuer"],
                    ValidAudience = builder.Configuration["JWTOptions:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)
                    )
                };
            });
            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(30);
            });
            var app = builder.Build();
            #endregion

            #region Data Seeding
            await app.MigrateDatabaseAsync();
            await app.SeedDatabaseAsync();
            await app.SeedIdentityDatabaseAsync();
            #endregion

            #region Configure the HTTP request pipeline.
            app.UseMiddleware<ExceptionHandlerMiddleWare>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowFrontend");

            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();
            app.MapHub<NotificationsHub>("/hubs/notifications");
            #endregion

            app.Run();
        }
    }
}
