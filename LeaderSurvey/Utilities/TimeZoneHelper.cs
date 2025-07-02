using System;

namespace LeaderSurvey.Utilities
{
    public static class TimeZoneHelper
    {
        private static readonly TimeZoneInfo CentralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

        /// <summary>
        /// Converts a UTC DateTime to Central Time (CST/CDT)
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime to convert</param>
        /// <returns>The DateTime in Central Time</returns>
        public static DateTime ConvertUtcToCentral(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, CentralTimeZone);
        }

        /// <summary>
        /// Converts a nullable UTC DateTime to Central Time (CST/CDT)
        /// </summary>
        /// <param name="utcDateTime">The nullable UTC DateTime to convert</param>
        /// <returns>The nullable DateTime in Central Time</returns>
        public static DateTime? ConvertUtcToCentral(DateTime? utcDateTime)
        {
            return utcDateTime.HasValue ? ConvertUtcToCentral(utcDateTime.Value) : null;
        }

        /// <summary>
        /// Gets the current time in Central Time Zone
        /// </summary>
        /// <returns>Current DateTime in Central Time</returns>
        public static DateTime GetCentralTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTimeZone);
        }

        /// <summary>
        /// Formats a UTC DateTime as a Central Time date string (MM/dd/yyyy)
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime to format</param>
        /// <returns>Formatted date string in Central Time</returns>
        public static string FormatUtcAsCentralDate(DateTime utcDateTime)
        {
            return ConvertUtcToCentral(utcDateTime).ToString("MM/dd/yyyy");
        }

        /// <summary>
        /// Formats a nullable UTC DateTime as a Central Time date string (MM/dd/yyyy)
        /// </summary>
        /// <param name="utcDateTime">The nullable UTC DateTime to format</param>
        /// <returns>Formatted date string in Central Time, or empty string if null</returns>
        public static string FormatUtcAsCentralDate(DateTime? utcDateTime)
        {
            return utcDateTime.HasValue ? FormatUtcAsCentralDate(utcDateTime.Value) : string.Empty;
        }
    }
}
