using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Enums;

namespace Core.Entities
{
    public class CommentHistory : BaseEntity
    {
        public int? ReportId { get; set; }
        public Report? Report { get; set; }

        public int? DeadlineId { get; set; }
        public SubmissionDeadline? Deadline { get; set; }

        public string Comment { get; set; }
        public ReportStatus Status { get; set; }

        public int? AuthorId { get; set; }
        public User? Author { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
