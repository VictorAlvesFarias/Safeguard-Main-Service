
using App.Extensions;
using Application.Services;
using Application.Services.AppFileService;
using Application.Services.AuthenticatorService;
using Application.Services.CaptureService;
using Application.Services.EmailService;
using Application.Services.Identity;
using Domain.Entitites;
using Infrastructure.Context;
using Infrastructure.Factories.DbContextFactory;
using Infrastructure.Repositories.BaseRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PicEnfermagem.Api.Extensions;
using Safeguard.Services;

namespace ASP.NET_Core_Template.Ioc
{
    public static class NativeInjectorConfig
    {
        //Injeção das dependecias
        public static void RegisterServices( this IServiceCollection services, IConfiguration configuration)
        {   
            services.AddScoped(typeof(IBaseRepository<>),typeof(BaseRepository<>));
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IProviderService, ProviderService>();
            services.AddScoped<IPlatformService, PlatformService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IAppFileService, AppFileService>();
            services.AddScoped<IRecoveryKeyService, RecoveryKeyService>();
            services.AddScoped<IEmailAddressService, EmailAddressService>();
            services.AddScoped<IRecoveryEmailService, RecoveryEmailService>();
            services.AddScoped<IEmailFileService, EmailFileService>();
            services.AddMemoryCache();
            services.AddScoped<ICaptureService, CaptureService>();
            services.AddScoped<IAuthenticatorService, AuthenticatorService>();
            services.AddScoped<DbContextFactory>();

            services.AddDefaultIdentity<ApplicationUser>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationContext>()
                .AddDefaultTokenProviders();
        }
    }
}
