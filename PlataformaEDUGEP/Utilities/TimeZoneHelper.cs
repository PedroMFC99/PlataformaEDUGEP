namespace PlataformaEDUGEP.AuxilliaryClasses
{
    public static class TimeZoneHelper
    {
        private const string LondonTimeZoneId = "GMT Standard Time";

        public static DateTime ConvertUtcToLondonTime(DateTime utcTime)
        {
            TimeZoneInfo londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LondonTimeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, londonTimeZone);
        }
    }
}
