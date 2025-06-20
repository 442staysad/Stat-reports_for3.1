using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.DTO;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            // Получаем все филиалы
            var allBranches = await _unitOfWork.Branches.GetAllAsync();

            // Фильтруем, исключая те, у которых имя "Admin", и затем преобразуем в DTO
            return allBranches
                .Where(b => b.Name != "Admin") // <-- ДОБАВЛЕНА ФИЛЬТРАЦИЯ
                .Select(b => new BranchDto
                {
                    Id = b.Id,
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
                }).ToList();
        }

        public async Task<IEnumerable<Branch>> GetAllBranchesAsync()
        {
            // Получаем все филиалы
            var allBranches = await _unitOfWork.Branches.GetAllAsync();

            // Фильтруем, исключая те, у которых имя "Admin"
            return allBranches.Where(b => b.Name != "Admin").ToList(); // <-- ДОБАВЛЕНА ФИЛЬТРАЦИЯ
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

        public async Task<IEnumerable<BranchDto>> GetBranchesWithAcceptedReportsAsync(int templateId, int year, int? month, int? quarter, int? halfYear)
        {
            // Начинаем запрос к срокам сдачи, включая связанные филиалы
            var query = (await _unitOfWork.SubmissionDeadlines
                .GetAll(includes: q => q.Include(d => d.Template).Include(d => d.Branch)).ToListAsync())
                .Where(d => d.Template != null &&
                            d.Template.Id == templateId &&
                            d.Period.Year == year &&
                            d.Status == ReportStatus.Reviewed &&
                            d.Branch != null && d.Branch.Name != "Admin");// Филиал должен существовать и не быть "Admin"

            // Применяем фильтр по периоду в зависимости от того, что было передано
            if (month.HasValue)
            {
                query = query.Where(d => d.Period.Month == month.Value);
            }
            else if (quarter.HasValue)
            {
                var monthsInQuarter = GetQuarterMonths(quarter.Value); // Предполагается, что у вас есть хелпер для этого
                query = query.Where(d => monthsInQuarter.Contains(d.Period.Month));
            }
            else if (halfYear.HasValue)
            {
                var monthsInHalfYear = halfYear.Value == 1 ? new[] { 1, 2, 3, 4, 5, 6 } : new[] { 7, 8, 9, 10, 11, 12 };
                query = query.Where(r => monthsInHalfYear.Contains(r.Period.Month));
            }
            // Для годового отчета дополнительная фильтрация по периоду не нужна

            // Выбираем уникальные филиалы из отфильтрованных записей и проецируем в DTO
            var branches = query
                .Select(d => d.Branch)
                .Distinct()
                .Select(b => new BranchDto { Id = b.Id, Name = b.Name })
                .OrderBy(b => b.Name);
                

            return branches.ToList();
        }
        private List<int> GetQuarterMonths(int quarter)
        {
            return quarter switch
            {
                1 => new List<int> { 1, 2, 3 },
                2 => new List<int> { 4, 5, 6 },
                3 => new List<int> { 7, 8, 9 },
                4 => new List<int> { 10, 11, 12 },
                _ => throw new ArgumentOutOfRangeException(nameof(quarter), "Номер квартала должен быть от 1 до 4.")
            };
        }
    }
}
