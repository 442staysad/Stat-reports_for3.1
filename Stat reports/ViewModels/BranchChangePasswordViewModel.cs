using System.ComponentModel.DataAnnotations;

namespace Stat_reports.ViewModels
{
    public class BranchChangePasswordViewModel
    {
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Текущий пароль филиала обязателен")]
        [DataType(DataType.Password)]
        [Display(Name = "Текущий пароль филиала")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Новый пароль филиала обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "{0} должен быть не менее {2} и не более {1} символов.")]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль филиала")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтвердите новый пароль филиала")]
        [Compare("NewPassword", ErrorMessage = "Новый пароль филиала и его подтверждение не совпадают.")]
        public string ConfirmNewPassword { get; set; }
    }
}
