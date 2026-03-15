using EmailService.Infrastructure.Services;
using MassTransit;
using Shared.Contracts.Events;
using System;
using System.Threading.Tasks;

namespace EmailService.Infrastructure.Consumers
{
    public class SendZoomHostInviteConsumer : IConsumer<SendZoomHostInviteEvent>
    {
        private readonly EmailSender _emailSender;

        public SendZoomHostInviteConsumer(EmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task Consume(ConsumeContext<SendZoomHostInviteEvent> context)
        {
            var msg = context.Message;
            Console.WriteLine($"[RabbitMQ] Sending Zoom Host invitation to Mentor: {msg.MentorEmail}");

            try
            {
                string subject = $"Host Link: {msg.Topic}";
                string htmlBody = $@"
                <div style='font-family: Helvetica, Arial, sans-serif; min-width:1000px; overflow:auto; line-height:2'>
                  <div style='margin:50px auto; width:70%; padding:20px 0'>
                    <div style='border-bottom:1px solid #eee'>
                      <a href='' style='font-size:1.4em; color: #00466a; text-decoration:none; font-weight:600'>Mentor Booking - Host Access</a>
                    </div>
                    <p style='font-size:1.1em'>Xin chào Mentor {msg.MentorName},</p>
                    <p>Buổi dạy của bạn đã được xác nhận. Với tư cách là **Host (Người chủ trì)**, vui lòng sử dụng link bên dưới để bắt đầu cuộc họp:</p>
                    <ul>
                        <li><strong>Chủ đề:</strong> {msg.Topic}</li>
                        <li><strong>Bắt đầu:</strong> {msg.StartTime:dd/MM/yyyy HH:mm}</li>
                        <li><strong>Kết thúc:</strong> {msg.EndTime:dd/MM/yyyy HH:mm}</li>
                    </ul>
                    <p style='color: #d9534f; font-weight: bold;'>Lưu ý: Link này chỉ dành riêng cho Mentor để mở phòng họp với quyền Host.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{msg.HostUrl}' style='background-color: #00466a; color: white; padding: 14px 25px; text-align: center; text-decoration: none; display: inline-block; border-radius: 4px; font-weight: bold;'>Bắt đầu cuộc họp (Host Only)</a>
                    </div>
                    <p style='font-size:0.9em;'>Trân trọng,<br />Mentor Booking Team</p>
                    <hr style='border:none;border-top:1px solid #eee' />
                  </div>
                </div>";

                await _emailSender.SendAsync(msg.MentorEmail, subject, htmlBody);
                Console.WriteLine($"[Success] Zoom Host invitation sent to {msg.MentorEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to send Zoom Host invite: {ex.Message}");
                throw;
            }
        }
    }
}
