namespace PlataformaEDUGEP.AuxilliaryClasses
{
    /// <summary>
    /// Provides methods for time zone conversion.
    /// </summary>
    public static class TimeZoneHelper
    {
        /// <summary>
        /// The time zone identifier for London.
        /// </summary>
        private const string LondonTimeZoneId = "GMT Standard Time";

        /// <summary>
        /// Converts a UTC DateTime to London time.
        /// </summary>
        /// <param name="utcTime">The UTC DateTime to convert.</param>
        /// <returns>The DateTime converted to London time.</returns>
        /// <remarks>
        /// This method uses the TimeZoneInfo class to find the London time zone and convert the provided UTC DateTime to that time zone.
        /// It is useful for displaying times to users in London's local time regardless of the server's time zone.
        /// </remarks>
        public static DateTime ConvertUtcToLondonTime(DateTime utcTime)
        {
            TimeZoneInfo londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LondonTimeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, londonTimeZone);
        }
    }
}