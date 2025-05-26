using System.ComponentModel.DataAnnotations;

namespace Stat_reports.ViewModels
{
    public class UserChangePasswordViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Текущий пароль обязателен")]
        [DataType(DataType.Password)]
        [Display(Name = "Текущий пароль")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "{0} должен быть не менее {2} и не более {1} символов.")]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтвердите новый пароль")]
        [Compare("NewPassword", ErrorMessage = "Новый пароль и его подтверждение не совпадают.")]
        public string ConfirmNewPassword { get; set; }
    }
}
