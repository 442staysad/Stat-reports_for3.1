using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces
{
    public interface IAuthService
    {
        Task<User> AuthenticateUserAsync(int branchId, string username, string password);
        Task<User> RegisterUserAsync(int branchId, string username, string fullName, string password);
        Task<Branch> AuthenticateBranchAsync(string unp, string password);
        Task<Branch> RegisterBranchAsync(string name, string unp, string password);
    }
}
