using Core.DTO;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher<User> _userpasswordHasher;

        public UserService(IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher)
        {
            _unitOfWork = unitOfWork;
           _userpasswordHasher = passwordHasher;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _unitOfWork.Users.GetAllAsync();
        }
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(int? branchId = null)
        {
            var query = (await _unitOfWork.Users.GetAll(u=>u.Include(r=>r.Role)).ToListAsync())
                        .Where(u => !branchId.HasValue || u.BranchId == branchId);
            return query.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                Number = u.Number,
                Email = u.Email,
                Position = u.Position,
                RoleId = u.RoleId,
                RoleName = u.Role?.RoleName,
                BranchId = u.BranchId!.Value,
                Password = "",
                RoleNameRu = u.Role?.RoleNameRu,
            });
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _unitOfWork.Users.FindAsync(u => u.Id == id);
        }
        public async Task<IEnumerable<User>> GetUsersByBranchIdAsync(int branchId)
        {
            return await _unitOfWork.Users.FindAllAsync(u => u.BranchId == branchId);
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            return await _unitOfWork.Users.FindAllAsync(u => u.Role.RoleName == role);
        }


        public async Task<User> CreateUserAsync(UserDto dto)
        {
            var entity = new User
            {
                UserName = dto.UserName,
                FullName = dto.FullName,
                Number = dto.Number,
                Email = dto.Email,
                Position = dto.Position,
                RoleId = dto.RoleId,
                BranchId = dto.BranchId
            };
            entity.PasswordHash = _userpasswordHasher.HashPassword(entity, dto.Password);
            return await _unitOfWork.Users.AddAsync(entity);
        }

        public async Task<User> UpdateUserAsync(UserProfileDto dto)
        {
            var user = await _unitOfWork.Users.FindAsync(b => b.Id == dto.Id) ?? throw new Exception("Пользователь не найден.");
            user.FullName = dto.FullName;
            user.Number = dto.Number;
            user.Email = dto.Email;
            user.Position = dto.Position;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(); // <-- Обязательно сохранить

            return user;
        }
        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _unitOfWork.Users.FindAsync(u => u.Id == id);
            if (user == null) return false;
            await _unitOfWork.Users.DeleteAsync(user);
            return true;
        }
    }
}
