using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace ArmyRanks;

/// <summary>
/// The mod's identity card. SPT 4.x finds this record automatically and reads it
/// instead of a package.json. SptVersion is a semver range of servers it will load on.
/// </summary>
public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.cotti.armyranks";
    public string Name { get; init; } = "ArmyRanks";
    public string Author { get; init; } = "Cotti";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.0.1");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

/// <summary>
/// The part that runs. In SPT 4.x there is no IPostDBLoadMod any more: a mod is a class
/// marked [Injectable] that implements IOnLoad. The server's dependency-injection container
/// builds it, hands it whatever services the constructor asks for, then calls OnLoadAsync.
/// TypePriority sets when that happens; PostLoad + 1 is after the database has loaded,
/// which is the 4.x equivalent of postDBLoad.
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class ArmyRanksMod(ISptLogger<ArmyRanksMod> logger, LocaleTable localeTable) : IOnLoad
{
    /// <summary>
    /// English UI labels that sit next to the player's level number, and what to show instead.
    /// Keys come from SPT_Data/database/locales/global/en.json. The level number itself is not
    /// in the locale data, so these change the words around it ("Rank: 12"), not the number.
    /// Remove any line that ends up relabelling something you don't want changed.
    /// </summary>
    private static readonly Dictionary<string, string> LabelOverrides = new()
    {
        ["LEVEL"] = "RANK:",                                                      // was "LEVEL:"
        ["Lvl"] = "Rank",                                                         // was "Level"
        ["Level "] = "Rank: ",                                                    // was "Level: "
        ["Player level"] = "Player rank",
        ["ClothingPanel/PlayerLevel"] = "Player\nRank:",
        ["Experience Needed for Next Level"] = "Experience needed for next promotion",
        ["UI/Quests/Conditions/ProfileLevel{0}"] = "Character rank: {0}",
        ["ArmoryCondition/ProfileLevel"] = "Reach rank:",
        ["Conditional/ConditionLevel/Type"] = "Character rank",
    };

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // localeTable.Global holds one entry per language ("en", "fr", ...). Each entry is a
        // LazyLoad: the huge dictionary is only read from disk when something asks for it,
        // and can be unloaded again to save memory. So editing it directly once would not stick.
        if (!localeTable.Global.TryGetValue("en", out var englishLocale))
        {
            logger.Warning("[ArmyRanks] English locale not found; nothing changed.");
            return Task.CompletedTask;
        }

        // Instead we register a transformer: a function SPT runs on the dictionary every time
        // it is loaded, before it is sent to the game client. Whatever we return is what the
        // client receives when it downloads the English strings.
        englishLocale.AddTransformer(strings =>
        {
            if (strings is null)
            {
                return strings;
            }

            foreach (var (key, newText) in LabelOverrides)
            {
                // Only overwrite keys that exist, so a game update that renames a key
                // never leaves junk entries behind.
                if (strings.ContainsKey(key))
                {
                    strings[key] = newText;
                }
            }

            return strings;
        });

        logger.Success($"[ArmyRanks] Relabelled {LabelOverrides.Count} level labels in the English locale.");
        logger.Info($"[ArmyRanks] Level 1 = {ArmyRankTable.ForLevel(1)}, level 79 = {ArmyRankTable.ForLevel(79)}.");

        return Task.CompletedTask;
    }
}
