using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BCrypt.Net;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private const string AdminUsername = "admin";
        private const string AdminPasswordHash = "$2a$11$B.jqTvv2EYQs44nNtPPHpuwiklvOzpfEX0MUH14ygkKAVyyPY9Idi";

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                if (model.Username == AdminUsername && BCrypt.Net.BCrypt.Verify(model.Password, AdminPasswordHash))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, AdminUsername),
                        new Claim(ClaimTypes.Role, "Admin")
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                    return LocalRedirect(returnUrl ?? "/");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Невірний логін або пароль");
                }
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // ГЕТ: сторінка підтвердження
        [HttpGet]
        public IActionResult ConfirmLogout()  // ЗМІНЕНО НАЗВУ!
        {
            return View("Logout"); // Views/Account/Logout.cshtml
        }

        // ПОСТ: реальний вихід
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}