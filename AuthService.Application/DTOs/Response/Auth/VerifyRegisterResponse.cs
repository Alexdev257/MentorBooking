using SharedContracts.Common.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTOs.Response.Auth
{
    public class VerifyRegisterResponse : CommonResponse<VerifyOtpDTO> { }

    public class VerifyOtpDTO
    {
        public string? Email { get; set; }
        public string? Otp { get; set; }
    }
}
