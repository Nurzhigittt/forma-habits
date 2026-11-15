using System.Text.Json;
using System.Text.Json.Serialization;
using Forma.Core;
using Microsoft.JSInterop;

namespace Forma.Web.State;

// The store owns I/O and observable state; all business rules live in Forma.Core.
public sealed class HabitStore(BrowserStorage storage)
{
    private readonly SemaphoreSlim _writer = new(1, 1);
    private string? _unreadableBackup;
    public TrackerState State { get; private set; } = TrackerState.Empty;
    public DateOnly Today { get; private set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? Error { get; private set; }
    public bool Ready { get; private set; }
    public bool HasUnreadableBackup => _unreadableBackup is not null;
    public const int MaxBackupBytes = 2_000_000;

    public async Task InitializeAsync()
    {
        if (Ready) return;
        try
        {
            Today = await storage.GetLocalDateAsync();
            var raw = await storage.ReadAsync();
            if (raw is not null)
            {
                var validation = ParseBackup(raw);
                if (validation.IsSuccess) State = validation.Value!;
                else
                {
                    _unreadableBackup = raw;
                    Error = "The saved data couldn't be read. Download a recovery copy or restore a valid backup before making changes.";
                }
            }
        }
        catch (JSException)
        {
            Error = "This browser is blocking local storage. Allow site storage to keep your progress.";
        }
        finally { Ready = true; }
    }

    public Task<bool> SaveHabitAsync(Habit habit) => UpdateAsync(current => Tracker.SaveHabit(current, habit, Today));
    public Task<bool> DeleteHabitAsync(Guid id) => UpdateAsync(current => Tracker.DeleteHabit(current, id));
    public Task<bool> SetProgressAsync(Guid id, DateOnly day, int value) =>
        UpdateAsync(current => Tracker.SetProgress(current, id, day, value, Today));
    public Task<bool> AdjustProgressAsync(Guid id, DateOnly day, int amount) =>
        UpdateAsync(current => Tracker.SetProgress(current, id, day, Math.Max(0, Tracker.GetProgress(current, id, day) + amount), Today));
    public Task<bool> ToggleProgressAsync(Guid id, DateOnly day) =>
        UpdateAsync(current => Tracker.SetProgress(current, id, day, Tracker.GetProgress(current, id, day) == 0 ? 1 : 0, Today));

    public Result<TrackerState> ParseBackup(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxBackupBytes)
            return new Result<TrackerState>(null, "The backup is larger than 2 MB.");
        try
        {
            var backup = JsonSerializer.Deserialize(json, BackupJson.Default.FormaBackup);
            if (backup is null || backup.Version != 1)
                return new Result<TrackerState>(null, "Choose a Forma version 1 JSON backup.");
            return Tracker.Validate(backup.State, Today);
        }
        catch (JsonException)
        {
            return new Result<TrackerState>(null, "This file does not contain a valid Forma JSON backup.");
        }
    }

    public Task<bool> ReplaceFromBackupAsync(TrackerState replacement) =>
        UpdateAsync(_ => Tracker.Validate(replacement, Today), allowRecovery: true);

    public async Task<bool> ExportAsync(bool original = false)
    {
        Error = null;
        try
        {
            var raw = original ? _unreadableBackup : Serialize(State);
            if (raw is null) return false;
            var name = original ? "forma-recovery.json" : $"forma-backup-{Today:yyyy-MM-dd}.json";
            await storage.DownloadAsync(name, raw);
            return true;
        }
        catch (JSException)
        {
            Error = "Your browser blocked the download. Try allowing downloads from this site.";
            return false;
        }
    }

    private async Task<bool> UpdateAsync(Func<TrackerState, Result<TrackerState>> change, bool allowRecovery = false)
    {
        await _writer.WaitAsync();
        try
        {
            Error = null;
            if (_unreadableBackup is not null && !allowRecovery)
            {
                Error = "Restore a valid backup in Settings before changing this data.";
                return false;
            }
            var result = change(State);
            if (!result.IsSuccess)
            {
                Error = result.Error;
                return false;
            }
            var next = result.Value!;
            var raw = Serialize(next);
            if (System.Text.Encoding.UTF8.GetByteCount(raw) > MaxBackupBytes)
            {
                Error = "This tracker reached its 2 MB storage limit. Remove an unused habit to make room.";
                return false;
            }
            await storage.WriteAsync(raw);
            State = next;
            _unreadableBackup = null;
            return true;
        }
        catch (JSException)
        {
            Error = "Changes weren’t saved. Check that browser storage is available and try again.";
            return false;
        }
        finally { _writer.Release(); }
    }

    private static string Serialize(TrackerState state) =>
        JsonSerializer.Serialize(new FormaBackup(1, state), BackupJson.Default.FormaBackup);
}

public sealed record FormaBackup(int Version, TrackerState State);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(FormaBackup))]
internal partial class BackupJson : JsonSerializerContext { }
