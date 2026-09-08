using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FisioSalud_Proyecto.Data;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Services;
using System;
using System.Threading.Tasks;

namespace FisioSalud_Proyecto
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<FisioSaludDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.Configure<SmtpSettings>(Configuration.GetSection("SmtpSettings"));
            services.AddSingleton(resolver =>
                Configuration.GetSection("SmtpSettings").Get<SmtpSettings>());

            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IAdminPanelService, AdminPanelService>();
            services.AddScoped<IPacienteService, PacienteService>();
            services.AddScoped<IEquipoService, EquipoService>();
            services.AddScoped<ICitaService, CitaService>();
            services.AddScoped<IMensajeService, MensajeService>();
            services.AddScoped<IEmailService, EmailService>();

            services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", options =>
                {
                    options.Cookie.Name = "FisioSalud.Auth";
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Administrador", policy => policy.RequireRole(Roles.Administrador));
                options.AddPolicy("Fisioterapeuta", policy => policy.RequireRole(Roles.Fisioterapeuta));
                options.AddPolicy("Cliente", policy => policy.RequireRole(Roles.Cliente));
            });

            services.AddControllersWithViews(options => options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider()));
            services.AddSession();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            Task.Run(() => DbInitializer.InitializeAsync(app.ApplicationServices)).Wait();
        }
    }
}
