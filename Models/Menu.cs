using Google.Cloud.Firestore;

namespace restaurant.Models;

[FirestoreData]
public class Menu
{
    [FirestoreProperty] public string MenuId { get; set; } = "";
    [FirestoreProperty] public string OcrText { get; set; } = "";
    [FirestoreProperty] public string Status { get; set; } = "pending";
    [FirestoreProperty] public List<MenuItem> Items { get; set; } = new();
    [FirestoreProperty] public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}

[FirestoreData]
public class MenuItem
{
    [FirestoreProperty] public string Name { get; set; } = "";
    [FirestoreProperty] public double Price { get; set; }
}
