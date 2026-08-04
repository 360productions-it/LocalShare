namespace LocalShare.Core.Models;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ConversationId { get; set; } = string.Empty;
    public string SenderDeviceId { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? FileTransferId { get; set; }
    public string? AttachmentFileName { get; set; }
    public long AttachmentSizeBytes { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
}

public enum ConversationType
{
    Direct = 0,
    Group = 1
}

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ConversationType Type { get; set; } = ConversationType.Direct;
    public string DisplayName { get; set; } = string.Empty;
    public string? TargetDeviceId { get; set; }
    public string? GroupId { get; set; }
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public int UnreadCount { get; set; }
}
