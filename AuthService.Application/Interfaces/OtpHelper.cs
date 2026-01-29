using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces
{
    public static class OtpHelper
    {
        public static string GenerateOtp(int length = 6)
        {
            var random = new Random();
            var otp = new string(Enumerable.Range(0, length).Select(_ => (char)('0' + random.Next(10))).ToArray());
            return otp;
        }    
    }

}
