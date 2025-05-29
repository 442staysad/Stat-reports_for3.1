using Core.Enums;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Stat_reports.ViewModels
{
    public class EditTemplateViewModel
    {
        public int Id { get; set; } // Скрытое поле для ID

        [Required(ErrorMessage = "Название обязательно")]
        [Display(Name = "Название")]
        public string Name { get; set; }

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Тип периодичности обязателен")]
        [Display(Name = "Тип периодичности")]
        public DeadlineType DeadlineType { get; set; }

        [Required(ErrorMessage = "Тип отчета обязателен")]
        [Display(Name = "Тип отчета")]
        public ReportType Type { get; set; }

        [Display(Name = "Текущий файл")]
        public string? CurrentFilePath { get; set; } // Показать имя текущего файла

        [Display(Name = "Загрузить новый файл (заменит текущий)")]
        public IFormFile? NewFile { get; set; } // Для загрузки нового файла

        // Для списка разрешенных типов (как в Create)
        public List<ReportType>? AllowedTypes { get; set; }
    }
}
