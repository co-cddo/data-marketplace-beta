using System;

namespace Cddo.Data.Marketplace.UI.Configuration
{
    public static class DateTimeExtensions
    {
        private static readonly TimeZoneInfo GmtTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        private static string GetOrdinalSuffix(int day)
        {
            if (day <= 0) return day.ToString();

            switch (day % 100)
            {
                case 11:
                case 12:
                case 13:
                    return day + "th";
            }

            switch (day % 10)
            {
                case 1:
                    return day + "st";
                case 2:
                    return day + "nd";
                case 3:
                    return day + "rd";
                default:
                    return day + "th";
            }
        }

        private static DateTime ConvertToGmt(DateTime dateTime)
        {
            // Assuming the input dateTime is in UTC
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, GmtTimeZone);
        }

        public static string ToCustomFormattedString(this DateTime dateTime)
        {
            dateTime = ConvertToGmt(dateTime);
            string dayWithSuffix = GetOrdinalSuffix(dateTime.Day);
            return $"{dateTime:HH:mm} {dayWithSuffix} {dateTime:MMM yyyy}";
        }

        public static string ToCustomFormattedString(this DateTime? dateTime)
        {
            return dateTime?.ToCustomFormattedString() ?? string.Empty;
        }

        public static string ToCustomFormattedStringWithAMPM(this DateTime dateTime)
        {
            dateTime = ConvertToGmt(dateTime);
            string dayWithSuffix = GetOrdinalSuffix(dateTime.Day);
            return $"{dateTime:HH:mm} - {dayWithSuffix} {dateTime:MMM yyyy}";
        }

        public static string ToCustomFormattedStringWithAMPM(this DateTime? dateTime)
        {
            return dateTime?.ToCustomFormattedStringWithAMPM() ?? string.Empty;
        }

        public static string ToDateStringWithOrdinal(this DateTime dateTime)
        {
            dateTime = ConvertToGmt(dateTime);
            string dayWithSuffix = GetOrdinalSuffix(dateTime.Day);
            return $"{dayWithSuffix} {dateTime:MMM yyyy}";
        }

        public static string ToDateStringWithOrdinal(this DateTime? dateTime)
        {
            return dateTime?.ToDateStringWithOrdinal() ?? string.Empty;
        }

        public static string ToDateStringAsDDMMYYYY(this DateTime dateTime)
        {
            dateTime = ConvertToGmt(dateTime);
            return dateTime.ToString("dd/MM/yyyy");
        }

        public static string ToDateStringAsDDMMYYYY(this DateTime? dateTime)
        {
            return dateTime?.ToDateStringAsDDMMYYYY() ?? string.Empty;
        }

        public static string ToCustomFormattedStringWithAMPMAndDDMMYYYY(this DateTime dateTime)
        {
            dateTime = ConvertToGmt(dateTime);
            string dayWithSuffix = GetOrdinalSuffix(dateTime.Day);
            return $"{dateTime:HH:mm} - {dayWithSuffix} {dateTime:MMM yyyy} ({dateTime:dd/MM/yyyy})";
        }

        public static string ToCustomFormattedStringWithAMPMAndDDMMYYYY(this DateTime? dateTime)
        {
            return dateTime?.ToCustomFormattedStringWithAMPMAndDDMMYYYY() ?? string.Empty;
        }

        public static string ToLastUpdatedString(this DateTime dateTime)
        {
            dateTime = ConvertToGmt(dateTime);
            return $"Last Updated: {dateTime:HH:mm} - {dateTime:dd/MM/yyyy}";
        }

        public static string ToLastUpdatedString(this DateTime? dateTime)
        {
            return dateTime?.ToLastUpdatedString() ?? "Last Updated: N/A";
        }

        public static string ToGDSHelperString(this DateTime dateTime)
        {
            dateTime = ConvertToGmt(dateTime);
            return $"{dateTime:HH:mm} - {dateTime:dd/MM/yyyy}";
        }

        public static string ToGDSHelperString(this DateTime? dateTime)
        {
            return dateTime?.ToGDSHelperString() ?? string.Empty;
        }
    }
}
