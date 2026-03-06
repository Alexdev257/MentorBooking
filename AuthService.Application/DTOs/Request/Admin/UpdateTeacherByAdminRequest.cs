using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTOs.Request.Admin
{
    public class UpdateTeacherByAdminRequest
    {
        [Required(ErrorMessage = "FullName is required")]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(500)]
        public IFormFile Avatar { get; set; }

        [MaxLength(50)]
        public string? Department { get; set; }
        public string? Specialization { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
