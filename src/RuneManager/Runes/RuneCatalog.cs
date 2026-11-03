using RuneManager.Models;

namespace RuneManager.Runes;

public sealed class RuneCatalog
{
    public IReadOnlyList<StyleInfo> Styles { get; }
    public IReadOnlyDictionary<int, PerkInfo> Perks { get; }
    public IReadOnlyList<int[]> ShardSlots { get; }

    private RuneCatalog(
        IReadOnlyList<StyleInfo> styles,
        IReadOnlyDictionary<int, PerkInfo> perks,
        IReadOnlyList<int[]> shardSlots)
    {
        Styles = styles;
        Perks = perks;
        ShardSlots = shardSlots;
    }

    public static RuneCatalog FromLcu(StyleDto[] styles, PerkDto[] perks)
    {
        var perkMap = new Dictionary<int, PerkInfo>();
        foreach (var perk in perks)
        {
            perkMap[perk.Id] = ToInfo(perk.Id, perk.Name, perk.IconPath, FirstText(perk.LongDesc, perk.ShortDesc, perk.Tooltip));
        }

        var styleList = new List<StyleInfo>();
        List<int[]>? shards = null;

        foreach (var style in styles)
        {
            foreach (var slot in style.Slots)
            {
                foreach (var rune in slot.Runes ?? [])
                {
                    if (perkMap.ContainsKey(rune.Id) && string.IsNullOrWhiteSpace(rune.Name) && string.IsNullOrWhiteSpace(rune.IconPath))
                    {
                        continue;
                    }

                    if (!perkMap.TryGetValue(rune.Id, out var existing))
                    {
                        perkMap[rune.Id] = ToInfo(rune.Id, rune.Name, rune.IconPath, "");
                    }
                    else if (string.IsNullOrWhiteSpace(existing.IconPath) && !string.IsNullOrWhiteSpace(rune.IconPath))
                    {
                        perkMap[rune.Id] = existing with { IconPath = rune.IconPath, Name = string.IsNullOrWhiteSpace(existing.Name) ? rune.Name : existing.Name };
                    }
                }
            }

            var slotIds = style.Slots.Select(s => s.PerkIds()).Where(ids => ids.Length > 0).ToArray();
            if (slotIds.Length == 0)
            {
                continue;
            }

            if (IsShardOnly(slotIds))
            {
                shards ??= slotIds.ToList();
                continue;
            }

            var treeSlots = slotIds.Where(s => !IsShardSlot(s)).ToArray();
            shards ??= slotIds.Where(IsShardSlot).TakeLast(3).ToList();
            styleList.Add(new StyleInfo(
                style.Id,
                string.IsNullOrWhiteSpace(style.Name) ? style.Id.ToString() : style.Name,
                style.IconPath,
                RuneText.Plain(style.Tooltip),
                treeSlots,
                style.AllowedSubStyles));
        }

        return new RuneCatalog(styleList, perkMap, shards ?? []);
    }

    public StyleInfo? Style(int id) => Styles.FirstOrDefault(s => s.Id == id);

    public PerkInfo Perk(int id) =>
        Perks.TryGetValue(id, out var perk)
            ? perk
            : new PerkInfo(id, id.ToString(), "", "");

    public string NameOf(int perkId) => Perk(perkId).Name;

    public IReadOnlyList<int[]> TreeSlots(int styleId) =>
        Style(styleId)?.Slots ?? [];

    public IReadOnlyList<StyleInfo> SubStylesFor(int primaryId)
    {
        var primary = Style(primaryId);
        if (primary is { AllowedSubStyles.Count: > 0 })
        {
            var allowed = primary.AllowedSubStyles.ToHashSet();
            var matched = Styles.Where(s => allowed.Contains(s.Id) && s.Id != primaryId).ToArray();
            if (matched.Length > 0)
            {
                return matched;
            }
        }

        return Styles.Where(s => s.Id != primaryId).ToArray();
    }

    private static PerkInfo ToInfo(int id, string name, string icon, string desc) =>
        new(id, string.IsNullOrWhiteSpace(name) ? id.ToString() : name, icon, RuneText.Plain(desc));

    private static string FirstText(params string[] values) =>
        values.Select(RuneText.Plain).FirstOrDefault(v => v.Length > 0) ?? "";

    private static bool IsShardSlot(int[] ids) =>
        ids.Length > 0 && ids.All(id => id is >= 5000 and < 6000);

    private static bool IsShardOnly(IReadOnlyList<int[]> slots) =>
        slots.Count > 0 && slots.All(IsShardSlot);
}
