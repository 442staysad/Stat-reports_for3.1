using Core.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace Stat_reports.ViewModels
{
    public class CreateTemplateViewModel
    {
        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        public ReportType Type { get; set; }

        [Required]
        public DeadlineType DeadlineType { get; set; }

        [Range(1, 31)]
        public int FixedDay { get; set; }

        [Required]
        public DateTime ReportDate { get; set; }

        public string? FilePath { get; set; }
        public IFormFile? File { get; set; }
    }
}
