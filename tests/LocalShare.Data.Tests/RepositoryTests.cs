using Microsoft.Data.Sqlite;
using LocalShare.Core.Models;
using LocalShare.Data;
using LocalShare.Data.Repositories;
using Xunit;

namespace LocalShare.Data.Tests;

public class RepositoryTests
{
    [Fact]
    public async Task DatabaseInitializer_ShouldCreateTablesAndProfile()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"test_localshare_{Guid.NewGuid():N}.db");
        try
        {
            var dbInit = new DatabaseInitializer(tempDb);
            await dbInit.InitializeAsync();

            var repos = new SqliteRepositories(dbInit);
            var profile = await repos.GetProfileAsync();

            Assert.NotNull(profile);
            Assert.False(string.IsNullOrWhiteSpace(profile.DeviceId));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }

    [Fact]
    public async Task PeerRepository_UpsertAndRetrieve_ShouldWork()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"test_localshare_{Guid.NewGuid():N}.db");
        try
        {
            var dbInit = new DatabaseInitializer(tempDb);
            await dbInit.InitializeAsync();

            var repos = new SqliteRepositories(dbInit);
            var peer = new Peer
            {
                DeviceId = "test-device-123",
                DisplayName = "Kavindu",
                IpAddress = "192.168.1.50",
                HttpPort = 53211,
                HasPublicSpace = true
            };

            await repos.UpsertPeerAsync(peer);
            var peers = await repos.GetAllPeersAsync();

            Assert.Single(peers);
            Assert.Equal("Kavindu", peers[0].DisplayName);
            Assert.True(peers[0].HasPublicSpace);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
