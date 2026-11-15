using System.Collections.Immutable;
using System.Text.Json;
using Forma.Core;
using Forma.Web.State;
using Xunit;

namespace Forma.Integration.Tests;

public sealed class HabitStoreTests
{
    private static readonly DateOnly Saturday = new(2026, 11, 14);
    private static readonly DateOnly Friday = Saturday.AddDays(-1);

    [Fact]
    public async Task CreateAndLogThenReopenDeserializesBothKindsAndExactDatedValues()
    {
        var browser = new BrowserRuntimeStub();
        var store = await Open(browser);
        var check = NewCheck();
        var quantity = NewQuantity();

        Assert.True(await store.SaveHabitAsync(check), store.Error);
        Assert.True(await store.SaveHabitAsync(quantity), store.Error);
        Assert.True(await store.ToggleProgressAsync(check.Id, Saturday), store.Error);
        Assert.True(await store.SetProgressAsync(quantity.Id, Friday, 4), store.Error);
        Assert.True(await store.AdjustProgressAsync(quantity.Id, Friday, 3), store.Error);

        Assert.Equal(5, browser.SuccessfulWrites);
        using var payload = JsonDocument.Parse(Assert.IsType<string>(browser.SavedJson));
        Assert.Equal(1, payload.RootElement.GetProperty("version").GetInt32());

        var reopened = await Open(browser);
        Assert.Equal(new[] { check, quantity }, reopened.State.Habits);
        Assert.Equal(1, Tracker.GetProgress(reopened.State, check.Id, Saturday));
        Assert.Equal(7, Tracker.GetProgress(reopened.State, quantity.Id, Friday));
        Assert.Equal(0, Tracker.GetProgress(reopened.State, quantity.Id, Saturday));
        Assert.True(Tracker.IsComplete(reopened.State, quantity, Friday));
        await reopened.InitializeAsync();
        Assert.Equal(2, browser.ReadCalls);
    }

    [Fact]
    public async Task InvalidMutationDoesNotWriteAndDeletionPersistsAcrossReopen()
    {
        var browser = new BrowserRuntimeStub();
        var store = await Open(browser);
        var habit = NewCheck();

        Assert.True(await store.SaveHabitAsync(habit), store.Error);
        Assert.True(await store.ToggleProgressAsync(habit.Id, Saturday), store.Error);
        var before = browser.SavedJson;

        Assert.False(await store.SetProgressAsync(habit.Id, Saturday.AddDays(1), 1));
        Assert.Contains("scheduled", store.Error);
        Assert.Equal(before, browser.SavedJson);
        Assert.Equal(2, browser.SuccessfulWrites);

        Assert.True(await store.DeleteHabitAsync(habit.Id), store.Error);
        var reopened = await Open(browser);
        Assert.Empty(reopened.State.Habits);
        Assert.Empty(reopened.State.Logs);
    }

    [Fact]
    public async Task WriteFailureLeavesMemoryAndPersistedDataUnchangedAndRetryWorks()
    {
        var browser = new BrowserRuntimeStub();
        var store = await Open(browser);
        var habit = NewCheck();
        Assert.True(await store.SaveHabitAsync(habit), store.Error);
        var priorState = store.State;
        var priorJson = browser.SavedJson;
        browser.FailOperation = "forma.write";

        Assert.False(await store.ToggleProgressAsync(habit.Id, Saturday));
        Assert.Contains("weren’t saved", store.Error);
        Assert.Same(priorState, store.State);
        Assert.Equal(priorJson, browser.SavedJson);
        Assert.Equal(1, browser.SuccessfulWrites);

        browser.FailOperation = null;
        Assert.True(await store.ToggleProgressAsync(habit.Id, Saturday), store.Error);
        Assert.Null(store.Error);
        var reopened = await Open(browser);
        Assert.True(Tracker.IsComplete(reopened.State, habit, Saturday));
    }

    [Theory]
    [InlineData("{not-json")]
    [InlineData("{\"version\":9,\"state\":{\"habits\":[],\"logs\":{}}}")]
    [InlineData("{\"version\":1,\"state\":{\"habits\":[],\"logs\":{\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\":{\"2026-11-14\":1}}}}")]
    public async Task InvalidPersistedDataIsPreservedUntilValidRecovery(string corruptBackup)
    {
        var browser = new BrowserRuntimeStub { SavedJson = corruptBackup };
        var store = new HabitStore(new BrowserStorage(browser));
        await store.InitializeAsync();

        Assert.True(store.Ready);
        Assert.True(store.HasUnreadableBackup);
        Assert.NotNull(store.Error);
        Assert.Empty(store.State.Habits);
        Assert.False(await store.SaveHabitAsync(NewCheck()));
        Assert.Equal(corruptBackup, browser.SavedJson);
        Assert.Equal(0, browser.SuccessfulWrites);

        Assert.True(await store.ExportAsync(original: true), store.Error);
        Assert.Equal(("forma-recovery.json", corruptBackup), browser.Download);

        var goodBrowser = new BrowserRuntimeStub();
        var goodStore = await Open(goodBrowser);
        var goodHabit = NewCheck();
        Assert.True(await goodStore.SaveHabitAsync(goodHabit), goodStore.Error);
        var parsed = store.ParseBackup(Assert.IsType<string>(goodBrowser.SavedJson));
        Assert.True(parsed.IsSuccess, parsed.Error);

        browser.FailOperation = "forma.write";
        Assert.False(await store.ReplaceFromBackupAsync(parsed.Value!));
        Assert.True(store.HasUnreadableBackup);
        Assert.Equal(corruptBackup, browser.SavedJson);

        browser.FailOperation = null;
        Assert.True(await store.ReplaceFromBackupAsync(parsed.Value!), store.Error);
        Assert.False(store.HasUnreadableBackup);
        Assert.Equal(goodHabit, Assert.Single(store.State.Habits));
        Assert.Equal(goodHabit, Assert.Single((await Open(browser)).State.Habits));
    }

    [Fact]
    public async Task BlockedReadReportsErrorWithoutOverwritingExistingBrowserRecord()
    {
        var browser = new BrowserRuntimeStub { SavedJson = "opaque-previous-record", FailOperation = "forma.read" };
        var store = new HabitStore(new BrowserStorage(browser));

        await store.InitializeAsync();
        Assert.True(store.Ready);
        Assert.Contains("blocking local storage", store.Error);
        Assert.Empty(store.State.Habits);
        Assert.Equal("opaque-previous-record", browser.SavedJson);
        Assert.Equal(0, browser.SuccessfulWrites);
    }

    [Fact]
    public async Task StandardExportCanBeImportedIntoAnIndependentBrowserStore()
    {
        var originalBrowser = new BrowserRuntimeStub();
        var original = await Open(originalBrowser);
        var habit = NewQuantity();

        Assert.True(await original.SaveHabitAsync(habit), original.Error);
        Assert.True(await original.SetProgressAsync(habit.Id, Saturday, 3), original.Error);
        Assert.True(await original.ExportAsync(), original.Error);
        var download = originalBrowser.Download.GetValueOrDefault();
        Assert.Equal("forma-backup-2026-11-14.json", download.Filename);
        Assert.Equal(originalBrowser.SavedJson, download.Json);

        var destinationBrowser = new BrowserRuntimeStub();
        var destination = await Open(destinationBrowser);
        var parsed = destination.ParseBackup(download.Json);
        Assert.True(parsed.IsSuccess, parsed.Error);
        Assert.True(await destination.ReplaceFromBackupAsync(parsed.Value!), destination.Error);
        Assert.Equal(3, Tracker.GetProgress((await Open(destinationBrowser)).State, habit.Id, Saturday));
    }

    [Fact]
    public async Task DownloadFailureIsReportedAndDoesNotModifyPersistence()
    {
        var browser = new BrowserRuntimeStub { FailOperation = "forma.download" };
        var store = await Open(browser);

        Assert.False(await store.ExportAsync());
        Assert.Contains("blocked the download", store.Error);
        Assert.Null(browser.Download);
        Assert.Equal(0, browser.SuccessfulWrites);
        browser.FailOperation = null;
        Assert.True(await store.ExportAsync(), store.Error);
        Assert.NotNull(browser.Download);
    }

    [Fact]
    public async Task BackupSizeUsesUtf8BytesRatherThanCharacterCount()
    {
        var browser = new BrowserRuntimeStub();
        var store = await Open(browser);
        var oversized = new string('é', HabitStore.MaxBackupBytes / 2 + 1);

        var result = store.ParseBackup(oversized);
        Assert.False(result.IsSuccess);
        Assert.Contains("larger than 2 MB", result.Error);
        Assert.Equal(0, browser.SuccessfulWrites);
    }

    [Fact]
    public async Task ValidSnapshotOverSerializationLimitCannotReplaceCurrentData()
    {
        var browser = new BrowserRuntimeStub();
        var store = await Open(browser);
        Assert.True(await store.SaveHabitAsync(NewCheck()), store.Error);
        var previous = store.State;
        var previousJson = browser.SavedJson;

        var history = ImmutableDictionary.CreateBuilder<DateOnly, int>();
        var start = Saturday.AddDays(1 - Tracker.MaxEntries);
        var habit = NewQuantity() with { StartDate = start };
        for (var day = 0; day < Tracker.MaxEntries; day++)
            history.Add(start.AddDays(day), Tracker.MaxQuantity);
        var oversized = new TrackerState([habit],
            ImmutableDictionary<Guid, ImmutableDictionary<DateOnly, int>>.Empty.Add(habit.Id, history.ToImmutable()));

        Assert.True(Tracker.Validate(oversized, Saturday).IsSuccess);
        Assert.False(await store.ReplaceFromBackupAsync(oversized));
        Assert.Contains("reached its 2 MB", store.Error);
        Assert.Same(previous, store.State);
        Assert.Equal(previousJson, browser.SavedJson);
        Assert.Equal(1, browser.SuccessfulWrites);
    }

    private static Habit NewCheck() => new(Guid.NewGuid(), "Walk outside", HabitKind.Check, 1, "", Weekdays.All, Friday);
    private static Habit NewQuantity() => new(Guid.NewGuid(), "Read", HabitKind.Quantity, 6, "pages", Weekdays.All, Friday);

    private static async Task<HabitStore> Open(BrowserRuntimeStub browser)
    {
        var store = new HabitStore(new BrowserStorage(browser));
        await store.InitializeAsync();
        Assert.True(store.Ready);
        Assert.Equal(Saturday, store.Today);
        Assert.Null(store.Error);
        return store;
    }
}
