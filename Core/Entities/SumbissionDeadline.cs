using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Core.Enums;
using System.Linq;
namespace Core.Entities
{
    public class SubmissionDeadline : BaseEntity
    {
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }
        public int ReportTemplateId { get; set; }
        public ReportTemplate Template { get; set; }
        public int? ReportId { get; set; }=null;

        public DeadlineType DeadlineType { get; set; }
        public DateTime DeadlineDate { get; set; }
        public DateTime Period { get; set; }

        public int? FixedDay { get; set; } // Например, 26-е число (если есть)
        [NotMapped] // Текущий комментарий не хранится отдельно, а берется из последней записи истории
        public string? Comment => CommentHistory.OrderByDescending(h => h.CreatedAt)
                                               .FirstOrDefault()?.Comment;
        // Навигационное свойство для истории
        public ICollection<CommentHistory> CommentHistory { get; set; }
            = new List<CommentHistory>();

        public bool IsClosed { get; set; } // Новый флаг
        public ReportStatus? Status { get; set; } = ReportStatus.InProgress;

    }
}
