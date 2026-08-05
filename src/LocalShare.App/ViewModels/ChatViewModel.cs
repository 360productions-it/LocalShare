using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalShare.Core.Interfaces;
using LocalShare.Core.Models;

namespace LocalShare.App.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly IChatService _chatService;
    private readonly IDiscoveryService _discoveryService;
    private readonly IPeerRepository _peerRepo;
    private readonly IGroupRepository _groupRepo;

    [ObservableProperty]
    private ObservableCollection<Conversation> _conversations = new();

    [ObservableProperty]
    private Conversation? _selectedConversation;

    [ObservableProperty]
    private ObservableCollection<Message> _messages = new();

    [ObservableProperty]
    private string _messageInput = string.Empty;

    [ObservableProperty]
    private string? _selectedAttachmentPath;

    [ObservableProperty]
    private string _typingText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusMessage;

    public ChatViewModel(
        IChatService chatService,
        IDiscoveryService discoveryService,
        IPeerRepository peerRepo,
        IGroupRepository groupRepo)
    {
        _chatService = chatService;
        _discoveryService = discoveryService;
        _peerRepo = peerRepo;
        _groupRepo = groupRepo;

        _chatService.MessageReceived += OnMessageReceived;
        _chatService.TypingIndicatorReceived += OnTypingReceived;

        _ = LoadConversationsAsync();
    }

    partial void OnStatusMessageChanged(string value)
    {
        HasStatusMessage = !string.IsNullOrWhiteSpace(value);
    }

    public async Task LoadConversationsAsync()
    {
        var convs = await _chatService.GetConversationsAsync();
        App.Current.Dispatcher.Invoke(() =>
        {
            Conversations.Clear();
            foreach (var c in convs) Conversations.Add(c);
        });
    }

    public async Task OpenConversationWithPeerAsync(Peer peer)
    {
        await LoadConversationsAsync();
        var existing = Conversations.FirstOrDefault(c =>
            c.Type == ConversationType.Direct &&
            (c.TargetDeviceId == peer.DeviceId || c.Id == peer.DeviceId));

        if (existing != null)
        {
            SelectedConversation = existing;
        }
        else
        {
            var newConv = new Conversation
            {
                Id = peer.DeviceId,
                Type = ConversationType.Direct,
                DisplayName = peer.DisplayName,
                TargetDeviceId = peer.DeviceId,
                LastMessageAt = DateTime.UtcNow
            };
            Conversations.Insert(0, newConv);
            SelectedConversation = newConv;
        }
    }

    partial void OnSelectedConversationChanged(Conversation? value)
    {
        StatusMessage = string.Empty;
        if (value != null)
        {
            _ = LoadMessagesForConversationAsync(value.Id);
        }
    }

    private async Task LoadMessagesForConversationAsync(string conversationId)
    {
        var targetId = SelectedConversation?.TargetDeviceId ?? conversationId;
        var msgs = await _chatService.GetMessagesAsync(targetId);

        if (msgs.Count == 0 && targetId != conversationId)
        {
            msgs = await _chatService.GetMessagesAsync(conversationId);
        }

        App.Current.Dispatcher.Invoke(() =>
        {
            Messages.Clear();
            foreach (var m in msgs) Messages.Add(m);
        });
    }

    [RelayCommand]
    private void SelectAttachment()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog();
        if (dlg.ShowDialog() == true)
        {
            SelectedAttachmentPath = dlg.FileName;
        }
    }

    [RelayCommand]
    private void ClearAttachment()
    {
        SelectedAttachmentPath = null;
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (SelectedConversation == null || (string.IsNullOrWhiteSpace(MessageInput) && string.IsNullOrWhiteSpace(SelectedAttachmentPath)))
            return;

        StatusMessage = string.Empty;

        if (SelectedConversation.Type == ConversationType.Direct)
        {
            var targetDeviceId = SelectedConversation.TargetDeviceId ?? SelectedConversation.Id;
            var peer = _discoveryService.GetPeerByDeviceId(targetDeviceId);

            if (peer == null)
            {
                peer = _discoveryService.GetDiscoveredPeers().FirstOrDefault(p => p.DeviceId == targetDeviceId);
            }

            if (peer == null)
            {
                var allPeers = await _peerRepo.GetAllPeersAsync();
                peer = allPeers.FirstOrDefault(p => p.DeviceId == targetDeviceId);
            }

            if (peer == null || string.IsNullOrWhiteSpace(peer.IpAddress))
            {
                StatusMessage = "❌ Target peer is offline or not found on network.";
                return;
            }

            var res = await _chatService.SendDirectMessageAsync(peer, MessageInput, SelectedAttachmentPath);
            if (res.IsSuccess && res.Value != null)
            {
                Messages.Add(res.Value);
                MessageInput = string.Empty;
                SelectedAttachmentPath = null;
                StatusMessage = string.Empty;
                await LoadConversationsAsync();
            }
            else
            {
                StatusMessage = $"❌ Direct message failed: {res.Error}";
            }
        }
        else if (SelectedConversation.Type == ConversationType.Group && !string.IsNullOrWhiteSpace(SelectedConversation.GroupId))
        {
            var groups = await _groupRepo.GetAllGroupsAsync();
            var group = groups.FirstOrDefault(g => g.Id == SelectedConversation.GroupId);
            if (group == null)
            {
                StatusMessage = "❌ Group conversation not found.";
                return;
            }

            var onlinePeers = _discoveryService.GetDiscoveredPeers();
            var res = await _chatService.SendGroupMessageAsync(group, onlinePeers, MessageInput, SelectedAttachmentPath);
            if (res.IsSuccess && res.Value != null)
            {
                Messages.Add(res.Value);
                MessageInput = string.Empty;
                SelectedAttachmentPath = null;
                StatusMessage = string.Empty;
                await LoadConversationsAsync();
            }
            else
            {
                StatusMessage = $"❌ Group message failed: {res.Error}";
            }
        }
    }

    private void OnMessageReceived(object? sender, Message msg)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (SelectedConversation != null &&
               (msg.ConversationId == SelectedConversation.Id ||
                msg.ConversationId == SelectedConversation.TargetDeviceId ||
                msg.SenderDeviceId == SelectedConversation.TargetDeviceId))
            {
                if (!Messages.Any(m => m.Id == msg.Id))
                {
                    Messages.Add(msg);
                }
            }
            _ = LoadConversationsAsync();
        });
    }

    private void OnTypingReceived(object? sender, string senderDeviceId)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (SelectedConversation != null && (SelectedConversation.TargetDeviceId == senderDeviceId || SelectedConversation.Id == senderDeviceId))
            {
                TypingText = "Peer is typing...";
                Task.Delay(3000).ContinueWith(_ => App.Current.Dispatcher.Invoke(() => TypingText = string.Empty));
            }
        });
    }
}
