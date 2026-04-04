using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookingService.Application.DTOs.Response
{
    public class ZoomMeetingResponse
    {
        public long id { get; set; }
        public string join_url { get; set; } = string.Empty;
        public string start_url { get; set; } = string.Empty;
    }
}