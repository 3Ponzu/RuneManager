using System.Text.Json;
using RuneManager.Models;

namespace RuneManager.Runes;

public sealed class PresetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string DirectoryPath { get; }

    public PresetStore()
    {
        DirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RuneManager",
            "presets");
        Directory.CreateDirectory(DirectoryPath);
    }

    /// <summary>
    /// 保存されているすべてのプリセットを取得します。
    /// </summary>
    public List<RunePreset> GetAll()
    {
        var list = new List<RunePreset>();
        if (!Directory.Exists(DirectoryPath)) return list;

        foreach (var file in Directory.GetFiles(DirectoryPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonSerializer.Deserialize<RunePreset>(json, JsonOptions);
                if (preset is not null)
                {
                    if (string.IsNullOrEmpty(preset.Id)) preset.Id = Guid.NewGuid().ToString();
                    list.Add(preset);
                }
            }
            catch
            {
                // 破損ファイル等はスキップ
            }
        }
        return list;
    }

    /// <summary>
    /// 指定カテゴリ（ロール）のプリセット一覧を取得します。
    /// </summary>
    public List<RunePreset> GetByCategory(LaneCategory category)
    {
        return GetAll()
            .Where(p => p.Category == category)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// プリセットを保存します（新規・上書き共通）。
    /// </summary>
    public void Save(RunePreset preset)
    {
        if (string.IsNullOrWhiteSpace(preset.Name))
        {
            throw new InvalidOperationException("プリセット名が空です。");
        }

        if (string.IsNullOrEmpty(preset.Id)) preset.Id = Guid.NewGuid().ToString();

        // 過去の {名前}.json 形式のファイルが存在すれば削除
        CleanLegacyFile(preset.Name);

        var filePath = Path.Combine(DirectoryPath, $"{preset.Id}.json");
        File.WriteAllText(filePath, JsonSerializer.Serialize(preset, JsonOptions));
    }

    /// <summary>
    /// 指定されたIDまたは名前のプリセットを削除します。
    /// </summary>
    public void Delete(string idOrName)
    {
        if (string.IsNullOrWhiteSpace(idOrName)) return;

        var target = GetAll().FirstOrDefault(p => p.Id == idOrName || string.Equals(p.Name, idOrName, StringComparison.OrdinalIgnoreCase));
        if (target is not null)
        {
            var path = Path.Combine(DirectoryPath, $"{target.Id}.json");
            if (File.Exists(path)) File.Delete(path);
            CleanLegacyFile(target.Name);
        }
        else
        {
            CleanLegacyFile(idOrName);
        }
    }

    /// <summary>
    /// 名前指定でプリセットを読み込みます。
    /// </summary>
    public RunePreset Load(string name)
    {
        var target = GetAll().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (target is not null) return target;

        var legacyPath = PathForLegacy(name);
        if (File.Exists(legacyPath))
        {
            var json = File.ReadAllText(legacyPath);
            return JsonSerializer.Deserialize<RunePreset>(json, JsonOptions)
                ?? throw new InvalidOperationException("プリセットを読めませんでした。");
        }

        throw new InvalidOperationException($"プリセット '{name}' が見つかりませんでした。");
    }

    /// <summary>
    /// 登録済みの全プリセット名一覧を取得します。
    /// </summary>
    public IReadOnlyList<string> ListNames()
    {
        return GetAll()
            .Select(p => p.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();
    }

    private void CleanLegacyFile(string name)
    {
        try
        {
            var legacyPath = PathForLegacy(name);
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }
        }
        catch
        {
        }
    }

    private string PathForLegacy(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return Path.Combine(DirectoryPath, name + ".json");
    }
}