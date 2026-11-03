using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RuneManager.Models;

// 1. レーン分け用のEnum（カテゴリ）
public enum LaneCategory
{
    All,     // すべて（レーン分けなし）
    Top,     // TOP
    Jungle,  // JG
    Mid,     // MID
    ADC,     // ADC
    Support  // SUP
}

// 2. RunePreset
public sealed class RunePreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New page";
    public LaneCategory Category { get; set; } = LaneCategory.All;
    public string Lane { get; set; } = "ALL"; // "ALL", "TOP", "JG", "MID", "ADC", "SUP"
    public int DisplayOrder { get; set; }
    public int PrimaryStyleId { get; set; }
    public int SubStyleId { get; set; }
    public int[] SelectedPerkIds { get; set; } = [];
}

public sealed class PerkPageDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("current")]
    public bool Current { get; set; }

    [JsonPropertyName("isEditable")]
    public bool IsEditable { get; set; }

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("primaryStyleId")]
    public int PrimaryStyleId { get; set; }

    [JsonPropertyName("subStyleId")]
    public int SubStyleId { get; set; }

    [JsonPropertyName("selectedPerkIds")]
    public int[] SelectedPerkIds { get; set; } = [];

    public RunePreset ToPreset() => new()
    {
        Name = Name,
        PrimaryStyleId = PrimaryStyleId,
        SubStyleId = SubStyleId,
        SelectedPerkIds = SelectedPerkIds
    };
}

public sealed class PerkDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("iconPath")]
    public string IconPath { get; set; } = "";

    [JsonPropertyName("shortDesc")]
    public string ShortDesc { get; set; } = "";

    [JsonPropertyName("longDesc")]
    public string LongDesc { get; set; } = "";

    [JsonPropertyName("tooltip")]
    public string Tooltip { get; set; } = "";
}

public sealed class StyleDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("iconPath")]
    public string IconPath { get; set; } = "";

    [JsonPropertyName("tooltip")]
    public string Tooltip { get; set; } = "";

    [JsonPropertyName("allowedSubStyles")]
    public int[] AllowedSubStyles { get; set; } = [];

    [JsonPropertyName("slots")]
    public StyleSlotDto[] Slots { get; set; } = [];
}

public sealed class StyleSlotDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("slotLabel")]
    public string SlotLabel { get; set; } = "";

    [JsonPropertyName("perks")]
    public int[]? Perks { get; set; }

    [JsonPropertyName("runes")]
    public SlotRuneDto[]? Runes { get; set; }

    public int[] PerkIds()
    {
        if (Perks is { Length: > 0 })
        {
            return Perks;
        }

        if (Runes is { Length: > 0 })
        {
            return Runes.Select(r => r.Id).ToArray();
        }

        return [];
    }
}

public sealed class SlotRuneDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("iconPath")]
    public string IconPath { get; set; } = "";
}

public sealed record PerkInfo(int Id, string Name, string IconPath, string Description);

public sealed record StyleInfo(
    int Id,
    string Name,
    string IconPath,
    string Description,
    IReadOnlyList<int[]> Slots,
    IReadOnlyList<int> AllowedSubStyles);

public static class RuneText
{
    private static readonly Regex Tags = new("<.*?>", RegexOptions.Singleline | RegexOptions.Compiled);

    public static string Plain(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return "";
        }

        var stripped = Tags.Replace(html, "");
        return WebUtility.HtmlDecode(stripped).Trim();
    }
}