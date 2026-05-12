using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olimpiadnic.Models.AccountModels;
using Olimpiadnic.Models.PasswordRecoveryModels;
using Olimpiadnic.Services;
using System.Security.Cryptography;

namespace Olimpiadnic.Controllers
{
    public class PasswordRecoveryController : Controller
    {
        private readonly IPasswordRecoveryService _passwordRecoveryService;

        public PasswordRecoveryController(IPasswordRecoveryService passwordRecoveryService)
        {
            _passwordRecoveryService = passwordRecoveryService;
        }

        // GET: /PasswordRecovery/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /PasswordRecovery/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _passwordRecoveryService.SendResetLinkAsync(model.Email, Request.Scheme, Request.Host.Value);

            if (result.Success)
            {
                TempData["DebugResetLink"] = result.ResetLink;
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(model);
        }

        // GET: /PasswordRecovery/ForgotPasswordConfirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /PasswordRecovery/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        // POST: /PasswordRecovery/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _passwordRecoveryService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(model);
            }

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        // GET: /PasswordRecovery/ResetPasswordConfirmation
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
