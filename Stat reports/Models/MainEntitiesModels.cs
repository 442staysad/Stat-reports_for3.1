using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Entities;
using Core.Enums;
using Microsoft.AspNetCore.Http;

namespace Stat_reports.Models
{
    public class BranchModel
    {
        public int Id { get; set; }
        public string? GoverningName { get; set; }
        public string? HeadName { get; set; }
        public string? Name { get; set; }
        public string? Shortname { get; set; }
        [Required]
        public string? UNP { get; set; } // Используется для входа филиала
        public string? OKPO { get; set; }
        public string? OKYLP { get; set; }
        public string? Region { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Supervisor { get; set; }
        public string? ChiefAccountant { get; set; }
        [Required]
        public string PasswordHash { get; set; }
    }

    public class ReportModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TemplateId { get; set; }
        public ReportTemplate Template { get; set; }
        public DateTime UploadDate { get; set; }
        public int UploadedById { get; set; }
        public User UploadedBy { get; set; }

        public int? BranchId { get; set; }
        public string? FilePath { get; set; } // Путь к файлу

        public ICollection<ReportAccess> Accesses { get; set; } = new List<ReportAccess>();
        public string? Comment { get; set; }
    }
    public class ReportTemplateModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? FilePath { get; set; }
        public IFormFile? File { get; set; }
        public ICollection<Report>? Reports { get; set; }
        public SubmissionDeadline? SubmissionDeadline { get; set; }

        // Новые свойства для дедлайна
        public DeadlineType DeadlineType { get; set; }
        public int? FixedDay { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ReportDate { get; set; }
    }

    public class UserModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string? Number { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public int? AccessId { get; set; }
        public string PasswordHash { get; set; }
        public int? Role { get; set; }
        public int? BranchId { get; set; } // Для пользователей филиалов

    }
}
