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

    public ChatViewModel(IChatService chatService, IDiscoveryService discoveryService)
    {
        _chatService = chatService;
        _discoveryService = discoveryService;

        _chatService.MessageReceived += OnMessageReceived;
        _chatService.TypingIndicatorReceived += OnTypingReceived;

        _ = LoadConversationsAsync();
    }

    public async Task LoadConversationsAsync()
    {
        var convs = await _chatService.GetConversationsAsync();
        Conversations.Clear();
        foreach (var c in convs) Conversations.Add(c);
    }

    public async Task OpenConversationWithPeerAsync(Peer peer)
    {
        await LoadConversationsAsync();
        var existing = Conversations.FirstOrDefault(c => c.Type == ConversationType.Direct && c.TargetDeviceId == peer.DeviceId);
        if (existing != null)
        {
            SelectedConversation = existing;
        }
        else
        {
            var newConv = new Conversation
            {
                Id = Guid.NewGuid().ToString("N"),
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
        if (value != null)
        {
            _ = LoadMessagesForConversationAsync(value.Id);
        }
    }

    private async Task LoadMessagesForConversationAsync(string conversationId)
    {
        var msgs = await _chatService.GetMessagesAsync(conversationId);
        Messages.Clear();
        foreach (var m in msgs) Messages.Add(m);
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

        if (SelectedConversation.Type == ConversationType.Direct && !string.IsNullOrWhiteSpace(SelectedConversation.TargetDeviceId))
        {
            var peer = _discoveryService.GetPeerByDeviceId(SelectedConversation.TargetDeviceId);
            if (peer != null)
            {
                var res = await _chatService.SendDirectMessageAsync(peer, MessageInput, SelectedAttachmentPath);
                if (res.IsSuccess && res.Value != null)
                {
                    Messages.Add(res.Value);
                    MessageInput = string.Empty;
                    SelectedAttachmentPath = null;
                }
            }
        }
    }

    private void OnMessageReceived(object? sender, Message msg)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (SelectedConversation != null && msg.ConversationId == SelectedConversation.Id)
            {
                Messages.Add(msg);
            }
            _ = LoadConversationsAsync();
        });
    }

    private void OnTypingReceived(object? sender, string senderDeviceId)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            TypingText = "Peer is typing...";
            Task.Delay(3000).ContinueWith(_ => App.Current.Dispatcher.Invoke(() => TypingText = string.Empty));
        });
    }
}
