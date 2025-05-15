using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTO;
using Core.Entities;

namespace Core.Interfaces
{

    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync(int? branchId = null);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> GetUserByIdAsync(int id);
        Task<User> CreateUserAsync(UserDto dto);
        Task<User> UpdateUserAsync(UserProfileDto dto);
        Task<bool> DeleteUserAsync(int id);
        Task<IEnumerable<User>> GetUsersByBranchIdAsync(int branchId);
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);

    }
}
