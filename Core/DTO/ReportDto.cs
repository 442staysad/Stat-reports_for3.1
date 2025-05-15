using System;
using System.Collections.Generic;
using Core.Entities;
using Core.Enums;

namespace Core.DTO
{
    public class ReportDto : BaseDTO
    {
        public string Name { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int UploadedById { get; set; }
        public int? BranchId { get; set; }
        public int? TemplateId { get; set; }
        public DeadlineType? DeadlineType { get; set; }
        public ReportStatus Status { get; set; }
        public string? FilePath { get; set; }
        public string? Comment { get; set; }
        public DateTime UploadDate { get; set; }
        public DateTime Period { get; set; }
        public ReportType Type { get; set; }
        public List<CommentHistoryDto> CommentHistory { get; set; } = new List<CommentHistoryDto>();
    }
}