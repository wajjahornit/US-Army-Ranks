using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using EFT.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmyRanks
{
    public enum RankDisplayStyle
    {
        /// <summary>"Staff Sergeant"</summary>
        FullTitle,
        /// <summary>"Staff Sergeant (SSG / E-6)"</summary>
        FullTitleWithGrade,
        /// <summary>"SSG"</summary>
        Abbreviation,
        /// <summary>"E-6"</summary>
        PayGrade,
        /// <summary>"SSG E-6"</summary>
        AbbreviationAndPayGrade,
    }

    /// <summary>
    /// BepInEx loads every class marked [BepInPlugin] from BepInEx\plugins when the game starts.
    /// Awake() runs once; we use it to read the config and switch on our Harmony patches.
    /// The soft dependency makes BepInEx load MenuOverhaul first when it's installed (so we can
    /// hook it), and changes nothing when it isn't.
    /// </summary>
    [BepInPlugin("com.cotti.armyranks.client", "ArmyRanks Client", "1.2.0")]
    [BepInDependency(MenuOverhaulCompat.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<RankDisplayStyle> DisplayStyle;
        internal static ConfigEntry<RankSystem> ArmySetting;
        internal static ConfigEntry<bool> ShowGrades;

        /// <summary>" II" when this level is the 2nd of a rank that lasts several levels, else "".</summary>
        internal static string GradeSuffix(int level)
        {
            if (ShowGrades == null || !ShowGrades.Value)
            {
                return "";
            }

            // Warrant officer names already end in a number ("Chief Warrant Officer 2"), and
            // "Chief Warrant Officer 2 II" reads badly, so those keep their plain name.
            var title = RankFor(level).Title;
            if (char.IsDigit(title[title.Length - 1]))
            {
                return "";
            }

            var grade = ArmyRankTable.GradeForLevel(level, ArmySetting.Value);
            return grade > 0 ? " " + ArmyRankTable.ToRoman(grade) : "";
        }

        /// <summary>The rank for a level in the army picked in the config.</summary>
        internal static ArmyRank RankFor(int level) => ArmyRankTable.ForLevel(level, ArmySetting.Value);

        private void Awake()
        {
            // Saved to BepInEx\config\com.cotti.armyranks.client.cfg, editable in-game with F12 (Configuration Manager)
            DisplayStyle = Config.Bind(
                "Display",
                "Rank format",
                RankDisplayStyle.FullTitle,
                "How a rank is written where the game used to show your level number.");
            ArmySetting = Config.Bind(
                "Display",
                "Rank system",
                RankSystem.USArmy,
                "Which army's ranks and emblems to use. Reopen a screen to see the change.");
            ShowGrades = Config.Bind(
                "Display",
                "Rank grades",
                true,
                "Number a rank that lasts several levels with roman numerals (Sergeant I, Sergeant II, Sergeant III). Ranks that last one level stay as they are.");

            // Harmony finds the [HarmonyPatch] classes below and hooks them onto the game's methods.
            var harmony = new Harmony("com.cotti.armyranks.client");
            harmony.PatchAll(typeof(Plugin).Assembly);

            // MenuOverhaul draws its own main-menu level, so it gets its own hook when present.
            if (MenuOverhaulCompat.TryPatch(harmony))
            {
                Logger.LogInfo("ArmyRanks: MenuOverhaul found, main menu level will show your rank");
            }

            Logger.LogInfo("ArmyRanks client loaded");
        }

        /// <summary>The text to show for a level, in the style picked in the config.</summary>
        internal static string RankText(int level)
        {
            var rank = RankFor(level);
            var grade = GradeSuffix(level);
            switch (DisplayStyle.Value)
            {
                case RankDisplayStyle.FullTitleWithGrade:
                    return rank.Title + grade + " (" + rank.Abbreviation + " / " + rank.PayGrade + ")";
                case RankDisplayStyle.Abbreviation:
                    return rank.Abbreviation + grade;
                case RankDisplayStyle.PayGrade:
                    return rank.PayGrade + grade;
                case RankDisplayStyle.AbbreviationAndPayGrade:
                    return rank.Abbreviation + grade + " " + rank.PayGrade;
                default:
                    return rank.Title + grade;
            }
        }

        /// <summary>
        /// Same as RankText, but a multi-word name is split onto two lines of similar length
        /// ("Master\nSergeant", "Sergeant First\nClass") so it stays compact next to the icon.
        /// </summary>
        internal static string StackedRankText(int level)
        {
            var rank = RankFor(level);
            switch (DisplayStyle.Value)
            {
                // The numeral stays on the last line with the end of the name ("Staff\nSergeant II").
                case RankDisplayStyle.FullTitle:
                    return SplitInTwo(rank.Title) + GradeSuffix(level);
                case RankDisplayStyle.FullTitleWithGrade:
                    return rank.Title + GradeSuffix(level) + "\n(" + rank.Abbreviation + " / " + rank.PayGrade + ")";
                default:
                    return RankText(level);
            }
        }

        /// <summary>Breaks text at the space that makes the two lines closest in length.</summary>
        private static string SplitInTwo(string text)
        {
            var best = -1;
            var bestLongest = int.MaxValue;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != ' ')
                {
                    continue;
                }

                var longest = Math.Max(i, text.Length - i - 1);
                if (longest < bestLongest)
                {
                    best = i;
                    bestLongest = longest;
                }
            }

            return best < 0 ? text : text.Substring(0, best) + "\n" + text.Substring(best + 1);
        }

        // Each label's own font size before we first touched it. PlayerLevelPanel.Set runs many
        // times on the same label, so scaling the current size each time would keep shrinking it.
        private static readonly Dictionary<TMP_Text, float> OriginalFontSizes = new Dictionary<TMP_Text, float>();

        /// <summary>
        /// Writes the rank into a text box that was sized for a 2-digit number. Short ranks
        /// (SGT, E-5) keep the game's size. Long ones stack onto two lines at a reduced size and
        /// are allowed to run past the box instead of being squeezed by auto-size.
        /// With leftAlign the text starts at the box's left edge and runs right, away from
        /// the chevron icon that sits to its left.
        /// </summary>
        internal static void ShowRank(TMP_Text label, int level, bool leftAlign = false)
        {
            if (label == null)
            {
                return;
            }

            if (!OriginalFontSizes.TryGetValue(label, out var originalSize))
            {
                originalSize = label.fontSize;
                OriginalFontSizes[label] = originalSize;
            }

            var text = StackedRankText(level);
            label.enableAutoSizing = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            if (leftAlign)
            {
                // Keep the vertical part of the alignment (the high byte), make the horizontal part "left".
                label.alignment = (TextAlignmentOptions)(((int)label.alignment & 0xFF00) | 0x01);
            }

            // Two lines have to fit in the height of one, so they get a bit under half size.
            label.fontSize = text.Contains("\n") ? originalSize * 0.42f
                : text.Length > 3 ? originalSize * 0.5f
                : originalSize;
            label.text = text;
        }
    }

    /// <summary>
    /// PlayerLevelPanel is the little level badge (number + chevron icon) the game reuses on the
    /// character screen, the experience bar, the post-raid screen, the matchmaker and party lists.
    /// Its Set() method writes the level as "07" into a field called _level.
    /// A Postfix runs straight after the original method, so we simply overwrite that text.
    /// "____level" (four underscores) is Harmony's way of handing us the private field "_level".
    /// </summary>
    [HarmonyPatch(typeof(PlayerLevelPanel), nameof(PlayerLevelPanel.Set))]
    internal static class PlayerLevelPanelSetPatch
    {
        private static void Postfix(int level, ESideType side, TextMeshProUGUI ____level, Image ____icon)
        {
            // The game hides the badge for Scavs; leave those alone.
            if (side == ESideType.Pmc)
            {
                Plugin.ShowRank(____level, level, leftAlign: true);
                RankIcons.Apply(____icon, level);
                RankIcons.PullTextToIcon(____level, ____icon);
            }
        }
    }

    // The trader screen is deliberately left alone: its loyalty table compares your level
    // number against each loyalty tier's required level, so it keeps the game's numbers.
    // Only its icon changes, through the SetLevelIcon patch below.

    /// <summary>
    /// MoxoPixel's MenuOverhaul replaces the main menu and writes your level itself, in
    /// PlayerProfileStatsController.UpdateLevelDisplay(), into a text object at
    /// BottomField/LevelInfoRow/Level. We can't reference that mod directly (it may not be
    /// installed), so we look its method up by name at runtime and patch it only if found.
    /// </summary>
    internal static class MenuOverhaulCompat
    {
        public const string Guid = "com.moxopixel.menuoverhaul";

        public static bool TryPatch(Harmony harmony)
        {
            var target = AccessTools.Method(
                "MoxoPixel.MenuOverhaul.Helpers.PlayerProfileStatsController:UpdateLevelDisplay");
            if (target == null)
            {
                return false;
            }

            harmony.Patch(target, postfix: new HarmonyMethod(typeof(MenuOverhaulCompat), nameof(UpdateLevelDisplayPostfix)));
            return true;
        }

        // Parameter names match MenuOverhaul's method, which is how Harmony knows what to pass in.
        private static void UpdateLevelDisplayPostfix(Transform bottomField, Profile profile)
        {
            var levelObject = bottomField != null ? bottomField.Find("LevelInfoRow/Level") : null;
            var label = levelObject != null ? levelObject.GetComponent<TextMeshProUGUI>() : null;
            if (label == null || profile?.Info == null)
            {
                return;
            }

            // MenuOverhaul has just written the number at 54pt. Measure how wide that number is
            // and pin the text box to exactly that width with a LayoutElement. The row's layout
            // then stays exactly as MenuOverhaul designed it, whatever we write into the box.
            var slotWidth = label.GetPreferredValues(profile.Info.Level.ToString()).x;
            var layout = levelObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = levelObject.gameObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = slotWidth;
            layout.preferredWidth = slotWidth;
            layout.flexibleWidth = 0f;

            // The box is right-aligned with wrapping off, so a longer rank runs out to the LEFT
            // of the box, towards the model, instead of off the right edge of the screen.
            // The label may have auto-size on, which would squeeze the rank into the pinned
            // width (that's what made it tiny), so it's switched off. Long ranks stack onto two
            // lines at 30pt, with line spacing pulled in a little so they sit close together.
            var text = Plugin.StackedRankText(profile.Info.Level);
            var stacked = text.Contains("\n");
            label.enableAutoSizing = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableWordWrapping = false;
            label.fontSize = stacked ? 30f : text.Length > 3 ? 40f : 54f;
            label.lineSpacing = stacked ? -8f : 0f;
            label.text = text;

            // The rank emblem replaces the game's chevron here too. MenuOverhaul puts the icon to
            // the LEFT of the text, which is exactly where a long rank now runs, so flip the row:
            // emblem on the right edge, rank text growing leftwards from it.
            var icon = bottomField.Find("LevelInfoRow/Level Icon");
            if (icon != null)
            {
                icon.gameObject.SetActive(true);
                var image = icon.GetComponent<Image>();
                if (image != null)
                {
                    RankIcons.Apply(image, profile.Info.Level);

                    // MenuOverhaul's icon slot is a 56x56 square. Make it a bit taller (64) so the
                    // emblem reads clearly, and as wide as the emblem itself so its spacing is real.
                    var iconLayout = icon.GetComponent<LayoutElement>();
                    if (iconLayout != null && image.sprite != null && image.sprite.rect.height > 0f)
                    {
                        const float emblemHeight = 64f;
                        var width = Mathf.Min(emblemHeight, emblemHeight * image.sprite.rect.width / image.sprite.rect.height);
                        iconLayout.minWidth = width;
                        iconLayout.preferredWidth = width;
                        iconLayout.minHeight = emblemHeight;
                        iconLayout.preferredHeight = emblemHeight;
                    }
                }
            }

            var row = bottomField.Find("LevelInfoRow");
            var rowLayout = row != null ? row.GetComponent<HorizontalLayoutGroup>() : null;
            if (rowLayout != null)
            {
                rowLayout.reverseArrangement = false;
                rowLayout.spacing = 18f; // breathing room between rank title and emblem
            }
        }
    }

    /// <summary>
    /// The rank emblems, built into the DLL as embedded resources (Icons\SGT.png etc.) so there
    /// are no loose files to install. Each PNG is turned into a Unity Sprite the first time it's
    /// needed and cached. Ranks without an emblem (PV1 wears none; warrant officers aren't
    /// included yet) keep the game's own icon.
    /// </summary>
    internal static class RankIcons
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite ForLevel(int level)
        {
            var key = Plugin.RankFor(level).IconKey;
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            Sprite sprite = null;
            using (var stream = typeof(RankIcons).Assembly.GetManifestResourceStream("ArmyRanks.Icons." + key + ".png"))
            {
                if (stream != null)
                {
                    var bytes = new byte[stream.Length];
                    var read = 0;
                    while (read < bytes.Length)
                    {
                        read += stream.Read(bytes, read, bytes.Length - read);
                    }

                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (ImageConversion.LoadImage(texture, bytes))
                    {
                        texture.filterMode = FilterMode.Bilinear;
                        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
            }

            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Slides left-aligned badge text over so it starts just right of the emblem. The icon
        /// slot is square and most emblems are narrow, so the visible emblem ends well before the
        /// slot does. A TextMeshPro margin moves the text without touching the layout, and since
        /// it's recalculated from the rects every time it never builds up.
        /// </summary>
        public static void PullTextToIcon(TMP_Text label, Image icon)
        {
            if (label == null || icon == null || icon.sprite == null)
            {
                return;
            }

            var labelRect = label.rectTransform;
            var corners = new Vector3[4];
            icon.rectTransform.GetWorldCorners(corners);
            var bottomLeft = labelRect.InverseTransformPoint(corners[0]);
            var topRight = labelRect.InverseTransformPoint(corners[2]);
            var iconWidth = topRight.x - bottomLeft.x;
            var iconHeight = topRight.y - bottomLeft.y;
            var spriteRect = icon.sprite.rect;
            if (iconWidth <= 0f || iconHeight <= 0f || spriteRect.height <= 0f)
            {
                return; // not laid out yet
            }

            var drawnWidth = Mathf.Min(iconWidth, iconHeight * spriteRect.width / spriteRect.height);
            var emblemRight = (bottomLeft.x + topRight.x) / 2f + drawnWidth / 2f;
            var gap = labelRect.rect.xMin - emblemRight;
            var wanted = iconHeight * 0.4f;
            var margin = label.margin;
            margin.x = gap > wanted ? -(gap - wanted) : 0f;
            label.margin = margin;
        }

        /// <summary>Puts the rank emblem on an icon, if this rank has one.</summary>
        public static void Apply(Image image, int level)
        {
            var sprite = image != null ? ForLevel(level) : null;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }
        }
    }

    /// <summary>
    /// PlayerLevelPanel.SetLevelIcon picks the game's chevron for a level. Every level icon in
    /// the game goes through it (badges, trader screen, and MenuOverhaul's main menu), so one
    /// postfix swaps them all for the rank emblem.
    /// </summary>
    [HarmonyPatch(typeof(PlayerLevelPanel), nameof(PlayerLevelPanel.SetLevelIcon))]
    internal static class SetLevelIconPatch
    {
        private static void Postfix(Image image, int level)
        {
            RankIcons.Apply(image, level);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // .NET Framework lacks this marker type, which C# records need. Declaring it here is the standard fix.
    internal static class IsExternalInit
    {
    }
}
