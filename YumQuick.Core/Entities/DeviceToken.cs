using System;

namespace YumQuick.Core.Entities
{
    public class DeviceToken
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Token { get; set; } // الـ FCM Token القادم من الموبايل

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}