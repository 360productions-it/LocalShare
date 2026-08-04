using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;
using LocalShare.Core.Interfaces;
using LocalShare.Core.Models;
using LocalShare.Data;
using LocalShare.Data.Repositories;
using LocalShare.Networking.Discovery;
using LocalShare.Networking.Http;
using LocalShare.Networking.PublicSpace;
using LocalShare.Networking.Transfer;
using LocalShare.Networking.Chat;
using LocalShare.Networking.Services;
using LocalShare.App.ViewModels;
using LocalShare.App.Views;
using LocalShare.App.Helpers;

namespace LocalShare.App;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Auto-register Windows Defender Firewall rule for P2P network traffic
            FirewallHelper.RegisterFirewallRules();

            // Apply WPF-UI Dark Theme
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);

            // 1. Initialize SQLite Database
            var dbInit = new DatabaseInitializer();
            await dbInit.InitializeAsync();

            // 2. Load Local User Profile
            var sqliteRepo = new SqliteRepositories(dbInit);
            var profile = await sqliteRepo.GetProfileAsync();

            // 3. Configure DI Container
            var services = new ServiceCollection();
            services.AddSingleton(dbInit);
            services.AddSingleton(sqliteRepo);
            services.AddSingleton<IProfileRepository>(sqliteRepo);
            services.AddSingleton<IPeerRepository>(sqliteRepo);
            services.AddSingleton<IMessageRepository>(sqliteRepo);
            services.AddSingleton<IGroupRepository>(sqliteRepo);
            services.AddSingleton<ITransferRepository>(sqliteRepo);

            services.AddSingleton(profile);

            // Networking Services
            services.AddSingleton<PeerRegistry>();
            services.AddSingleton<IDiscoveryService, UdpBeaconService>();
            services.AddSingleton<ITransferService, TransferService>();
            services.AddSingleton<IPublicSpaceService, PublicSpaceService>();
            services.AddSingleton<IChatService, ChatService>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<KestrelServerHost>();

            // ViewModels
            services.AddSingleton<ShellViewModel>();
            services.AddSingleton<PeersViewModel>();
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<PublicSpaceViewModel>();
            services.AddSingleton<GroupsViewModel>();
            services.AddSingleton<TransfersViewModel>();
            services.AddSingleton<ProfileSettingsViewModel>();

            // Views
            services.AddSingleton<ShellView>();

            ServiceProvider = services.BuildServiceProvider();

            // 4. Start Kestrel Server Host (HTTP & SignalR)
            var kestrelHost = ServiceProvider.GetRequiredService<KestrelServerHost>();
            await kestrelHost.StartAsync();

            // 5. Start UDP Discovery Beacon
            var discoveryService = ServiceProvider.GetRequiredService<IDiscoveryService>();
            await discoveryService.StartAsync();

            // 6. Show Shell Window
            var shellView = ServiceProvider.GetRequiredService<ShellView>();
            shellView.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup Error: {ex.Message}\n{ex.StackTrace}", "360 LocalShare Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (ServiceProvider != null)
        {
            var discovery = ServiceProvider.GetService<IDiscoveryService>();
            if (discovery != null) await discovery.StopAsync();

            var kestrel = ServiceProvider.GetService<KestrelServerHost>();
            if (kestrel != null) await kestrel.StopAsync();
        }

        base.OnExit(e);
    }
}
