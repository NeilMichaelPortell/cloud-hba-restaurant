using Google.Cloud.Firestore;

namespace restaurant.Models;

[FirestoreData]
public class MenuImage
{
    [FirestoreProperty] public string ImageId { get; set; } = "";
    [FirestoreProperty] public string StoragePath { get; set; } = "";
    [FirestoreProperty] public string UploadedBy { get; set; } = "";
    [FirestoreProperty] public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
