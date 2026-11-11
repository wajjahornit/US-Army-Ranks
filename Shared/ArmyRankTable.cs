#nullable enable
using System;

namespace ArmyRanks;

/// <summary>Which army's ranks to show. U.S. Army is the default.</summary>
public enum RankSystem
{
    USArmy,
    BritishArmy,
}

/// <summary>
/// One rank, e.g. Sergeant / SGT / E-5. PayGrade holds the U.S. pay grade or, for the
/// British Army, the NATO code (OR-6, OF-3). Icon names the emblem PNG in the client's
/// Icons folder; when left out the abbreviation is used.
/// </summary>
public record ArmyRank(string Title, string Abbreviation, string PayGrade, string? Icon = null)
{
    /// <summary>File name (without .png) of this rank's emblem.</summary>
    public string IconKey => Icon ?? Abbreviation;

    /// <summary>"Sergeant (SGT / E-5)"</summary>
    public override string ToString() => $"{Title} ({Abbreviation} / {PayGrade})";
}

/// <summary>
/// Player level (1-79) to a U.S. or British Army rank. Written as level ranges so it reads like the
/// spec; ForLevel() does the lookup. Both the server mod and the client plugin compile
/// this same file, so the two can never disagree.
/// </summary>
public static class ArmyRankTable
{
    public const int MaxLevel = 79;

    private static readonly (int From, int To, ArmyRank Rank)[] UsRanges =
    [
        // Junior Enlisted
        (1, 1, new("Private", "PV1", "E-1")),
        (2, 2, new("Private", "PV2", "E-2")),
        (3, 3, new("Private First Class", "PFC", "E-3")),

        // Non-Commissioned Officers
        (4, 5, new("Specialist", "SPC", "E-4")),
        (6, 6, new("Corporal", "CPL", "E-4")),
        (7, 9, new("Sergeant", "SGT", "E-5")),
        (10, 12, new("Staff Sergeant", "SSG", "E-6")),
        (13, 15, new("Sergeant First Class", "SFC", "E-7")),

        // Senior NCOs
        (16, 18, new("Master Sergeant", "MSG", "E-8")),
        (19, 21, new("First Sergeant", "1SG", "E-8")),
        (22, 24, new("Sergeant Major", "SGM", "E-9")),
        (25, 25, new("Command Sergeant Major", "CSM", "E-9")),

        // Warrant Officers
        (26, 27, new("Warrant Officer 1", "WO1", "W-1")),
        (28, 29, new("Chief Warrant Officer 2", "CW2", "W-2")),
        (30, 31, new("Chief Warrant Officer 3", "CW3", "W-3")),
        (32, 33, new("Chief Warrant Officer 4", "CW4", "W-4")),
        (34, 35, new("Chief Warrant Officer 5", "CW5", "W-5")),

        // Company Grade Officers
        (36, 38, new("Second Lieutenant", "2LT", "O-1")),
        (39, 41, new("First Lieutenant", "1LT", "O-2")),
        (42, 45, new("Captain", "CPT", "O-3")),

        // Field Grade Officers
        (46, 48, new("Major", "MAJ", "O-4")),
        (49, 52, new("Lieutenant Colonel", "LTC", "O-5")),
        (53, 55, new("Colonel", "COL", "O-6")),

        // General Officers
        (56, 60, new("Brigadier General", "BG", "O-7")),
        (61, 65, new("Major General", "MG", "O-8")),
        (66, 70, new("Lieutenant General", "LTG", "O-9")),
        (71, 78, new("General", "GEN", "O-10")),

        // The Ultimate End Game
        (79, 79, new("General of the Army", "GA", "O-11")),
    ];

    private static readonly (int From, int To, ArmyRank Rank)[] BritishRanges =
    [
        // Other Ranks. There is no Private badge, so Private wears the Lance Corporal stripe.
        (1, 1, new("Private", "Pte", "OR-2", "UK_LCpl")),
        (2, 3, new("Lance Corporal", "LCpl", "OR-3", "UK_LCpl")),
        (4, 6, new("Corporal", "Cpl", "OR-4", "UK_Cpl")),
        (7, 10, new("Sergeant", "Sgt", "OR-6", "UK_Sgt")),
        (11, 14, new("Staff Sergeant", "SSgt", "OR-7", "UK_SSgt")),
        (15, 18, new("Colour Sergeant", "CSgt", "OR-7", "UK_SSgt")),

        // Warrant Officers
        (19, 21, new("Company Sergeant Major", "CSM", "OR-8", "UK_CSM")),
        (22, 24, new("Regimental Quartermaster Sergeant", "RQMS", "OR-8", "UK_RQMS")),
        (25, 28, new("Regimental Sergeant Major", "RSM", "OR-9", "UK_RSM")),
        (29, 30, new("Army Sergeant Major", "ASM", "OR-9", "UK_ASM")),

        // Officer Cadet and Junior Officers
        (31, 33, new("Officer Cadet", "OCdt", "OF-D", "UK_OCdt")),
        (34, 37, new("Second Lieutenant", "2Lt", "OF-1", "UK_2Lt")),
        (38, 41, new("Lieutenant", "Lt", "OF-1", "UK_Lt")),
        (42, 45, new("Captain", "Capt", "OF-2", "UK_Capt")),

        // Field Officers
        (46, 49, new("Major", "Maj", "OF-3", "UK_Maj")),
        (50, 53, new("Lieutenant Colonel", "Lt Col", "OF-4", "UK_LtCol")),
        (54, 57, new("Colonel", "Col", "OF-5", "UK_Col")),
        (58, 62, new("Brigadier", "Brig", "OF-6", "UK_Brig")),

        // General Officers
        (63, 67, new("Major General", "Maj Gen", "OF-7", "UK_MajGen")),
        (68, 72, new("Lieutenant General", "Lt Gen", "OF-8", "UK_LtGen")),
        (73, 78, new("General", "Gen", "OF-9", "UK_Gen")),

        // The Ultimate End Game
        (79, 79, new("Field Marshal", "FM", "OF-10", "UK_FM")),
    ];

    /// <summary>Rank for a player level. Below 1 counts as 1; above 79 stays the top rank.</summary>
    public static ArmyRank ForLevel(int level, RankSystem system = RankSystem.USArmy)
    {
        var ranges = system == RankSystem.BritishArmy ? BritishRanges : UsRanges;
        // Manual clamp: Math.Clamp doesn't exist in the .NET Framework the game client uses.
        var clamped = level < 1 ? 1 : level > MaxLevel ? MaxLevel : level;
        foreach (var (from, to, rank) in ranges)
        {
            if (clamped >= from && clamped <= to)
            {
                return rank;
            }
        }

        throw new InvalidOperationException($"No rank defined for level {clamped}");
    }
}
