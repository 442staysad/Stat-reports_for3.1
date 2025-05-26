using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.DTO;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchDto>> GetAllBranchesDtosAsync();
        Task<IEnumerable<Branch>> GetAllBranchesAsync();
        Task<Branch> GetBranchByIdAsync(int? id);
        Task<Branch> CreateBranchAsync(BranchDto dto);
        Task<Branch> UpdateBranchAsync(BranchProfileDto dto);
        Task<bool> DeleteBranchAsync(int id);
        Task<bool> ChangeBranchPasswordAsync(BranchChangePasswordDto dto);
    }
}
