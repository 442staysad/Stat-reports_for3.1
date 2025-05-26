using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.DTO;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Core.Services
{
    public class BranchService : IBranchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher<Branch> _branchpasswordHasher;

        public BranchService(IUnitOfWork unitOfWork, 
            IPasswordHasher<Branch> branchpasswordhasher)
        {
            _unitOfWork = unitOfWork;
            _branchpasswordHasher = branchpasswordhasher;
        }


        public async Task<Branch> CreateBranchAsync(BranchDto dto)
        {
            var entity = new Branch
            {
                Name = dto.Name,
                Shortname = dto.Shortname,
                UNP = dto.UNP,
                OKPO = dto.OKPO,
                OKYLP = dto.OKYLP,
                Region = dto.Region,
                Address = dto.Address,
                Email = dto.Email,
                GoverningName = dto.GoverningName,
                HeadName = dto.HeadName,
                Supervisor = dto.Supervisor,
                ChiefAccountant = dto.ChiefAccountant
            };
            entity.PasswordHash = _branchpasswordHasher.HashPassword(entity, dto.Password);
            return await _unitOfWork.Branches.AddAsync(entity);
        }

        public async Task<bool> DeleteBranchAsync(int id)
        {
            var branch = await _unitOfWork.Branches.FindAsync(b => b.Id == id);
            if (branch == null) return false;
            await _unitOfWork.Branches.DeleteAsync(branch);
            await _unitOfWork.SaveChangesAsync(); 
            return true;
        }

        public async Task<IEnumerable<BranchDto>> GetAllBranchesDtosAsync()
        {
            return (await _unitOfWork.Branches.GetAllAsync()).Select(b => new BranchDto
            {
                Id=b.Id,
                Name = b.Name,
                Shortname = b.Shortname,
                UNP = b.UNP!,
                OKPO = b.OKPO,
                OKYLP = b.OKYLP,
                Region = b.Region,
                Address = b.Address,
                Email = b.Email,
                GoverningName = b.GoverningName,
                HeadName = b.HeadName,
                Supervisor = b.Supervisor,
                ChiefAccountant = b.ChiefAccountant,
                Password = "" // not returned
            });
        }

        public async Task<IEnumerable<Branch>> GetAllBranchesAsync()
        {
            return await _unitOfWork.Branches.GetAllAsync();
        }

        public async Task<Branch> GetBranchByIdAsync(int? id)
        {
            return await _unitOfWork.Branches.FindAsync(b => b.Id == id);
        }


        public async Task<Branch> UpdateBranchAsync(BranchProfileDto dto)
        {
            var branch = await _unitOfWork.Branches.FindAsync(b=>b.Id==dto.Id) ?? throw new Exception("Филиал не найден.");
            branch.GoverningName = dto.GoverningName;
            branch.HeadName = dto.HeadName;
            branch.Name = dto.Name;
            branch.Shortname = dto.Shortname;
            branch.UNP = dto.UNP;
            branch.OKPO = dto.OKPO;
            branch.OKYLP = dto.OKYLP;
            branch.Region = dto.Region;
            branch.Address = dto.Address;
            branch.Email = dto.Email;
            branch.Supervisor = dto.Supervisor;
            branch.ChiefAccountant = dto.ChiefAccountant;
            await _unitOfWork.Branches.UpdateAsync(branch);
            await _unitOfWork.SaveChangesAsync(); // <-- Обязательно сохранить

            return branch;
        }

        public async Task<bool> ChangeBranchPasswordAsync(BranchChangePasswordDto dto)
        {
            var branch = await _unitOfWork.Branches.FindAsync(b => b.Id == dto.BranchId);
            if (branch == null)
            {
                return false; // Филиал не найден
            }

            // Проверяем текущий пароль
            var result = _branchpasswordHasher.VerifyHashedPassword(branch, branch.PasswordHash, dto.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                return false; // Неверный текущий пароль
            }

            // Хешируем новый пароль
            branch.PasswordHash = _branchpasswordHasher.HashPassword(branch, dto.NewPassword);

            await _unitOfWork.Branches.UpdateAsync(branch);
            return true;
        }

    }
}
