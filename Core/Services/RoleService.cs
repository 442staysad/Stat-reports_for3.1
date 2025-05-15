using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.DTO;
using Core.Interfaces;

namespace Core.Services
{
    public class RoleService:IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        public RoleService(IUnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            return (await _unitOfWork.SystemRoles.GetAllAsync())
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    RoleNameRu = r.RoleNameRu
                });
        }
        public async Task<RoleDto> GetRoleByNameAsync(string name)
        {
            var role = await _unitOfWork.SystemRoles.FindAsync(r => r.RoleName == name);
            return new RoleDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                RoleNameRu = role.RoleNameRu
            };

        }
        public async Task<RoleDto> GetRoleByIdAsync(int id)
        {
            var role = await _unitOfWork.SystemRoles.FindAsync(r => r.Id == id);
            return new RoleDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                RoleNameRu = role.RoleNameRu
            };

        }
    }
}
