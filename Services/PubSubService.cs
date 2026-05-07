using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using System.Text.Json;

namespace restaurant.Services;

public class PubSubService
{
    private readonly ILogger<PubSubService> _logger;
    private readonly string _projectId;
    private readonly PublisherClient _publisher;

    public PubSubService(ILogger<PubSubService> logger, IConfiguration config)
    {
        _logger = logger;
        _projectId = config["Authentication:Google:ProjectId"]!;
        TopicName topicName = TopicName.FromProjectTopic(_projectId, "menu-uploads-topic");
        _publisher = PublisherClient.CreateAsync(topicName).Result;
    }

    public async Task PublishMenuUploadAsync(string restaurantId, string menuId, string imagePath)
    {
        var message = new
        {
            restaurantId,
            menuId,
            imagePath
        };

        string json = JsonSerializer.Serialize(message);

        PubsubMessage pubsubMessage = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(json)
        };

        string messageId = await _publisher.PublishAsync(pubsubMessage);
        _logger.LogInformation("Published to Pub/Sub, messageId: {MessageId}", messageId);
    }
}
