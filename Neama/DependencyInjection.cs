﻿using FluentValidation.AspNetCore;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using Ne3ma.Services;
using Ne3ma.Settings;
using Neama.Authentication;
using Neama.Errors;
using System.Reflection;
using System.Text;

namespace Neama;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services
        , IConfiguration configuration)
    {
        services.AddControllers();

        services.AddDistributedMemoryCache();

        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();

        //services.AddCors(options =>
        //    options.AddDefaultPolicy(builder =>
        //        builder
        //            .AllowAnyMethod()
        //            .AllowAnyHeader()
        //            //.WithOrigins(allowedOrigins!)
        //        )
        //    );
        services.AddCors(options =>
        options.AddDefaultPolicy(builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
    )
);

        services.AddAuthConfig(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Connection String 'DefaultConnection' not found.");

        //var serverConnection = configuration.GetConnectionString("ServerConnection") ??
        //    throw new InvalidOperationException("Connection String 'ServerConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));


        //services.AddDbContext<ApplicationDbContext>(options =>
        //    options.UseSqlServer(serverConnection));

        services
            .AddSwaggerServices()
            .AddMapesterConfig()
            .AddFluentValidationConfig();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailSender, EmailService>();
        services.AddScoped<IUserService, UserService>();



        services.AddExceptionHandler<GloabalExceptionHandler>();
        services.AddProblemDetails();

        services.Configure<MailSettings>(configuration.GetSection(nameof(MailSettings)));

        services.AddHttpContextAccessor();


        return services;
    }

    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static IServiceCollection AddMapesterConfig(this IServiceCollection services)
    {
        // add Mapster
        var mappingConfig = TypeAdapterConfig.GlobalSettings;
        mappingConfig.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton<IMapper>(new Mapper(mappingConfig));

        return services;
    }

    public static IServiceCollection AddFluentValidationConfig(this IServiceCollection services)
    {
        services
           .AddFluentValidationAutoValidation()
           .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IServiceCollection AddAuthConfig(this IServiceCollection services,
        IConfiguration configuration)
    {

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IJwtProvider, JwtProvider>();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtSettings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.Key!)),
                    ValidIssuer = jwtSettings?.Issuer,
                    ValidAudience = jwtSettings?.Audience
                };
            });

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedEmail = false;
            options.User.RequireUniqueEmail = true;
        });

        return services;
    }
}
