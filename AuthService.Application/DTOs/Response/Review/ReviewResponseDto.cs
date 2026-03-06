using AuthService.Application.DTOs.Response.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.DTOs.Response.Review
{
    public class ReviewResponseDto
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public UserDto? Mentor { get; set; }
        public UserDto? Mentee { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
