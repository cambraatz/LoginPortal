using System.Globalization;

namespace LoginPortal.Server.Models
{
    // DTO for Manifest Access Request
    public class ManifestAccessRequest
    {
        public string PowerUnit { get; set; } = string.Empty;

        // This property will receive the string in "MMDDYYYY" format from the client
        public string MfstDateString { get; set; } = string.Empty;

        // This read-only property provides the DateTime object after parsing MfstDateString.
        // It will return default(DateTime) if parsing fails.
        public DateTime MfstDate
        {
            get
            {
                if (DateTime.TryParseExact(MfstDateString, "MMddyyyy",
                                           CultureInfo.InvariantCulture,
                                           DateTimeStyles.None,
                                           out DateTime parsedDate))
                {
                    return parsedDate;
                }
                // Log a warning if parsing fails, but return default(DateTime) so the controller
                // can still check for request.MfstDate == default(DateTime).
                // In a more complex scenario, you might add a validation attribute or throw a specific exception.
                return default(DateTime);
            }
        }
    }
}
