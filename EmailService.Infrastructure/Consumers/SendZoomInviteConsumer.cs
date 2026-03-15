using EmailService.Infrastructure.Services;
using MassTransit;
using Shared.Contracts.Events;
using System;
using System.Threading.Tasks;

namespace EmailService.Infrastructure.Consumers
{
    public class SendZoomInviteConsumer : IConsumer<SendZoomInviteEvent>
    {
        private readonly EmailSender _emailSender;

        public SendZoomInviteConsumer(EmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task Consume(ConsumeContext<SendZoomInviteEvent> context)
        {
            var msg = context.Message;
            Console.WriteLine($"[RabbitMQ] Sending Zoom invitation to: {msg.ToEmail}");

            try
            {
                string subject = $"Invitation: {msg.Topic} with {msg.MentorName}";
                string htmlBody = $@"
                <div style='font-family: Helvetica, Arial, sans-serif; min-width:1000px; overflow:auto; line-height:2'>
                  <div style='margin:50px auto; width:70%; padding:20px 0'>
                    <div style='border-bottom:1px solid #eee'>
                      <a href='' style='font-size:1.4em; color: #4CAF50; text-decoration:none; font-weight:600'>Mentor Booking</a>
                    </div>
                    <p style='font-size:1.1em'>Xin chào {msg.MenteeName},</p>
                    <p>Buổi Mentor của bạn đã được chấp nhận. Dưới đây là thông tin chi tiết:</p>
                    <ul>
                        <li><strong>Chủ đề:</strong> {msg.Topic}</li>
                        <li><strong>Mentor:</strong> {msg.MentorName}</li>
                        <li><strong>Bắt đầu:</strong> {msg.StartTime:dd/MM/yyyy HH:mm}</li>
                        <li><strong>Kết thúc:</strong> {msg.EndTime:dd/MM/yyyy HH:mm}</li>
                    </ul>
                    <p>Vui lòng tham gia cuộc họp Zoom theo đường dẫn bên dưới:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{msg.JoinUrl}' style='background-color: #4CAF50; color: white; padding: 14px 25px; text-align: center; text-decoration: none; display: inline-block; border-radius: 4px; font-weight: bold;'>Tham gia cuộc họp</a>
                    </div>
                    <p style='font-size:0.9em;'>Trân trọng,<br />Mentor Booking Team</p>
                    <hr style='border:none;border-top:1px solid #eee' />
                  </div>
                </div>";

                await _emailSender.SendAsync(msg.ToEmail, subject, htmlBody);
                Console.WriteLine($"[Success] Zoom invitation sent to {msg.ToEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to send Zoom invite: {ex.Message}");
                throw;
            }
        }
    }
}
