using System.Globalization;
using Microsoft.JSInterop;

namespace Forma.Web.State;

public sealed class BrowserStorage(IJSRuntime browser)
{
    public async ValueTask<DateOnly> GetLocalDateAsync()
    {
        var isoDate = await browser.InvokeAsync<string>("forma.localDate");
        return DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public ValueTask<string?> ReadAsync() => browser.InvokeAsync<string?>("forma.read");
    public ValueTask WriteAsync(string json) => browser.InvokeVoidAsync("forma.write", json);
    public ValueTask DownloadAsync(string filename, string json) =>
        browser.InvokeVoidAsync("forma.download", filename, json);
}
