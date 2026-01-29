using EmailService.Infrastructure.Services;
using MassTransit;
using MassTransit.Internals;
using SharedContracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailService.Infrastructure.Consumers
{
    public class SendOtpRegisterConsumer : IConsumer<SendOtpRegisterEvent>
    {
        private readonly EmailSender _emailSender;
        public SendOtpRegisterConsumer(EmailSender emailSender)
        {
            _emailSender = emailSender;
        }
        public async Task Consume(ConsumeContext<SendOtpRegisterEvent> context)
        {
            var msg = context.Message;
            Console.WriteLine($"[RabbitMQ] Received request to send email to: {msg.ToEmail}");
            try
            {
                string subject = "Verify registering account - EventManagement";
                string htmlBody = $@"
                <div style='font-family: Helvetica, Arial, sans-serif; min-width:1000px; overflow:auto; line-height:2'>
                  <div style='margin:50px auto; width:70%; padding:20px 0'>
                    <div style='border-bottom:1px solid #eee'>
                      <a href='' style='font-size:1.4em; color: #00466a; text-decoration:none; font-weight:600'>Event Management</a>
                    </div>
                    <p style='font-size:1.1em'>Xin chào,</p>
                    <p>Cảm ơn bạn đã đăng ký. Sử dụng mã OTP sau để hoàn tất quá trình đăng ký của bạn. Mã có hiệu lực trong 5 phút.</p>
                    <h2 style='background: #00466a; margin: 0 auto; width: max-content; padding: 0 10px; color: #fff; border-radius: 4px;'>{msg.OTP}</h2>
                    <p style='font-size:0.9em;'>Xin cảm ơn,<br />Event Management Team</p>
                    <hr style='border:none;border-top:1px solid #eee' />
                  </div>
                </div>";
                await _emailSender.SendAsync(msg.ToEmail, subject, htmlBody);
                Console.WriteLine($"[Success] Email sent to {msg.ToEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to send email: {ex.Message}");
                throw;
            }
        }
    }
}
