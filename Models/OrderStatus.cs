namespace RestaurantApp.Models
{
    // The lifecycle of an order. Each value only moves forward (enforced in OrderController),
    // except Cancelled which can happen from Pending or Accepted.
    public enum OrderStatus
    {
        Pending = 0,        // Just placed, waiting for restaurant to accept
        Accepted = 1,       // Restaurant accepted, about to start preparing
        Preparing = 2,      // Kitchen is preparing the order
        OutForDelivery = 3, // Handed to a delivery person
        Delivered = 4,      // Completed
        Cancelled = 5
    }
}
