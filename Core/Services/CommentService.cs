using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Core.Services
{
    public class CommentService:ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CommentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CommentHistory> AddCommentAsync(int deadlineId, string comment, ReportStatus status, int? changedById)
        {
            var historyRecord = new CommentHistory
            {
                DeadlineId = deadlineId,
                Comment = comment,
                Status = status,
                AuthorId = changedById,
                CreatedAt = DateTime.UtcNow
            };

            return await _unitOfWork.CommentHistory.AddAsync(historyRecord);
        }

        public async Task<IEnumerable<CommentHistory>> GetHistoryAsync(int deadlineId)
        {
            return await _unitOfWork.CommentHistory
                .FindAll(c=>c.Where(h=>h.DeadlineId==deadlineId)) // Фильтрация по deadlineId
                .OrderByDescending(h => h.CreatedAt) // Сортировка по дате
                .Include(h => h.Author) // Включение данных об авторе
                .ToListAsync(); // Преобразование результата в список асинхронно
        }

        public async Task<int> DeleteComment(int commentId) 
        { 
            var comment = await _unitOfWork.CommentHistory.FindAsync(c=>c.Id==commentId);
            return await _unitOfWork.CommentHistory.DeleteAsync(comment);
        }
    }
}
