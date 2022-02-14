using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

//add for db
using Microsoft.EntityFrameworkCore;
using MySql.Data.EntityFrameworkCore;

//add by copyleft
using System.Text;
using Microsoft.Extensions.Options;
//add for jwt
using Microsoft.IdentityModel.Tokens;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.Extensions.Hosting;




using XAccount.Services;
using XAccount.Data;
using XAccount.Helpers;
using XAccount.Entities;

using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace XAccount
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
            //添加数据服务
            if (false)
            {
                ////使用内存数据库
                //services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("MoneyTalksDatabase"));
                // services.AddDbContext<DataContext>(opt => opt.u);
            }
            else
            {
                //使用mysql
                var connection = Configuration.GetConnectionString("MySqlConnection");
                services.AddDbContext<DataContext>(opt => opt.UseMySQL(connection));
            }


            //跨域访问
            services.AddCors();

            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // configure jwt authentication
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            // configure DI for application services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFamilyService, FamilyService>();
            services.AddScoped<ITalksService, TalksService>();
            services.AddScoped<IMahjongService, MahjongService>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();


            //定时执行
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, APIDataService>();



            ////
            //services.Configure<CookiePolicyOptions>(options =>
            //{
            //    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            //    options.CheckConsentNeeded = context => true;
            //    options.MinimumSameSitePolicy = SameSiteMode.None;
            //});

            services.AddOData();

            //继续使用 NewtonsoftJson 反对系统 json 
            //https://stackoverflow.com/questions/42290811/how-to-use-newtonsoft-json-as-default-in-asp-net-core-web-api
            //services.AddControllers();
            services.AddControllers().AddNewtonsoftJson();

            //services.AddControllersWithViews();//.SetCompatibilityVersion(CompatibilityVersion.Version3_1);
            services.AddRazorPages();

            //for odata
            //services.AddControllers(mvcOptions => mvcOptions.EnableEndpointRouting = false);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
            //同时要去掉 https:// 地址

            app.UseRouting();
            //跨域访问配置
            // global cors policy
            app.UseCors(x => x
                            //.AllowAnyOrigin()  //3.1 不在允许任意跨域
                            .WithOrigins("http://app.ournet.club", "http://app.ournet.club:2021", "http://localhost:8080") // 只允许部分源
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                        );

            app.UseStaticFiles();
            app.UseCookiePolicy();
            //token 验证
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStaticFiles();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "admin",
                    pattern: "{controller=Admin}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                        name: "api",
                        pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.Select().Expand().Filter().OrderBy().MaxTop(100).Count();                    //使用高级查询

                endpoints.MapODataRoute("ODataRoute", "odata", GetEdmModel());
          
            });

            //创建数据库
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                //serviceScope.ServiceProvider.GetService<DataContext>().Database.Migrate();
                var context = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
                context.Database.EnsureCreated();
            }
        }

        private static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();

            builder.EntitySet<User>("Users");           //可以不显示某些字段  https://docs.microsoft.com/zh-cn/aspnet/web-api/overview/odata-support-in-aspnet-web-api/odata-security-guidance

            builder.EntitySet<UserTag>("UserTags");
            builder.EntitySet<Tag>("Tags");

            return builder.GetEdmModel();
        }

        /*  asp.net 3.1   odata 使用https://www.codercto.com/a/113218.html
         *      https://docs.microsoft.com/zh-cn/azure/search/search-query-odata-comparison-operators
                相等性运算符：
                eq：测试某个字段是否等于某个常量值
                ne：测试某个字段是否不等于某个常量值
                范围运算符：
                gt：测试某个字段是否大于某个常量值
                lt：测试某个字段是否小于某个常量值
                ge：测试某个字段是否大于或等于某个常量值
                le：测试某个字段是否小于或等于某个常量值

         * https://localhost:44308/odata/Users?$filter=Price eq 0       ==
         * https://localhost:44308/odata/Users?$filter=Price gt 0      > 0
         * 
         */

    }
}
