using System.Text.Json;
using RestaurantApp.ViewModels;

namespace RestaurantApp.Services
{
    public static class SessionCartExtensions
    {
        private const string CartSessionKey = "Cart";

        public static List<CartItem> GetCart(this ISession session)
        {
            var json = session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(json)) return new List<CartItem>();

            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
            }
            catch (JsonException)
            {
                // Corrupt/old-format session data — treat as an empty cart rather than 500ing.
                return new List<CartItem>();
            }
        }

        public static void SaveCart(this ISession session, List<CartItem> cart)
        {
            session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        public static void ClearCart(this ISession session)
        {
            session.Remove(CartSessionKey);
        }
    }
}
