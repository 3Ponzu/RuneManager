using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using RuneManager.Lcu;

namespace RuneManager.Ui;

internal sealed class IconCache : IDisposable
{
    private readonly ConcurrentDictionary<string, Bitmap> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _directory;
    private readonly SemaphoreSlim _gate = new(4, 4);

    public IconCache()
    {
        _directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RuneManager",
            "icons");
        Directory.CreateDirectory(_directory);
    }

    public async Task<Bitmap?> GetAsync(LcuClient lcu, string path, int size, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var key = path + "@" + size;
        if (_images.TryGetValue(key, out var cached))
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            if (_images.TryGetValue(key, out cached))
            {
                return cached;
            }

            var file = FilePath(path);
            byte[]? bytes = null;
            if (File.Exists(file))
            {
                bytes = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(true);
            }
            else if (lcu.IsConnected)
            {
                bytes = await lcu.GetBytesAsync(path, cancellationToken).ConfigureAwait(true);
                if (bytes is { Length: > 0 })
                {
                    await File.WriteAllBytesAsync(file, bytes, cancellationToken).ConfigureAwait(true);
                }
            }

            if (bytes is not { Length: > 0 })
            {
                return null;
            }

            using var sourceStream = new MemoryStream(bytes, writable: false);
            using var source = Image.FromStream(sourceStream);
            var scaled = new Bitmap(size, size);
            using (var graphics = Graphics.FromImage(scaled))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(source, 0, 0, size, size);
            }

            _images[key] = scaled;
            return scaled;
        }
        catch
        {
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        foreach (var image in _images.Values)
        {
            image.Dispose();
        }

        _images.Clear();
        _gate.Dispose();
    }

    private string FilePath(string path)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path.ToLowerInvariant())))[..16];
        return Path.Combine(_directory, hash + ".img");
    }
}
