using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Core.DTO
{
    public class BranchChangePasswordDto
    {
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Текущий пароль филиала обязателен")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Новый пароль филиала обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }
}
