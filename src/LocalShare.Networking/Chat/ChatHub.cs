using Microsoft.AspNetCore.SignalR;

namespace LocalShare.Networking.Chat;

public class ChatMessagePayload
{
    public string MessageId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string SenderDeviceId { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? FileTransferId { get; set; }
    public string? AttachmentFileName { get; set; }
    public long AttachmentSizeBytes { get; set; }
    public string SentAt { get; set; } = DateTime.UtcNow.ToString("o");
}

public interface IChatClient
{
    Task ReceiveMessage(ChatMessagePayload payload);
    Task ReceiveGroupMessage(ChatMessagePayload payload);
    Task ReceiveTyping(string senderDeviceId);
}

public class ChatHub : Hub<IChatClient>
{
    public async Task SendDirectMessage(ChatMessagePayload payload)
    {
        await Clients.Others.ReceiveMessage(payload);
    }

    public async Task SendGroupMessage(ChatMessagePayload payload)
    {
        await Clients.Others.ReceiveGroupMessage(payload);
    }

    public async Task SendTyping(string senderDeviceId)
    {
        await Clients.Others.ReceiveTyping(senderDeviceId);
    }
}
