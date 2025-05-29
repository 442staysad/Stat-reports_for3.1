using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Stat_reports.ViewModels
{
    public class EditDeadlineViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Филиал обязателен")]
        [Display(Name = "Филиал")]
        public int? BranchId { get; set; }

        [Required(ErrorMessage = "Шаблон отчета обязателен")]
        [Display(Name = "Шаблон отчета")]
        public int ReportTemplateId { get; set; }

        [Required(ErrorMessage = "Срок сдачи обязателен")]
        [DataType(DataType.Date)]
        [Display(Name = "Срок сдачи")]
        public DateTime DeadlineDate { get; set; }

        [Required(ErrorMessage = "Дата периода обязательна")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата периода (начало)")]
        public DateTime Period { get; set; }

        [Display(Name = "Закрыт (архивирован)")]
        public bool IsClosed { get; set; }

        // Для Dropdowns (будут заполняться в контроллере)
        public SelectList? Branches { get; set; }
        public SelectList? Templates { get; set; }

        // Отображение информации для удобства
        public string? BranchName { get; set; }
        public string? TemplateName { get; set; }
    }
}