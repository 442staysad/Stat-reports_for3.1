using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stat_reports.ViewModels;
using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Stat_reportsnt.Filters;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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

            return View("Profile", model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

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
            return RedirectToAction("Profile");
        }
    }
}
