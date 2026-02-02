using Shared.Contracts.Common.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Interfaces
{
    public interface IValidatable<TResponse> where TResponse : CommonResponseBase
    {
        Task<TResponse> ValidateAsync();
    }
}
