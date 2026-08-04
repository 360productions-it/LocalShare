using LocalShare.Common;
using LocalShare.Core.Models;

namespace LocalShare.Core.Interfaces;

public interface IChatService
{
    event EventHandler<Message>? MessageReceived;
    event EventHandler<string>? TypingIndicatorReceived;

    Task<Result<Message>> SendDirectMessageAsync(Peer targetPeer, string body, string? attachmentPath = null, CancellationToken cancellationToken = default);
    Task<Result<Message>> SendGroupMessageAsync(Group group, IEnumerable<Peer> onlineMembers, string body, string? attachmentPath = null, CancellationToken cancellationToken = default);
    Task SendTypingNotificationAsync(Peer targetPeer);
    Task<IReadOnlyList<Conversation>> GetConversationsAsync();
    Task<IReadOnlyList<Message>> GetMessagesAsync(string conversationId, int limit = 50);
}
