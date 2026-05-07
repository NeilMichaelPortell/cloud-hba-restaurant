using Google.Cloud.Translation.V2;

namespace restaurant.Services;

public class TranslationService
{
    private readonly ILogger<TranslationService> _logger;
    private readonly TranslationClient _client;

    public TranslationService(ILogger<TranslationService> logger)
    {
        _logger = logger;
        _client = TranslationClient.Create();
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage)
    {
        try
        {
            TranslationResult result = await _client.TranslateTextAsync(
                text, targetLanguage);

            _logger.LogInformation("Translated '{Text}' to {Lang}: '{Result}'",
                text, targetLanguage, result.TranslatedText);

            return result.TranslatedText;
        }
        catch (Exception ex)
        {
            _logger.LogError("Translation failed: {Message}", ex.Message);
            throw;
        }
    }
}