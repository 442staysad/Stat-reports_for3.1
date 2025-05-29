using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;

namespace Core.DTO
{
    public class PendingTemplateDto : BaseDTO
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime Period { get; set; } // Период отчета, если есть
        public ReportStatus Status { get; set; }
        public string? Comment { get; set; }
        public int? ReportId { get; set; } // ID загруженного отчета (если есть)
        public string ReportType { get; set; }
        public DeadlineType Type { get; set; } // Тип дедлайна (ежемесячный, квартальный и т.д.)
        public int? BranchId { get; set; }

        // Новое поле: история комментариев
        public List<CommentHistoryDto> CommentHistory { get; set; } = new List<CommentHistoryDto>();
    }

    public class CommentHistoryDto
    {
        public DateTime CreatedAt { get; set; }
        public string Comment { get; set; }
        public string? AuthorName { get; set; }
    }
}
