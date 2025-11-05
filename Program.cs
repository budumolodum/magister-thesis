using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddControllers();
            // Додай cookie authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";  // Redirect на логін, якщо не авторизований
                    options.AccessDeniedPath = "/Account/AccessDenied";  // Якщо немає прав
                    options.ExpireTimeSpan = TimeSpan.FromHours(1);  // Термін дії cookie
                    options.SlidingExpiration = true;  // Подовжувати при активності
                });

            // Add authorization if needed
            builder.Services.AddAuthorization();
            builder.Services.AddDbContext<ContextDataBase>();
            builder.Services.AddWebEncoders();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}