using Google.Cloud.Firestore;
using restaurant.Models;

namespace restaurant.DataAccess;

public class MenuRepository
{
    private readonly ILogger<MenuRepository> _logger;
    private readonly FirestoreDb _db;

    public MenuRepository(ILogger<MenuRepository> logger, IConfiguration configuration)
    {
        _logger = logger;
        _db = FirestoreDb.Create(configuration["Authentication:Google:ProjectId"]);
    }

    // ── Restaurants ──────────────────────────────────────────────

    public async Task<string> CreateRestaurantAsync(Models.Restaurant restaurant)
    {
        restaurant.RestaurantId = Guid.NewGuid().ToString();
        await _db.Collection("restaurants")
                 .Document(restaurant.RestaurantId)
                 .SetAsync(restaurant);
        _logger.LogInformation("Restaurant {Id} created.", restaurant.RestaurantId);
        return restaurant.RestaurantId;
    }

    public async Task<Models.Restaurant?> GetRestaurantByNameAsync(string name)
    {
        QuerySnapshot snap = await _db.Collection("restaurants")
                                      .WhereEqualTo("Name", name)
                                      .GetSnapshotAsync();
        if (snap.Count == 0) return null;
        return snap.Documents[0].ConvertTo<Models.Restaurant>();
    }

    public async Task<List<Models.Restaurant>> GetAllRestaurantsAsync()
    {
        QuerySnapshot snap = await _db.Collection("restaurants").GetSnapshotAsync();
        return snap.Documents.Select(d => d.ConvertTo<Models.Restaurant>()).ToList();
    }

    //menus
    public async Task<string> AddMenuAsync(string restaurantId, Menu menu)
    {
        menu.MenuId = Guid.NewGuid().ToString();
        await _db.Collection("restaurants")
                 .Document(restaurantId)
                 .Collection("menus")
                 .Document(menu.MenuId)
                 .SetAsync(menu);
        _logger.LogInformation("Menu {MenuId} added to restaurant {RestaurantId}.",
            menu.MenuId, restaurantId);
        return menu.MenuId;
    }

    public async Task UpdateOcrTextAsync(string restaurantId, string menuId, string ocrText)
    {
        await _db.Collection("restaurants")
                 .Document(restaurantId)
                 .Collection("menus")
                 .Document(menuId)
                 .UpdateAsync(new Dictionary<string, object>
                 {
                     { "OcrText", ocrText },
                     { "Status", "pending" }
                 });
        _logger.LogInformation("OcrText updated for menu {MenuId}.", menuId);
    }

    public async Task UpdateMenuItemsAsync(string restaurantId, string menuId,
                                           List<MenuItem> items, string status = "confirmed")
    {
        var itemData = items.Select(i => new Dictionary<string, object>
        {
            { "Name", i.Name },
            { "Price", i.Price }
        }).ToList();

        await _db.Collection("restaurants")
                 .Document(restaurantId)
                 .Collection("menus")
                 .Document(menuId)
                 .UpdateAsync(new Dictionary<string, object>
                 {
                     { "Items", itemData },
                     { "Status", status }
                 });
        _logger.LogInformation("Menu {MenuId} items updated, status -> {Status}.", menuId, status);
    }

    public async Task<List<(string restaurantId, string menuId, Menu menu)>> GetPendingMenusAsync()
    {
        var results = new List<(string, string, Menu)>();
        QuerySnapshot restaurants = await _db.Collection("restaurants").GetSnapshotAsync();

        foreach (var restaurantDoc in restaurants.Documents)
        {
            QuerySnapshot menus = await restaurantDoc.Reference
                                                     .Collection("menus")
                                                     .WhereEqualTo("Status", "pending")
                                                     .GetSnapshotAsync();
            foreach (var menuDoc in menus.Documents)
            {
                results.Add((restaurantDoc.Id, menuDoc.Id, menuDoc.ConvertTo<Menu>()));
            }
        }
        return results;
    }

    public async Task<List<(Models.Restaurant restaurant, string menuId, Menu menu)>> GetAllConfirmedMenusAsync()
    {
        var results = new List<(Models.Restaurant, string, Menu)>();
        QuerySnapshot restaurants = await _db.Collection("restaurants").GetSnapshotAsync();

        foreach (var restaurantDoc in restaurants.Documents)
        {
            var restaurant = restaurantDoc.ConvertTo<Models.Restaurant>();
            QuerySnapshot menus = await restaurantDoc.Reference
                                                     .Collection("menus")
                                                     .WhereEqualTo("Status", "confirmed")
                                                     .GetSnapshotAsync();
            foreach (var menuDoc in menus.Documents)
            {
                results.Add((restaurant, menuDoc.Id, menuDoc.ConvertTo<Menu>()));
            }
        }
        return results;
    }

    // ── Images ────────────────────────────────────────────────────

    public async Task<string> AddImageReferenceAsync(string restaurantId,
                                                      string menuId,
                                                      MenuImage image)
    {
        image.ImageId = Guid.NewGuid().ToString();
        await _db.Collection("restaurants")
                 .Document(restaurantId)
                 .Collection("menus")
                 .Document(menuId)
                 .Collection("images")
                 .Document(image.ImageId)
                 .SetAsync(image);
        _logger.LogInformation("Image {ImageId} reference saved under menu {MenuId}.",
            image.ImageId, menuId);
        return image.ImageId;
    }
}
