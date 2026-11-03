using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RuneManager.Lcu;

public readonly record struct LcuLockfile(int Port, string Password, string Protocol)
{
    public string BaseUrl => $"{Protocol}://127.0.0.1:{Port}";
}

public static class LockfileLocator
{
    private const uint ProcessQueryLimitedInformation = 0x1000;

    public static bool TryFind(out LcuLockfile lockfile)
    {
        foreach (var process in Process.GetProcessesByName("LeagueClientUx"))
        {
            try
            {
                var imagePath = TryGetImagePath(process.Id);
                if (string.IsNullOrEmpty(imagePath))
                {
                    continue;
                }

                var path = Path.Combine(Path.GetDirectoryName(imagePath)!, "lockfile");
                if (TryRead(path, out lockfile))
                {
                    return true;
                }
            }
            catch
            {
                // Keep scanning other client processes.
            }
            finally
            {
                process.Dispose();
            }
        }

        foreach (var candidate in FallbackLockfilePaths())
        {
            if (TryRead(candidate, out lockfile))
            {
                return true;
            }
        }

        lockfile = default;
        return false;
    }

    private static IEnumerable<string> FallbackLockfilePaths()
    {
        yield return @"C:\Riot Games\League of Legends\lockfile";
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Riot Games",
            "League of Legends",
            "lockfile");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Riot Games",
            "League of Legends",
            "lockfile");
    }

    private static bool TryRead(string path, out LcuLockfile lockfile)
    {
        lockfile = default;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            var parts = line.Split(':');
            if (parts.Length < 5)
            {
                return false;
            }

            if (!int.TryParse(parts[2], out var port))
            {
                return false;
            }

            lockfile = new LcuLockfile(port, parts[3], parts[4]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? TryGetImagePath(int pid)
    {
        var handle = OpenProcess(ProcessQueryLimitedInformation, false, pid);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var buffer = new StringBuilder(1024);
            var size = buffer.Capacity;
            return QueryFullProcessImageName(handle, 0, buffer, ref size)
                ? buffer.ToString()
                : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(
        IntPtr process,
        int flags,
        StringBuilder exeName,
        ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
