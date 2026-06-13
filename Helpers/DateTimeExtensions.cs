using System;

namespace Tech_Store.Helpers
{
    public static class DateTimeExtensions
    {
        public static string TimeAgo(this DateTime dateTime)
        {
            var now = DateTime.Now;
            var normalizedDateTime = NormalizeForDisplay(dateTime, now);

            if (normalizedDateTime > now)
            {
                return "Vừa xong";
            }

            var timeSpan = now - normalizedDateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} tuần trước";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} tháng trước";

            return $"{(int)(timeSpan.TotalDays / 365)} năm trước";
        }

        private static DateTime NormalizeForDisplay(DateTime dateTime, DateTime referenceNow)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime.ToLocalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, referenceNow.Kind),
                _ => dateTime
            };
        }
    }
}
