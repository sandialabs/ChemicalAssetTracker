using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CMS.Data;
using CMS.Models;
using CMS.Services;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.StaticFiles;
using DataModel;

namespace CMS
{
    public class Startup
    {
        const bool UseHTTPS = true;
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            CMSDB.Configuration = Configuration;
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection")));

            //services.AddIdentity<ApplicationUser, IdentityRole>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultTokenProviders();

            //----------------------------------------------------------------
            //
            // Configure In-Memory Session State
            //
            //----------------------------------------------------------------
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);   // keep session variables for 1 hour
                options.Cookie.HttpOnly = true;
            });

            //----------------------------------------------------------------
            //
            // Configure Authentication
            //
            //----------------------------------------------------------------
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 4;
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            }).AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // PH: auto logout

            //----------------------------------------------------------------
            //
            // Auto logout
            //
            //----------------------------------------------------------------
            double timeout_check_interval_seconds = 10;  // the system will check every 10 seconds
            double user_logout_time_minutes = 30;        // the user will be logged out after 30 minutes of inactivity
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromSeconds(timeout_check_interval_seconds));
            services.AddAuthentication().Services.ConfigureApplicationCookie(options =>
            {
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(user_logout_time_minutes);
            });

            services.Configure<IISOptions>(options => options.ForwardClientCertificate = false);
            // This is a service that can be injected to support user account management
            services.AddScoped<IAccountHelper, AccountHelper>();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            if (UseHTTPS) 
            {
                services.AddHttpsRedirection(options => {
                    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                    options.HttpsPort = Program.ListenPort;
                });
            }
            services.AddMvc()
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            services.AddAntiforgery();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ApplicationDbContext db_context)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseSession();

            // PH: fix for .vue files
            var myProvider = new FileExtensionContentTypeProvider();
            myProvider.Mappings.Add(".vue", "text/plain");
            // app.UseHttpsRedirection();
            string[] clargs = System.Environment.GetCommandLineArgs();
            if (clargs.Contains("-https")) 
            {
                Console.WriteLine("USE HTTPS");
            }
            app.UseStaticFiles(new StaticFileOptions() { ContentTypeProvider = myProvider });
            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            try
            {
                db_context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                Console.WriteLine("Unable to open CMSUsers database: " + ex.Message);
            }
            using (var db = new CMSDB())
            {
                db.Database.EnsureCreated();
                db.EnsureSeeded();
                db.InitializeStandardSettings();
                db.LogInfo("", "startup", "CAT Web Application is starting");
            }
        }
    }
}
