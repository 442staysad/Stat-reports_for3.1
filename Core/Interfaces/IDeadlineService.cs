using Core.Enums;
using Core.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Core.Interfaces
{
    public interface IDeadlineService
    {
       Task<IEnumerable<SubmissionDeadline>> GetAllAsync();
        Task CheckAndUpdateDeadlineAsync(int templateId, int branchId, int? reportId=null);
        DateTime CalculateDeadline(DeadlineType deadlineType, int fixedDay, DateTime reportDate);
        Task<bool> DeleteDeadlineAsync(int id);
        Task<SubmissionDeadline> GetDeadlineByIdAsync(int id);
        Task UpdateDeadlineAsync(SubmissionDeadline deadlineToUpdate);
        Task UpdateCommentAsync(int commentId, string commentText, string currentUserId);
    }
}