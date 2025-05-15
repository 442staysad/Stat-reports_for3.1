using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Core.Enums
{
    public enum ReportStatus
    {
        [Display(Name = "Черновик")]
        Draft,

        [Display(Name = "Нужна корректировка")]
        NeedsCorrection,

        [Display(Name = "Принято")]
        Reviewed,

        [Display(Name = "В работе")]
        InProgress

}
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>()?
                .Name ?? enumValue.ToString();
        }
    }
    public static class ReportStatusHelper
    {
        public static string GetCssClass(ReportStatus status) => status switch
        {
            ReportStatus.Draft => "bg-light text-dark",
            ReportStatus.InProgress => "bg-secondary",
            ReportStatus.NeedsCorrection => "bg-warning text-dark",
            ReportStatus.Reviewed => "bg-success",
            _ => "bg-secondary"
        };
    }
}
