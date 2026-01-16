using MedicineLog.Data;
using MedicineLog.Data.Entities;
using MedicineLog.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedicineLog.Controllers
{
    public sealed class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginVm { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            var result = await _signInManager.PasswordSignInAsync(
                userName: email,
                password: model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Kontot är låst tillfälligt. Försök igen senare.");
                    return View(model);
                }

                ModelState.AddModelError(string.Empty, "Felaktig e-post eller lösenord.");
                return View(model);
            }

            // Prefer explicit returnUrl if it's local (e.g. user was sent to login from a protected page)
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            // Role-based landing page
            var user = await _userManager.FindByNameAsync(email);
            if (user is null)
            {
                // Should be rare if sign-in succeeded, but keep it safe.
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            if (await _userManager.IsInRoleAsync(user, UserRoles.Admin.ToString()))
                return RedirectToAction("Index", "Admin", new { area = "" }); // /Admin/Index

            if (await _userManager.IsInRoleAsync(user, UserRoles.Auditor.ToString()))
                return RedirectToAction("Index", "Audit", new { area = "" }); // /Audit/Index

            // Fallback if no known role
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", new { area = "Identity" });
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
