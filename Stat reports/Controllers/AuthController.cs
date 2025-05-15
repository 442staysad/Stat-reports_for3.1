using System.Threading.Tasks;
using Core.Interfaces;
using Stat_reports.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;

namespace Stat_reports.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public AuthController(IAuthService authService, IWebHostEnvironment hostingEnvironment)
        {
            _authService = authService;
            _hostingEnvironment = hostingEnvironment;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult BranchLogin()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> BranchLogin(BranchLoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var branch = await _authService.
                AuthenticateBranchAsync(model.UNP, model.Password);
            if (branch == null)
            {
                ModelState.AddModelError("", "Неверные УНП или пароль.");
                return View(model);
            }

            HttpContext.Session.SetInt32("BranchId", branch.Id);
            return RedirectToAction("UserLogin");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult UserLogin()
        {
            if (HttpContext.Session.GetInt32("BranchId") == null)
                return RedirectToAction("BranchLogin");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> UserLogin(UserLoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var branchId = HttpContext.Session.GetInt32("BranchId");
            if (branchId == null)
                return RedirectToAction("BranchLogin");

            var user = await _authService.AuthenticateUserAsync(branchId.Value, model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Неверные имя пользователя или пароль.");
                return View(model);
            }

            // Создаем claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role.RoleName), // ВАЖНО: роль
                new Claim("FullName", user.FullName ?? ""),
                new Claim("BranchId", user.BranchId?.ToString() ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            HttpContext.Session.SetInt32("UserId", user.Id);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet] 
        public IActionResult DownloadUserManual()
        {
            string fileName = "Руководство_пользователя.docx";
            // Формируем полный физический путь к файлу в папке wwwroot/docs
            // Убедитесь, что папка 'docs' существует в вашей папке wwwroot
            string filePath = Path.Combine(_hostingEnvironment.WebRootPath, "docs", fileName);

            // Проверяем, существует ли файл по указанному пути
            if (!System.IO.File.Exists(filePath))
            {
                // Если файл не найден, можно вернуть 404 ошибку или другое сообщение
                return NotFound("Руководство пользователя не найдено на сервере.");
            }

            // Определяем MIME-тип для файлов .docx
            string mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            // Отдаем файл
            return PhysicalFile(filePath, mimeType, fileName);
            // PhysicalFile(путь_к_файлу, mime_тип, имя_для_скачивания)
        }

        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear(); // если хочешь чистить
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("BranchLogin");
        }
    }
}