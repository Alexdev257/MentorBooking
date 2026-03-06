using SharedContracts.Common.Wrappers.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTOs.Request.Review
{
    public class ReviewGetListRequest : PaginationRequest
    {
        public Guid? BookingId { get; set; }
        public Guid? MentorId { get; set; }
        public Guid? MenteeId { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public bool? Sorting { get; set; }
        public string? SortBy { get; set; }
    }
}
