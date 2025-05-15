using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTO;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stat_reports.ViewModels;

namespace Stat_reports.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IBranchService _branchService;
        private readonly IRoleService _roleService; // сервис, который отдаёт SystemRole

        public UserController(IUserService u, IBranchService b, IRoleService r)
        {
            _userService = u;
            _branchService = b;
            _roleService = r;
        }
        
        public async Task<IActionResult> Index()
        {
            int? sessionBranchId = HttpContext.Session.GetInt32("BranchId");
            bool isGlobal = User.IsInRole("Admin");
            var dtos = await _userService.GetAllUsersAsync(isGlobal ? null : sessionBranchId);
            return View(dtos);
        }

        [Authorize(Roles = "Admin,AdminTrest,AdminBranch")]
        public async Task<IActionResult> Create()
        {
            int? sessionBranchId = HttpContext.Session.GetInt32("BranchId");

            var allRoles = await _roleService.GetAllRolesAsync();
            var allBranches = await _branchService.GetAllBranchesAsync();

            IEnumerable<SelectListItem> roleOptions;
            IEnumerable<SelectListItem> branchOptions;

            if (User.IsInRole("Admin"))
            {
                // Admin может выбрать любую роль и филиал
                roleOptions = allRoles.Select(r => new SelectListItem(r.RoleNameRu, r.Id.ToString()));
                branchOptions = allBranches.Select(b => new SelectListItem(b.Name, b.Id.ToString()));
            }
            else if (User.IsInRole("AdminTrest"))
            {
                // AdminTrest — только User, PEB, OBUnF в своём филиале
                var allowedRoles = new[] { "User", "PEB", "OBUnF" };
                roleOptions = allRoles
                    .Where(r => allowedRoles.Contains(r.RoleName))
                    .Select(r => new SelectListItem(r.RoleNameRu, r.Id.ToString()));

                branchOptions = allBranches
                    .Where(b => b.Id == sessionBranchId)
                    .Select(b => new SelectListItem(b.Name, b.Id.ToString()));
            }
            else // AdminBranch
            {
                // Только User и только свой филиал
                var userRole = allRoles.First(r => r.RoleName == "User");
                roleOptions = new[] { new SelectListItem(userRole.RoleNameRu, userRole.Id.ToString()) };

                branchOptions = allBranches
                    .Where(b => b.Id == sessionBranchId)
                    .Select(b => new SelectListItem(b.Name, b.Id.ToString()));
            }

            var vm = new UserCreateViewModel
            {
                RoleOptions = roleOptions,
                BranchOptions = branchOptions
            };

            return View(vm);
        }


        [HttpPost, Authorize(Roles = "Admin,AdminTrest,AdminBranch")]
        public async Task<IActionResult> Create(UserCreateViewModel vm)
        {
            int? sessionBranchId = HttpContext.Session.GetInt32("BranchId");

            if (User.IsInRole("Admin"))
            {
                // Admin — без ограничений
            }
            else if (User.IsInRole("AdminTrest"))
            {
                // Только User, PEB, OBUnF и только свой филиал
                var allowedRoles = new[] { "User", "PEB", "OBUnF" };
                var selectedRole = await _roleService.GetRoleByIdAsync(vm.RoleId);
                if (!allowedRoles.Contains(selectedRole.RoleName))
                {
                    var defaultRole = await _roleService.GetRoleByNameAsync("User");
                    vm.RoleId = defaultRole.Id;
                }

                vm.BranchId = sessionBranchId!.Value;
            }
            else // AdminBranch
            {
                // Только User и только свой филиал
                var defaultRole = await _roleService.GetRoleByNameAsync("User");
                vm.RoleId = defaultRole.Id;
                vm.BranchId = sessionBranchId!.Value;
            }

            var dto = new UserDto
            {
                UserName = vm.UserName,
                FullName = vm.FullName,
                Number = vm.Number,
                Email = vm.Email,
                Position = vm.Position,
                Password = vm.Password,
                RoleId = vm.RoleId,
                BranchId = vm.BranchId
            };

            await _userService.CreateUserAsync(dto);
            TempData["Success"] = "Пользователь создан";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [Authorize(Roles = "Admin,AdminTrest,AdminBranch")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteUserAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
