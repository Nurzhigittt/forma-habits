using Microsoft.JSInterop;

namespace Forma.Integration.Tests;

// Substitute only the browser/interoperability transport. Persist the exact JSON
// supplied by BrowserStorage, just as the real forma.js localStorage adapter does.
internal sealed class BrowserRuntimeStub : IJSRuntime
{
    internal string LocalDate { get; init; } = "2026-11-14";
    internal string? SavedJson { get; set; }
    internal string? FailOperation { get; set; }
    internal int SuccessfulWrites { get; private set; }
    internal int ReadCalls { get; private set; }
    internal (string Filename, string Json)? Download { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (identifier == FailOperation)
            throw new JSException($"Simulated browser failure: {identifier}");

        return identifier switch
        {
            "forma.localDate" when (args ?? []).Length == 0 => ValueTask.FromResult((TValue)(object)LocalDate),
            "forma.read" when (args ?? []).Length == 0 => Read<TValue>(),
            "forma.write" when args is [string json] => Write<TValue>(json),
            "forma.download" when args is [string filename, string json] => SaveDownload<TValue>(filename, json),
            _ => throw new InvalidOperationException($"Unexpected JS call: {identifier}")
        };
    }

    private ValueTask<TValue> Read<TValue>()
    {
        ReadCalls++;
        return ValueTask.FromResult((TValue)(object?)SavedJson!);
    }

    private ValueTask<TValue> Write<TValue>(string json)
    {
        SavedJson = json;
        SuccessfulWrites++;
        return ValueTask.FromResult(default(TValue)!);
    }

    private ValueTask<TValue> SaveDownload<TValue>(string filename, string json)
    {
        Download = (filename, json);
        return ValueTask.FromResult(default(TValue)!);
    }
}
