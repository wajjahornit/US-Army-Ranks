#nullable enable
using System;

namespace ArmyRanks;

/// <summary>Which army's ranks to show. U.S. Army is the default.</summary>
public enum RankSystem
{
    USArmy,
    BritishArmy,
    RussianArmy,
    GermanArmy,
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
        (26, 27, new("Warrant Officer I", "WO1", "W-1")),
        (28, 29, new("Chief Warrant Officer II", "CW2", "W-2")),
        (30, 31, new("Chief Warrant Officer III", "CW3", "W-3")),
        (32, 33, new("Chief Warrant Officer IV", "CW4", "W-4")),
        (34, 35, new("Chief Warrant Officer V", "CW5", "W-5")),

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
    private static readonly (int From, int To, ArmyRank Rank)[] GermanRanges =
    [
        // Mannschaften
        (1, 1, new("Schütze", "Schtz", "OR-1", "DE_Schtz")),
        (2, 2, new("Gefreiter", "Gefr", "OR-2", "DE_Gefr")),
        (3, 4, new("Obergefreiter", "OGefr", "OR-3", "DE_OGefr")),
        (5, 6, new("Hauptgefreiter", "HptGefr", "OR-3", "DE_HGefr")),
        (7, 8, new("Stabsgefreiter", "StGefr", "OR-4", "DE_StGefr")),
        (9, 10, new("Oberstabsgefreiter", "OStGefr", "OR-4", "DE_OStGefr")),
        (11, 12, new("Korporal", "Korp", "OR-4", "DE_Korp")),
        (13, 14, new("Stabskorporal", "StKorp", "OR-4", "DE_StKorp")),

        // Unteroffiziere ohne Portepee & Fahnenjunker
        (15, 16, new("Unteroffizier", "Uffz", "OR-5", "DE_Uffz")),
        (17, 18, new("Stabsunteroffizier", "StUffz", "OR-5", "DE_StUffz")),
        (19, 20, new("Fahnenjunker", "Fhj", "OR-5", "DE_Fhj")),

        // Unteroffiziere mit Portepee & Fähnriche
        (21, 22, new("Feldwebel", "Fw", "OR-6", "DE_Fw")),
        (23, 24, new("Oberfeldwebel", "OFw", "OR-6", "DE_OFw")),
        (25, 26, new("Hauptfeldwebel", "HptFw", "OR-7", "DE_HFw")),
        (27, 28, new("Stabsfeldwebel", "StFw", "OR-8", "DE_StFw")),
        (29, 30, new("Oberstabsfeldwebel", "OStFw", "OR-9", "DE_OStFw")),
        (31, 33, new("Fähnrich", "Fähnr", "OR-6", "DE_Faehnrich")),
        (34, 36, new("Oberfähnrich", "OFähnr", "OR-7", "DE_OFaehnrich")),

        // Leutnante & Hauptleute
        (37, 39, new("Leutnant", "Lt", "OF-1", "DE_Lt")),
        (40, 42, new("Oberleutnant", "OLt", "OF-1", "DE_OLt")),
        (43, 45, new("Hauptmann", "Hptm", "OF-2", "DE_Hptm")),
        (46, 48, new("Stabshauptmann", "StHptm", "OF-2", "DE_StHptm")),

        // Stabsoffiziere
        (49, 52, new("Major", "Maj", "OF-3", "DE_Maj")),
        (53, 56, new("Oberstleutnant", "Oberstlt", "OF-4", "DE_Oberstlt")),
        (57, 60, new("Oberst", "Oberst", "OF-5", "DE_Oberst")),

        // Generale
        (61, 65, new("Brigadegeneral", "BrigGen", "OF-6", "DE_BrigGen")),
        (66, 70, new("Generalmajor", "GenMaj", "OF-7", "DE_GenMaj")),
        (71, 75, new("Generalleutnant", "GenLt", "OF-8", "DE_GenLt")),
        (76, 79, new("General", "Gen", "OF-9", "DE_Gen")),
    ];
    private static readonly (int From, int To, ArmyRank Rank)[] RussianRanges =
    [
        // Soldaten (Рядовые)
        (1, 1, new("Рядовой", "Ряд.", "OR-1", "RU_Ryad")),
        (2, 4, new("Ефрейтор", "Ефр.", "OR-2", "RU_Yefr")),

        // Sergeanten (Сержанты)
        (5, 7, new("Младший сержант", "Мл. с-нт", "OR-4", "RU_MlSgt")),
        (8, 11, new("Сержант", "С-нт", "OR-5", "RU_Sgt")),
        (12, 15, new("Старший сержант", "Ст. с-нт", "OR-6", "RU_StSgt")),
        (16, 19, new("Старшина", "С-на", "OR-7", "RU_Stsh")),

        // Praporschtschiks (Прапорщики)
        (20, 24, new("Прапорщик", "Пр-к", "OR-8", "RU_Prap")),
        (25, 29, new("Старший прапорщик", "Ст. пр-к", "OR-9", "RU_StPrap")),

        // Leutnante & Hauptleute (Младшие офицеры)
        (30, 33, new("Младший лейтенант", "Мл. л-т", "OF-1", "RU_MlLt")),
        (34, 37, new("Лейтенант", "Л-т", "OF-1", "RU_Lt")),
        (38, 41, new("Старший лейтенант", "Ст. л-т", "OF-1", "RU_StLt")),
        (42, 46, new("Капитан", "К-н", "OF-2", "RU_Kpt")),

        // Stabsoffiziere (Старшие офицеры)
        (47, 51, new("Майор", "М-р", "OF-3", "RU_May")),
        (52, 56, new("Подполковник", "П/п-к", "OF-4", "RU_PPolk")),
        (57, 61, new("Полковник", "П-к", "OF-5", "RU_Polk")),

        // Generale (Высшие офицеры)
        (62, 66, new("Генерал-майор", "Ген.-м-р", "OF-6", "RU_GenMay")),
        (67, 71, new("Генерал-лейтенант", "Ген.-л-т", "OF-7", "RU_GenLt")),
        (72, 75, new("Генерал-полковник", "Ген.-п-к", "OF-8", "RU_GenPolk")),
        (76, 78, new("Генерал армии", "Ген. армии", "OF-9", "RU_GenArm")),

        // The Ultimate End Game
        (79, 79, new("Маршал Российской Федерации", "Маршал РФ", "OF-10", "RU_Marshal")),
    ];

    /// <summary>
    /// Where a level sits inside its rank's level range, from 1. Returns 0 when the rank
    /// covers a single level, so it needs no grade (Corporal, General of the Army).
    /// </summary>
    public static int GradeForLevel(int level, RankSystem system = RankSystem.USArmy)
    {
        var ranges = system == RankSystem.BritishArmy ? BritishRanges : UsRanges;
        var clamped = level < 1 ? 1 : level > MaxLevel ? MaxLevel : level;
        foreach (var (from, to, _) in ranges)
        {
            if (clamped >= from && clamped <= to)
            {
                return from == to ? 0 : clamped - from + 1;
            }
        }

        return 0;
    }

    /// <summary>1 to I, 4 to IV, 8 to VIII. Empty for 0.</summary>
    public static string ToRoman(int number)
    {
        var result = "";
        foreach (var (value, numeral) in new[] { (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I") })
        {
            while (number >= value)
            {
                result += numeral;
                number -= value;
            }
        }

        return result;
    }

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
