using System;

namespace ArmyRanks;

/// <summary>One U.S. Army rank, e.g. Sergeant / SGT / E-5.</summary>
public record ArmyRank(string Title, string Abbreviation, string PayGrade)
{
    /// <summary>"Sergeant (SGT / E-5)"</summary>
    public override string ToString() => $"{Title} ({Abbreviation} / {PayGrade})";
}

/// <summary>
/// Player level (1-79) to U.S. Army rank. Written as level ranges so it reads like the
/// spec; ForLevel() does the lookup. Both the server mod and the client plugin compile
/// this same file, so the two can never disagree.
/// </summary>
public static class ArmyRankTable
{
    public const int MaxLevel = 79;

    private static readonly (int From, int To, ArmyRank Rank)[] Ranges =
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

    /// <summary>Rank for a player level. Below 1 counts as 1; above 79 stays General of the Army.</summary>
    public static ArmyRank ForLevel(int level)
    {
        // Manual clamp: Math.Clamp doesn't exist in the .NET Framework the game client uses.
        var clamped = level < 1 ? 1 : level > MaxLevel ? MaxLevel : level;
        foreach (var (from, to, rank) in Ranges)
        {
            if (clamped >= from && clamped <= to)
            {
                return rank;
            }
        }

        throw new InvalidOperationException($"No rank defined for level {clamped}");
    }
}
