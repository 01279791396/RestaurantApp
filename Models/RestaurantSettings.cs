using System.ComponentModel.DataAnnotations;

namespace RestaurantApp.Models
{
    // Singleton settings row (always Id = 1 — see DbInitializer, which seeds it, and
    // Admin/SettingsController, which is the only place that edits it).
    public class RestaurantSettings
    {
        public int Id { get; set; }

        [Required]
        public TimeSpan OpenTime { get; set; } = new TimeSpan(10, 0, 0);

        [Required]
        public TimeSpan CloseTime { get; set; } = new TimeSpan(23, 0, 0);

        // Lets the admin close ordering outright (holiday, ran out of stock for the day, etc.)
        // even during normal opening hours, without having to change OpenTime/CloseTime.
        public bool IsTemporarilyClosed { get; set; } = false;

        // Whether the restaurant is open right now, given the time of day. Handles the
        // overnight case too (e.g. open 18:00, close 02:00 — "close" is earlier than "open").
        public bool IsOpenAt(DateTime now)
        {
            if (IsTemporarilyClosed) return false;

            var time = now.TimeOfDay;
            if (OpenTime <= CloseTime)
            {
                return time >= OpenTime && time < CloseTime;
            }
            // Overnight window (crosses midnight).
            return time >= OpenTime || time < CloseTime;
        }
    }
}
