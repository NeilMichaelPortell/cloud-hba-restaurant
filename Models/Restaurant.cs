using Google.Cloud.Firestore;

namespace restaurant.Models;

[FirestoreData]
public class Restaurant
{
    [FirestoreProperty] public string RestaurantId { get; set; } = "";
    [FirestoreProperty] public string Name { get; set; } = "";
    [FirestoreProperty] public string Location { get; set; } = "";
    [FirestoreProperty] public string Status { get; set; } = "pending";
    [FirestoreProperty] public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
