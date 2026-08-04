using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using LocalShare.Common;
using LocalShare.Core.Interfaces;
using LocalShare.Core.Models;

namespace LocalShare.Networking.Chat;

public class ChatService : IChatService
{
    private readonly Profile _localProfile;
    private readonly IMessageRepository _messageRepo;
    private readonly ITransferService _transferService;
    private readonly ConcurrentDictionary<string, HubConnection> _hubConnections = new();

    public event EventHandler<Message>? MessageReceived;
    public event EventHandler<string>? TypingIndicatorReceived;

    public ChatService(Profile localProfile, IMessageRepository messageRepo, ITransferService transferService)
    {
        _localProfile = localProfile;
        _messageRepo = messageRepo;
        _transferService = transferService;
    }

    public async Task<Result<Message>> SendDirectMessageAsync(Peer targetPeer, string body, string? attachmentPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var hub = await GetOrCreateConnectionAsync(targetPeer.IpAddress, targetPeer.HttpPort, cancellationToken);
            if (hub.State != HubConnectionState.Connected)
                return Result<Message>.Failure("Could not connect to peer chat endpoint.");

            string? transferId = null;
            string? fileName = null;
            long sizeBytes = 0;

            if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath))
            {
                var sendRes = await _transferService.SendFileAsync(targetPeer, attachmentPath, null, cancellationToken);
                if (sendRes.IsSuccess && sendRes.Value != null)
                {
                    transferId = sendRes.Value.Id;
                    fileName = sendRes.Value.FileName;
                    sizeBytes = sendRes.Value.SizeBytes;
                }
            }

            var msgId = Guid.NewGuid().ToString("N");
            var conversationId = targetPeer.DeviceId;
            var sentAt = DateTime.UtcNow;

            var payload = new ChatMessagePayload
            {
                MessageId = msgId,
                ConversationId = conversationId,
                SenderDeviceId = _localProfile.DeviceId,
                SenderDisplayName = _localProfile.DisplayName,
                Body = body,
                FileTransferId = transferId,
                AttachmentFileName = fileName,
                AttachmentSizeBytes = sizeBytes,
                SentAt = sentAt.ToString("o")
            };

            await hub.InvokeAsync("SendDirectMessage", payload, cancellationToken);

            var message = new Message
            {
                Id = msgId,
                ConversationId = conversationId,
                SenderDeviceId = _localProfile.DeviceId,
                SenderDisplayName = _localProfile.DisplayName,
                Body = body,
                FileTransferId = transferId,
                AttachmentFileName = fileName,
                AttachmentSizeBytes = sizeBytes,
                SentAt = sentAt,
                DeliveredAt = sentAt
            };

            await _messageRepo.SaveMessageAsync(message);
            return Result<Message>.Success(message);
        }
        catch (Exception ex)
        {
            return Result<Message>.Failure($"Failed to send message: {ex.Message}");
        }
    }

    public async Task<Result<Message>> SendGroupMessageAsync(Group group, IEnumerable<Peer> onlineMembers, string body, string? attachmentPath = null, CancellationToken cancellationToken = default)
    {
        var msgId = Guid.NewGuid().ToString("N");
        var sentAt = DateTime.UtcNow;

        string? transferId = null;
        string? fileName = null;
        long sizeBytes = 0;

        foreach (var memberPeer in onlineMembers)
        {
            if (memberPeer.DeviceId == _localProfile.DeviceId) continue;

            try
            {
                var hub = await GetOrCreateConnectionAsync(memberPeer.IpAddress, memberPeer.HttpPort, cancellationToken);
                if (hub.State == HubConnectionState.Connected)
                {
                    if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath) && transferId == null)
                    {
                        var sendRes = await _transferService.SendFileAsync(memberPeer, attachmentPath, null, cancellationToken);
                        if (sendRes.IsSuccess && sendRes.Value != null)
                        {
                            transferId = sendRes.Value.Id;
                            fileName = sendRes.Value.FileName;
                            sizeBytes = sendRes.Value.SizeBytes;
                        }
                    }

                    var payload = new ChatMessagePayload
                    {
                        MessageId = msgId,
                        GroupId = group.Id,
                        SenderDeviceId = _localProfile.DeviceId,
                        SenderDisplayName = _localProfile.DisplayName,
                        Body = body,
                        FileTransferId = transferId,
                        AttachmentFileName = fileName,
                        AttachmentSizeBytes = sizeBytes,
                        SentAt = sentAt.ToString("o")
                    };

                    await hub.InvokeAsync("SendGroupMessage", payload, cancellationToken);
                }
            }
            catch
            {
                // Continue fanout to other online members
            }
        }

        var message = new Message
        {
            Id = msgId,
            ConversationId = group.Id,
            SenderDeviceId = _localProfile.DeviceId,
            SenderDisplayName = _localProfile.DisplayName,
            Body = body,
            FileTransferId = transferId,
            AttachmentFileName = fileName,
            AttachmentSizeBytes = sizeBytes,
            SentAt = sentAt,
            DeliveredAt = sentAt
        };

        await _messageRepo.SaveMessageAsync(message);
        return Result<Message>.Success(message);
    }

    public async Task SendTypingNotificationAsync(Peer targetPeer)
    {
        try
        {
            var hub = await GetOrCreateConnectionAsync(targetPeer.IpAddress, targetPeer.HttpPort);
            if (hub.State == HubConnectionState.Connected)
            {
                await hub.InvokeAsync("SendTyping", _localProfile.DeviceId);
            }
        }
        catch { }
    }

    public async Task<IReadOnlyList<Conversation>> GetConversationsAsync() => await _messageRepo.GetConversationsAsync();

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(string conversationId, int limit = 50) => await _messageRepo.GetMessagesAsync(conversationId, limit);

    private async Task<HubConnection> GetOrCreateConnectionAsync(string ip, int port, CancellationToken ct = default)
    {
        var key = $"{ip}:{port}";
        if (_hubConnections.TryGetValue(key, out var existingHub) && existingHub.State == HubConnectionState.Connected)
        {
            return existingHub;
        }

        var url = $"http://{ip}:{port}/hub/chat";
        var hub = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        hub.On<ChatMessagePayload>("ReceiveMessage", async (payload) =>
        {
            var msg = new Message
            {
                Id = payload.MessageId,
                ConversationId = payload.SenderDeviceId,
                SenderDeviceId = payload.SenderDeviceId,
                SenderDisplayName = payload.SenderDisplayName,
                Body = payload.Body,
                FileTransferId = payload.FileTransferId,
                AttachmentFileName = payload.AttachmentFileName,
                AttachmentSizeBytes = payload.AttachmentSizeBytes,
                SentAt = DateTime.TryParse(payload.SentAt, out DateTime dt) ? dt : DateTime.UtcNow,
                DeliveredAt = DateTime.UtcNow
            };

            await _messageRepo.SaveMessageAsync(msg);
            MessageReceived?.Invoke(this, msg);
        });

        hub.On<ChatMessagePayload>("ReceiveGroupMessage", async (payload) =>
        {
            var msg = new Message
            {
                Id = payload.MessageId,
                ConversationId = payload.GroupId,
                SenderDeviceId = payload.SenderDeviceId,
                SenderDisplayName = payload.SenderDisplayName,
                Body = payload.Body,
                FileTransferId = payload.FileTransferId,
                AttachmentFileName = payload.AttachmentFileName,
                AttachmentSizeBytes = payload.AttachmentSizeBytes,
                SentAt = DateTime.TryParse(payload.SentAt, out DateTime dt) ? dt : DateTime.UtcNow,
                DeliveredAt = DateTime.UtcNow
            };

            await _messageRepo.SaveMessageAsync(msg);
            MessageReceived?.Invoke(this, msg);
        });

        hub.On<string>("ReceiveTyping", (senderId) =>
        {
            TypingIndicatorReceived?.Invoke(this, senderId);
        });

        await hub.StartAsync(ct);
        _hubConnections[key] = hub;
        return hub;
    }
}
