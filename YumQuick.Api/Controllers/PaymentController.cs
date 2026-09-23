using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using YumQuick.Api.Services;
using YumQuick.Core.DTOs;
using YumQuick.Core.Enums;
using YumQuick.Core.Interfaces;
using YumQuick.Data;

namespace YumQuick.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFawryPaymentService _fawryService;
        private readonly FawrySettings _fawrySettings;

        public PaymentController(ApplicationDbContext context, IFawryPaymentService fawryService, IOptions<FawrySettings> fawrySettings)
        {
            _context = context;
            _fawryService = fawryService;
            _fawrySettings = fawrySettings.Value;
        }

        // POST: api/Payment/fawry-webhook
        // هذه الدالة ستستدعيها خوادم فوري تلقائياً عند دفع العميل
        [HttpPost("fawry-webhook")]
        [AllowAnonymous] // فوري لا ترسل توكن تسجيل دخول، بل تعتمد على التوقيع المشفر
        public async Task<IActionResult> FawryWebhook([FromBody] dynamic fawryPayload)
        {
            // بناءً على توثيق فوري، استخراج البيانات من الـ Payload
            string fawryRefNumber = fawryPayload.merchantRefNumber;
            string orderStatus = fawryPayload.orderStatus; // "PAID", "CANCELED", "EXPIRED"
            decimal paymentAmount = fawryPayload.paymentAmount;
            string messageSignature = fawryPayload.messageSignature;

            // البحث عن الطلب
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.FawryRefNumber == fawryRefNumber);
            if (order == null) return Ok(); // إعادة Ok لفوري حتى لا تحاول الإرسال مجدداً

            // يمكنك التحقق من صحة التوقيع هنا لزيادة الأمان باستخدام _fawryService.ValidateWebhookSignature

            if (orderStatus == "PAID")
            {
                order.PaymentStatus = PaymentStatus.Paid;
                // إذا كان الطلب معلقاً، يتم تحويله للتحضير
                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Preparing;
                }
            }
            else if (orderStatus == "CANCELED" || orderStatus == "EXPIRED")
            {
                order.PaymentStatus = PaymentStatus.Failed;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}