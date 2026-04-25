using AutoMapper;
using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Presistence.Data.DataSeed;
using CorpServe.Presistence.Data.DbContext;
using CorpServe.Services;
using CorpServe.Services.Abstraction;
using CorpServe.Services.Mapping;
using CorpServe.Domain.Contracts;
using CorpServe.Web.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using ToDoManagementAPI.CustomMiddleWare;
using ToDoManagementAPI.Factories;
using CorpServe.Presistence;
using CorpServe.Presistence.Queries;
using CorpServe.Presistence.Repository;
using CorpServe.Services.Payments;
using CorpServe.Web.Hubs;
using CorpServe.Web.RealTime;
using System.Net.Http.Headers;

namespace CorpServe.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            #region Add services to the container.
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
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
                                if (string.IsNullOrWhiteSpace(origin))
                                    return false;
                                var uri = new Uri(origin);
                                if (uri.Scheme != "https" && uri.Host != "localhost")
                                    return false;

                                return uri.Host == "localhost"
                                    || uri.Host == "127.0.0.1"
                                    || origin == "https://corp-serve-frontend.vercel.app"
                                    || origin == "https://corpserve.works"
                                    || origin == "https://www.corpserve.works"
                                    || (uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase) && uri.Scheme == "https");
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
            builder.Services.AddScoped<IVendorVerificationQuery, VendorVerificationQuery>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ICategoryDataQueries, CategoryDataQueries>();
            builder.Services.AddScoped<IVendorVerifyService, VendorVerifyService>();
            builder.Services.AddScoped<IAdminVendorService, AdminVendorService>();
            builder.Services.AddScoped<IAdminMonitorService, AdminMonitorService>();
            builder.Services.AddHttpClient<IAIEstimationService, AIEstimationService>();
            var paymobSection = builder.Configuration.GetSection("Paymob");
            var paymobOptions = paymobSection.Get<PaymobOptions>() ?? throw new InvalidOperationException("Missing Paymob configuration section.");
            ValidatePaymobOptions(paymobOptions);
            builder.Services.Configure<PaymobOptions>(paymobSection);
            builder.Services.AddHttpClient<IPaymobClient, PaymobClient>(client =>
            {
                var baseUrl = paymobOptions.BaseUrl.TrimEnd('/') + "/";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(paymobOptions.TimeoutSeconds <= 0 ? 30 : paymobOptions.TimeoutSeconds);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", $"Token {paymobOptions.SecretKey}");
            });
            builder.Services.AddScoped<IRequestService, RequestService>();
            builder.Services.AddScoped<IProposalService, ProposalService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IRatingService, RatingService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IChatRealtimeNotifier, SignalRChatNotifier>();
            builder.Services.AddHostedService<SLAStatusMonitorBackgroundService>();
            builder.Services.AddHostedService<NotificationCleanupBackgroundService>();
            builder.Services.AddHostedService<PaymentOverdueBackgroundService>();
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
                        var path = context.Request.Path;
                        if (!path.StartsWithSegments("/hubs/notifications") && !path.StartsWithSegments("/hubs/chat"))
                            return Task.CompletedTask;

                        var accessToken = context.Request.Query["access_token"].ToString();
                        if (string.IsNullOrWhiteSpace(accessToken))
                        {
                            var authHeader = context.Request.Headers.Authorization.ToString();
                            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                accessToken = authHeader["Bearer ".Length..].Trim();
                        }

                        if (!string.IsNullOrWhiteSpace(accessToken))
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
                    ),
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });
            builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMinutes(30);
            });
            var app = builder.Build();
            #endregion

            app.UseForwardedHeaders();

            #region Data Seeding
            //await app.MigrateDatabaseAsync();
            //await app.SeedDatabaseAsync();
            //await app.SeedIdentityDatabaseAsync();
            #endregion

            #region Configure the HTTP request pipeline.
            app.UseMiddleware<ExceptionHandlerMiddleWare>();

            app.UseResponseCompression();

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
            app.MapHub<ChatHub>("/hubs/chat");
            #endregion

            await WarmUpAsync(app);

            app.Run();
        }


        private static async Task WarmUpAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CorpServeDbContext>();
                await dbContext.Database.CanConnectAsync();

                _ = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                _ = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

                logger.LogInformation("Startup warm-up completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Startup warm-up failed. The app will continue running.");
            }
        }
        private static void ValidatePaymobOptions(PaymobOptions options)
        {
            if (!string.Equals(options.Mode, "Test", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Paymob integration is locked to Test mode in this phase.");

            if (string.IsNullOrWhiteSpace(options.BaseUrl)
                || !options.BaseUrl.Contains("accept.paymob.com", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Paymob BaseUrl must target accept.paymob.com test environment.");

            if (string.IsNullOrWhiteSpace(options.SecretKey)
                || string.IsNullOrWhiteSpace(options.PublicKey)
                || string.IsNullOrWhiteSpace(options.WebhookHmacSecret)
                || string.IsNullOrWhiteSpace(options.WebhookUrl)
                || string.IsNullOrWhiteSpace(options.SuccessRedirectUrl)
                || string.IsNullOrWhiteSpace(options.FailureRedirectUrl))
                throw new InvalidOperationException("Paymob configuration is incomplete.");

            if (options.PaymentMethodIntegrationIds is null || options.PaymentMethodIntegrationIds.Count == 0)
                throw new InvalidOperationException("At least one Paymob payment method integration ID is required.");
        }
    }
}
