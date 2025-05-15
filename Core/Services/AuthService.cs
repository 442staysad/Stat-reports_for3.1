using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Branch> _branchRepository;
        private readonly IPasswordHasher<User> _userpasswordHasher;
        private readonly IPasswordHasher<Branch> _branchpasswordHasher;

        public AuthService(IRepository<User> userRepository, 
            IPasswordHasher<User> passwordHasher, 
            IRepository<Branch> branchRepository, 
            IPasswordHasher<Branch> branchpasswordHasher)
        {
            _userRepository = userRepository;
            _userpasswordHasher = passwordHasher;
            _branchpasswordHasher = branchpasswordHasher;
            _branchRepository = branchRepository;
        }

        public async Task<User> AuthenticateUserAsync(int branchId, string username, string password)
        {
            var user = await _userRepository.FindAsync(u => u.BranchId == branchId && u.UserName == username,r=>r.Include(ur=>ur.Role));
            if (user == null) return null;

            var result = _userpasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result == PasswordVerificationResult.Success ? user : null;
        }
        public async Task<User> RegisterUserAsync(int branchId, string username, 
            string fullName, string password)
        {
            var existingUser = await _userRepository.FindAsync(u => u.BranchId == branchId && u.UserName == username);
            if (existingUser != null) return null; // Пользователь с таким именем уже существует

            var user = new User
            {
                BranchId = branchId,
                UserName = username,
                FullName = fullName,
                PasswordHash = _userpasswordHasher.HashPassword(null, password) // Хешируем пароль
            };

            await _userRepository.AddAsync(user);
            return user;
        }
        public async Task<Branch> AuthenticateBranchAsync(string unp, string password)
        {
            var branch = await _branchRepository.FindAsync(b => b.UNP == unp);
            if (branch == null) return null;

            var result = _branchpasswordHasher.VerifyHashedPassword(branch, branch.PasswordHash, password);
            return result == PasswordVerificationResult.Success ? branch : null;
        }
        public async Task<Branch> RegisterBranchAsync(string name, string unp, string password)
        {
            var existingBranch = await _branchRepository.FindAsync(b => b.UNP == unp);
            if (existingBranch != null) return null; // Филиал с таким УНП уже зарегистрирован

            var branch = new Branch
            {
                Name = name,
                UNP = unp,
                PasswordHash = _branchpasswordHasher.HashPassword(null, password) // Хешируем пароль
            };

            await _branchRepository.AddAsync(branch);
            return branch;
        }
    }
}