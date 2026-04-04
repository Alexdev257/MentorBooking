using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public record SendOtpRegisterEvent(string ToEmail, string OTP) : IntegrationEvent;
}
