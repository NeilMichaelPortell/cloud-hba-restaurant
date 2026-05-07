using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using restaurant.DataAccess;
using restaurant.Interfaces;
using restaurant.Models;
using restaurant.Services;
using System.Text;
using System.Text.Json;

namespace restaurant.Controllers;

public class MenuController : Controller
{
    private readonly MenuRepository _repo;
    private readonly ILogger<MenuController> _logger;
    private readonly IBucketStorageService _bucketStorageService;
    private readonly PubSubService _pubSubService;
    private readonly CacheService _cacheService;
    private readonly IConfiguration _configuration;

    public MenuController(
        ILogger<MenuController> logger,
        MenuRepository repo,
        IBucketStorageService bucketStorageService,
        PubSubService pubSubService,
        CacheService cacheService,
        IConfiguration configuration)
    {
        _repo = repo;
        _logger = logger;
        _bucketStorageService = bucketStorageService;
        _pubSubService = pubSubService;
        _cacheService = cacheService;
        _configuration = configuration;
    }

    [Authorize]
    public IActionResult Upload()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile file, string restaurantName)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file provided." });

            if (string.IsNullOrWhiteSpace(restaurantName))
                return BadRequest(new { success = false, message = "Restaurant name is required." });

            Models.Restaurant? restaurant = await _repo.GetRestaurantByNameAsync(restaurantName);

            string restaurantId;

            if (restaurant == null)
            {
                restaurantId = await _repo.CreateRestaurantAsync(new Models.Restaurant
                {
                    Name = restaurantName
                });
            }
            else
            {
                restaurantId = restaurant.RestaurantId;
            }

            string menuId = await _repo.AddMenuAsync(restaurantId, new Menu());

            string fileNameForStorage = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string imageUrl = await _bucketStorageService.UploadFileAsync(file, fileNameForStorage);

            await _repo.AddImageReferenceAsync(restaurantId, menuId, new MenuImage
            {
                StoragePath = imageUrl,
                UploadedBy = User.Identity?.Name ?? "unknown"
            });

            await _pubSubService.PublishMenuUploadAsync(restaurantId, menuId, imageUrl);

            // Assignment requirement: when a new menu is uploaded, old cached translations
            // for that menu/restaurant should be invalidated.
            await _cacheService.InvalidateMenuCacheAsync(restaurantId, menuId);

            _logger.LogInformation(
                "Image uploaded and published to Pub/Sub for restaurant {Name}",
                restaurantName
            );

            return Ok(new
            {
                success = true,
                imageUrl,
                restaurantId,
                menuId
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Upload error.");

            return StatusCode(500, new
            {
                success = false,
                message = e.Message
            });
        }
    }

    [Authorize]
    public async Task<IActionResult> Index(string? search, string? sort)
    {
        var confirmedMenus = await _repo.GetAllConfirmedMenusAsync();

        var items = confirmedMenus.SelectMany(r =>
            r.menu.Items.Select(i => new CatalogItem
            {
                RestaurantId = r.restaurant.RestaurantId,
                RestaurantName = r.restaurant.Name,
                MenuId = r.menuId,
                ItemName = i.Name,
                Price = i.Price
            })
        ).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            items = items
                .Where(i => i.ItemName.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        items = sort == "desc"
            ? items.OrderByDescending(i => i.Price).ToList()
            : items.OrderBy(i => i.Price).ToList();

        ViewBag.Search = search;
        ViewBag.Sort = sort ?? "asc";

        return View(items);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Translate(
        string menuId,
        string restaurantId,
        string text,
        string language)
    {
        if (string.IsNullOrWhiteSpace(menuId) ||
            string.IsNullOrWhiteSpace(restaurantId) ||
            string.IsNullOrWhiteSpace(text) ||
            string.IsNullOrWhiteSpace(language))
        {
            return BadRequest(new
            {
                translated = "Missing menuId, restaurantId, text, or language."
            });
        }

        try
        {
            // 1. Check Redis cache first
            string? cached = await _cacheService.GetTranslationAsync(
                restaurantId,
                menuId,
                language,
                text
            );

            if (!string.IsNullOrWhiteSpace(cached))
            {
                _logger.LogInformation(
                    "Returning cached translation for restaurant {RestaurantId}, menu {MenuId}, language {Language}, text {Text}",
                    restaurantId,
                    menuId,
                    language,
                    text
                );

                return Ok(new
                {
                    translated = cached,
                    fromCache = true
                });
            }

            // 2. If not cached, call Cloud Run / HTTP translation service
            string? translateUrl = _configuration["CloudFunctions:TranslateUrl"];

            if (string.IsNullOrWhiteSpace(translateUrl))
            {
                return StatusCode(500, new
                {
                    translated = "Translation failed.",
                    details = "CloudFunctions:TranslateUrl is missing from configuration."
                });
            }

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            var payload = new
            {
                text,
                language
            };

            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync(translateUrl, content);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Translation service failed. Status: {StatusCode}, Body: {Body}",
                    response.StatusCode,
                    responseBody
                );

                return StatusCode(500, new
                {
                    translated = "Translation failed.",
                    statusCode = response.StatusCode.ToString(),
                    details = responseBody
                });
            }

            using var doc = JsonDocument.Parse(responseBody);

            string translated = doc.RootElement
                .GetProperty("translatedText")
                .GetString() ?? "";

            if (string.IsNullOrWhiteSpace(translated))
            {
                return StatusCode(500, new
                {
                    translated = "Translation failed.",
                    details = "Cloud Run returned empty translatedText.",
                    raw = responseBody
                });
            }

            // 3. Store result in Redis cache
            await _cacheService.SetTranslationAsync(
                restaurantId,
                menuId,
                language,
                text,
                translated
            );

            return Ok(new
            {
                translated,
                fromCache = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed.");

            return StatusCode(500, new
            {
                translated = "Translation failed.",
                details = ex.Message
            });
        }
    }
}

public class CatalogItem
{
    public string RestaurantId { get; set; } = "";
    public string RestaurantName { get; set; } = "";
    public string MenuId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public double Price { get; set; }
}