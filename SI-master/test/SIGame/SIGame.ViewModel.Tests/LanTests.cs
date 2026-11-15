using Microsoft.Extensions.DependencyInjection;
using SIGame.ViewModel.Contracts;
using SIGame.ViewModel.PackageSources;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Tests.Helpers;
using SIGame.ViewModel.Tests.Mocks;
using SIData;
using SIPackages;
using System.Reflection;

namespace SIGame.ViewModel.Tests;

[TestFixture]
public sealed class LanTests
{
    [Test]
    public void TenFreeSeatsAndBotsKeepValidAccountSelection()
    {
        PlatformManager.Instance = new TestPlatformManager();
        using var services = (ServiceProvider)TestHelper.CreateServiceProvider();
        var settings = services.GetRequiredService<UserSettings>();
        settings.GameSettings.Role = GameRole.Showman;
        settings.GameSettings.PlayersTypes = Enumerable.Repeat(AccountTypes.Human, 10).ToArray();
        var main = TestHelper.CreateMainViewModel(services);
        using var game = services.GetRequiredService<IGameSettingsViewModelFactory>().Create(main.Settings.ThemeSettings.SIUISettings, [], true);
        game.Human = main.Human.HumanPlayer;
        game.Role = GameRole.Showman;
        game.NetworkGameType = NetworkGameType.DirectConnection;
        game.PlayersCount = 10;
        typeof(GameSettingsViewModel).GetMethod("PrepareForGame", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, null);
        Assert.That(game.Players.Count, Is.EqualTo(10));
        foreach (var player in game.Players)
        {
            Assert.That(player.SelectedAccount, Is.Not.Null);
            Assert.That(player.SelectionList, Does.Contain(player.SelectedAccount));
            Assert.That(player.SelectedAccount!.Name, Is.EqualTo(SICore.Constants.FreePlace));
        }
        game.Players[0].AccountType = AccountTypes.Computer;
        Assert.That(game.Players[0].SelectedAccount, Is.TypeOf<ComputerAccount>());
        game.Players[0].AccountType = AccountTypes.Human;
        Assert.That(game.Players[0].SelectionList, Does.Contain(game.Players[0].SelectedAccount));
        Assert.That(game.Showman.SelectedAccount, Is.SameAs(main.Human.HumanPlayer));
    }

    [Test]
    public async Task SuppliedPackageLoadsThroughNativeReader()
    {
        var package = Environment.GetEnvironmentVariable("SI_LAN_TEST_PACKAGE");
        if (package == null) Assert.Ignore("Set SI_LAN_TEST_PACKAGE to run the supplied-package check.");
        var folder = Path.Combine(Path.GetTempPath(), "si-lan-package-test-" + Guid.NewGuid());
        using var document = await SIDocument.ExtractToFolderAndLoadAsync(package!, folder);
        Assert.That(document.Package.Rounds.Count, Is.EqualTo(4));
        Assert.That(document.Package.Rounds.Sum(r => r.Themes.Sum(t => t.Questions.Count)), Is.EqualTo(175));
        Assert.That(Directory.GetFiles(Path.Combine(folder, "Images")).Length, Is.GreaterThan(200));
    }
}
