using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stat_reports.ViewModels;
using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Stat_reportsnt.Filters;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace Stat_reports.Controllers
{
    [AuthorizeBranchAndUser]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly IBranchService _branchService;

        public ProfileController(IUserService userService, IBranchService branchService)
        {
            _userService = userService;
            _branchService = branchService;
        }

        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("UserLogin", "Auth");

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null) return NotFound();

            var branch = user.Branch ?? await _branchService.GetBranchByIdAsync(user.BranchId ?? 0);

            var model = new UserProfileViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Number = user.Number,
                Position = user.Position,

                BranchId = branch.Id,
                Name = branch.Name,
                Shortname = branch.Shortname,
                UNP = branch.UNP,
                OKPO = branch.OKPO,
                OKYLP = branch.OKYLP,
                Region = branch.Region,
                Address = branch.Address,
                BranchEmail = branch.Email,
                GoverningName = branch.GoverningName,
                HeadName = branch.HeadName,
                Supervisor = branch.Supervisor,
                ChiefAccountant = branch.ChiefAccountant
            };
            // Инициализация моделей для модалок и передача их через ViewBag
            ViewBag.UserChangePasswordModel = new UserChangePasswordViewModel { UserId = model.UserId };
            ViewBag.BranchChangePasswordModel = new BranchChangePasswordViewModel { BranchId = model.BranchId };


            return View("Profile", model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {

            var userDto = new UserProfileDto
            {
                Id = model.UserId,
                FullName = model.FullName,
                Number = model.Number,
                Email = model.Email,
                Position = model.Position
            };

            var branchDto = new BranchProfileDto
            {
                Id = model.BranchId,
                GoverningName = model.GoverningName,
                HeadName = model.HeadName,
                Name = model.Name,
                Shortname = model.Shortname,
                UNP = model.UNP,
                OKPO = model.OKPO,
                OKYLP = model.OKYLP,
                Region = model.Region,
                Address = model.Address,
                Email = model.BranchEmail,
                Supervisor = model.Supervisor,
                ChiefAccountant = model.ChiefAccountant
            };

            await _userService.UpdateUserAsync(userDto);
            await _branchService.UpdateBranchAsync(branchDto);

            TempData["Success"] = "Профиль успешно обновлен!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserPassword(UserChangePasswordViewModel model)
        {
            // Валидация модели
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Пожалуйста, исправьте ошибки в форме.";
                // Если невалидно, нужно заново получить данные профиля, чтобы отобразить страницу
                // Это может быть сложно, если ViewModel большая. Проще вернуть View("Profile", currentProfileViewModel)
                // Но для простоты сейчас просто перенаправим на Index и покажем ошибки через TempData
                return RedirectToAction("Index");
            }

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || userId.Value != model.UserId)
            {
                TempData["Error"] = "Ошибка авторизации или неверный ID пользователя.";
                return RedirectToAction("UserLogin", "Auth"); // или AccessDenied
            }

            var userChangePasswordDto = new UserChangePasswordDto
            {
                UserId = model.UserId,
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword
            };

            bool success = await _userService.ChangeUserPasswordAsync(userChangePasswordDto);

            if (success)
            {
                TempData["Success"] = "Пароль пользователя успешно изменен!";
            }
            else
            {
                TempData["Error"] = "Ошибка при смене пароля пользователя. Возможно, текущий пароль неверен.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeBranchPassword(BranchChangePasswordViewModel model)
        {
            // Проверка прав пользователя
            if (!User.IsInRole("AdminBranch") && !User.IsInRole("Admin") && !User.IsInRole("AdminTrest"))
            {
                TempData["Error"] = "У вас нет прав для изменения пароля филиала.";
                return RedirectToAction("AccessDenied", "Auth");
            }

            // Валидация модели
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Пожалуйста, исправьте ошибки в форме.";
                return RedirectToAction("Index");
            }

            var branchChangePasswordDto = new BranchChangePasswordDto
            {
                BranchId = model.BranchId,
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword
            };

            bool success = await _branchService.ChangeBranchPasswordAsync(branchChangePasswordDto);

            if (success)
            {
                TempData["Success"] = "Пароль филиала успешно изменен!";
            }
            else
            {
                TempData["Error"] = "Ошибка при смене пароля филиала. Возможно, текущий пароль неверен или филиал не найден.";
            }

            return RedirectToAction("Index");
        }

    }
}
