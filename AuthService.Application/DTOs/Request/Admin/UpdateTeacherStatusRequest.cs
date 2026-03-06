using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTOs.Request.Admin
{
    public class UpdateTeacherStatusRequest
    {
        public bool IsActive { get; set; }
    }
}
