using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OsmIntegrator.AutoMapper;
using OsmIntegrator.Database;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Errors;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Services;
using OsmIntegrator.Tools;
using OsmIntegrator.Validators;

namespace osmintegrator
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddApiVersioning(config =>
      {
        config.DefaultApiVersion = new ApiVersion(0, 9); // global default version all controlers fit it
        config.AssumeDefaultVersionWhenUnspecified = true;
        config.ReportApiVersions = true;
        config.ApiVersionReader = new HeaderApiVersionReader("Api-Version");
        config.ErrorResponses = new ApiVersioningErrorResponseProvider();
      });

      services.AddSingleton<DataInitializer>();
      // ===== Add our DbContext ========
      services.AddDbContext<ApplicationDbContext>();
      services.AddControllers()
          .AddNewtonsoftJson(options =>
          {
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
          });

      // ===== Allow-Origin ========
      services.AddCors(c =>
      {
        c.AddPolicy("AllowOrigin", options => options
                  .AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod());
      });

      services.AddLocalization(options =>
      {
        options.ResourcesPath = "Resources";
      });

      services.Configure<RequestLocalizationOptions>(options =>
      {
        options.SetDefaultCulture("pl-PL");
        options.AddSupportedUICultures("pl-PL", "en-US");
        options.FallBackToParentUICultures = true;

        options.RequestCultureProviders.Clear();
        options.RequestCultureProviders.Add(new CustomRequestCultureProvider(context =>
              {
                var userLangs = context.Request.Headers["Accept-Language"].ToString();
                var firstLang = userLangs.Split(',').FirstOrDefault();
                var defaultLang = string.IsNullOrEmpty(firstLang) ? "en" : firstLang;
                return Task.FromResult(new ProviderCultureResult(defaultLang, defaultLang));
              }));

      });


      // ===== Add Identity ========
      services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
      {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.SignIn.RequireConfirmedEmail = true;
      })
          .AddRoles<ApplicationRole>()
          .AddEntityFrameworkStores<ApplicationDbContext>()
          .AddDefaultTokenProviders();

      // https://stackoverflow.com/a/42389162/1816687
      services.Configure<SecurityStampValidatorOptions>(options =>
      {
        // enables immediate logout, after updating the user's stat.
        options.ValidationInterval = TimeSpan.Zero;
      });

      // ===== Add Jwt Authentication ========
      JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); // => remove default claims
      services
          .AddAuthentication(options =>
          {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
          })
          .AddJwtBearer(cfg =>
          {
            cfg.RequireHttpsMetadata = false;
            cfg.SaveToken = true;

            cfg.TokenValidationParameters = new TokenValidationParameters
            {
              ValidIssuer = Configuration["JwtIssuer"],
              ValidAudience = Configuration["JwtIssuer"],
              IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JwtKey"])),
              ClockSkew = TimeSpan.Zero // remove delay of token when expire
            };
          });

      services.AddAuthorization();
      services.AddControllers();
      services.AddSwaggerGenNewtonsoftSupport();
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "osmintegrator", Version = "v1" });
      });
      services.AddAutoMapper(
          typeof(TileProfile),
          typeof(StopProfile),
          typeof(TagProfile),
          typeof(ApplicationUserProfile),
          typeof(ConnectionProfile),
          typeof(ExistingNoteProfile),
          typeof(NewNoteProfile),
          typeof(ConversationProfile),
          typeof(MessageProfile));

      services.AddSingleton<IEmailService, EmailService>();
      services.AddSingleton<ITokenHelper, TokenHelper>();
      services.AddSingleton<ITileValidator, TileValidator>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app,
        IWebHostEnvironment env,
        ApplicationDbContext dbContext)
    {
      if (env.IsDevelopment() || env.EnvironmentName == "Test")
      {
        app.UseExceptionHandler("/error-local-development");
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "osmintegrator v1"));
      }
      else
      {
        app.UseExceptionHandler("/error");
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseCors();

      // ===== Use Authentication ======
      app.UseAuthentication();
      app.UseAuthorization();
      app.UseRequestLocalization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });

      // ===== Create tables ======
      dbContext.Database.EnsureCreated();
    }
  }
}
