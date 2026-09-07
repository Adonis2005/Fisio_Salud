using System;
using System.Threading.Tasks;
using FisioSalud_Proyecto.Helpers;
using FisioSalud_Proyecto.Models.Auth;
using FisioSalud_Proyecto.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FisioSalud_Proyecto.Controllers.Auth
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IEmailService emailService, IConfiguration configuration)
        {
            _authService = authService;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToRoleDashboard();

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.LoginAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            var principal = _authService.CreateClaimsPrincipal(result.User);
            await HttpContext.SignInAsync("CookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToRoleDashboard(result.User.Rol.Nombre);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToRoleDashboard();

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.RegisterAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            TempData["Success"] = "Registro exitoso. Ya puede iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.RequestPasswordResetAsync(model.Correo.Trim().ToLower());

            if (result.Token != null)
            {
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
                var resetLink = $"{baseUrl}/Auth/ResetPassword?token={result.Token}";

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(model.Correo.Trim().ToLower(), resetLink);
                }
                catch (InvalidSmtpConfigurationException)
                {
                    TempData["Info"] = $"SMTP no configurado. Enlace de prueba: {resetLink}";
                    return RedirectToAction(nameof(Login));
                }
            }

            TempData["Success"] = "Si el correo está registrado, recibirá instrucciones para restablecer su contraseña.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(Login));

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.ResetPasswordAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View(model);
            }

            TempData["Success"] = "Contraseña restablecida correctamente. Inicie sesión.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToRoleDashboard(string role = null)
        {
            role ??= User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return role switch
            {
                Roles.Administrador => RedirectToAction("Dashboard", "Administrador"),
                Roles.Fisioterapeuta => RedirectToAction("Dashboard", "Fisioterapeuta"),
                Roles.Cliente => RedirectToAction("Dashboard", "Cliente"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}
