using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RuneManager.Models;

namespace RuneManager.Lcu;

public sealed class LcuClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private HttpClient? _http;

    public bool IsConnected => _http is not null;

    public string? BaseUrl { get; private set; }

    /// <summary>
    /// League of Legends (LeagueClient.exe) のプロセスが実行中かどうかを判定します。
    /// </summary>
    public bool IsLeagueRunning()
    {
        return Process.GetProcessesByName("LeagueClient").Length > 0;
    }

    public bool TryConnect()
    {
        if (!LockfileLocator.TryFind(out var lockfile))
        {
            Disconnect();
            return false;
        }

        if (BaseUrl == lockfile.BaseUrl && _http is not null)
        {
            return true;
        }

        Disconnect();

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 6,
            SslOptions =
            {
                RemoteCertificateValidationCallback = static (_, _, _, _) => true
            }
        };

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(lockfile.BaseUrl),
            Timeout = TimeSpan.FromSeconds(4)
        };

        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{lockfile.Password}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _http = http;
        BaseUrl = lockfile.BaseUrl;
        return true;
    }

    public void Disconnect()
    {
        _http?.Dispose();
        _http = null;
        BaseUrl = null;
    }

    public Task<PerkPageDto[]?> GetPagesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<PerkPageDto[]>("/lol-perks/v1/pages", cancellationToken);

    public Task<PerkPageDto?> GetCurrentPageAsync(CancellationToken cancellationToken = default) =>
        GetAsync<PerkPageDto>("/lol-perks/v1/currentpage", cancellationToken);

    public Task<StyleDto[]?> GetStylesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<StyleDto[]>("/lol-perks/v1/styles", cancellationToken);

    public Task<PerkDto[]?> GetPerksAsync(CancellationToken cancellationToken = default) =>
        GetAsync<PerkDto[]>("/lol-perks/v1/perks", cancellationToken);

    public async Task OverwritePageAsync(long pageId, RunePreset preset, CancellationToken cancellationToken = default)
    {
        var http = RequireHttp();
        var body = new
        {
            name = preset.Name,
            primaryStyleId = preset.PrimaryStyleId,
            subStyleId = preset.SubStyleId,
            selectedPerkIds = preset.SelectedPerkIds,
            current = true
        };

        using var response = await http.PutAsJsonAsync(
            $"/lol-perks/v1/pages/{pageId}",
            body,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException($"LCU PUT {response.StatusCode}: {error}");
    }

    public async Task<byte[]?> GetBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        var http = RequireHttp();
        using var response = await http.GetAsync(path, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => Disconnect();

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        var http = RequireHttp();
        using var response = await http.GetAsync(path, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private HttpClient RequireHttp() =>
        _http ?? throw new InvalidOperationException("LCU に未接続です。クライアントを起動してください。");
}