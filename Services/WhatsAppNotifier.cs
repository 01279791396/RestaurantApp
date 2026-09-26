using RestaurantApp.Models;

namespace RestaurantApp.Services
{
    public interface IWhatsAppNotifier
    {
        Task NotifyNewOrderAsync(Order order);
    }

    // Uses the free CallMeBot WhatsApp API (https://www.callmebot.com) — a single phone
    // number (the admin's own WhatsApp) receives a message for every new order. No WhatsApp
    // Business account or approval process needed, which is the right trade-off for a single
    // restaurant that just wants a personal alert, not customer-facing messaging.
    public class WhatsAppNotifier : IWhatsAppNotifier
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppNotifier> _logger;

        public WhatsAppNotifier(HttpClient httpClient, IConfiguration configuration, ILogger<WhatsAppNotifier> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task NotifyNewOrderAsync(Order order)
        {
            var enabled = _configuration.GetValue<bool>("WhatsAppNotify:Enabled");
            var phone = _configuration["WhatsAppNotify:Phone"];
            var apiKey = _configuration["WhatsAppNotify:ApiKey"];

            if (!enabled || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(apiKey))
            {
                return; // not configured — silently skip, this must never block order placement
            }

            var itemsSummary = string.Join(", ", order.OrderItems.Select(i =>
                $"{i.ItemNameAtOrderTime}{(i.SizeNameAtOrderTime != null ? $" ({i.SizeNameAtOrderTime})" : "")} x{i.Quantity}"));

            var message =
                $"🔔 أوردر جديد #{order.InvoiceNumber}\n" +
                $"العميل: {order.Customer?.FullName} - {order.PhoneNumber}\n" +
                $"العنوان: {order.DeliveryAddress}\n" +
                $"الأصناف: {itemsSummary}\n" +
                $"الإجمالي: {order.TotalPrice:0.00} ج.م";

            try
            {
                var url = "https://api.callmebot.com/whatsapp.php" +
                          $"?phone={Uri.EscapeDataString(phone)}" +
                          $"&text={Uri.EscapeDataString(message)}" +
                          $"&apikey={Uri.EscapeDataString(apiKey)}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("WhatsApp notification failed with status {Status} for order {Invoice}",
                        response.StatusCode, order.InvoiceNumber);
                }
            }
            catch (Exception ex)
            {
                // A WhatsApp delivery hiccup (network, CallMeBot downtime, bad config) must
                // never surface to the customer or stop their order from going through.
                _logger.LogWarning(ex, "WhatsApp notification threw for order {Invoice}", order.InvoiceNumber);
            }
        }
    }
}
