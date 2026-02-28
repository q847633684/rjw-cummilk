using System.Linq;
using MilkCum.Core;
using RimWorld;
using Verse;

namespace MilkCum.Milk.Helpers;
[StaticConstructorOnStartup]
public static class Lang
{
    static Lang() { LoadDefTranslations(); }
    public static void LoadDefTranslations()
    {
        EMDefOf.EM_Lucilactin.ingestible.ingestCommandString = Lang.Inject;
        EMDefOf.EM_Lucilactin.ingestible.ingestReportString = Lang.Injecting;
        EMDefOf.EM_Lucilactin.SetDefaultDesc(Lang.Join(Lang.Add, Lang.Lactating) + "(" + Lang.Permanent + ")");
        EMDefOf.EM_Prolactin.ingestible.ingestCommandString = Lang.Inject;
        EMDefOf.EM_Prolactin.ingestible.ingestReportString = Lang.Injecting;
        EMDefOf.EM_Prolactin.SetDefaultDesc(Lang.Join(Lang.Add, Lang.Lactating));
        EMDefOf.EM_ActiveSuckle.reportString = JobDefOf.BabySuckle.reportString;
        EMDefOf.EM_ForcedBreastfeed.reportString = JobDefOf.Breastfeed.reportString;

        EMDefOf.EM_Milk_Amount_Factor.SetDefaultLabel(Lang.Join(Lang.MilkAmount, Lang.StatFactor));
        EMDefOf.EM_Lactating_Efficiency_Factor.SetDefaultLabel(Lang.Join(Lang.Lactating, Lang.Efficiency, Lang.StatFactor));
        HediffDefOf.Lactating.SetDefaultDesc(HediffDefOf.Lactating.description.RemoveTailingLines(2)); //Delete pregnant rate description
        EMDefOf.Milk_Fullness.SetDefaultLabel(Lang.MilkFullness);
        EMDefOf.Milk_RemainingDays.SetDefaultLabel("EM.PoolRemainingDays".Translate());
        EMDefOf.Milk_Lactating.SetDefaultLabel(Lang.Lactating);
        EMDefOf.Milk_MilkType.SetDefaultLabel(Lang.MilkType);
        EMDefOf.Milk_MainButton.SetDefaultLabel("Equal_Milking".Translate());

        EMDefOf.EM_HumanMilk.SetDefaultLabel(Lang.Join(Lang.Human, Lang.Milk));
        EMDefOf.EM_HumanMilk.SetDefaultDesc(Lang.Join(Lang.Human, Lang.Milk) + "\n" + "Milk".DefDesc<ThingDef>().RemoveLeadingLines(1));

        EMDefOf.EM_MilkEntity.SetDefaultLabel(Lang.Join(Lang.Milk, Lang.Entities));
        EMDefOf.EM_MilkEntity.verb = Lang.Milk;
        EMDefOf.EM_MilkEntity.gerund = Lang.Milking;

        EMDefOf.EM_Lactation_Enhanced.SetDefaultLabel(Lang.Join(Lang.Enhanced, Lang.Lactating));
        EMDefOf.EM_Lactation_Enhanced.labelShortAdj = EMDefOf.EM_Lactation_Enhanced.label;
        EMDefOf.EM_Lactation_Enhanced.SetDefaultDesc();

        EMDefOf.EM_Lactation_Poor.SetDefaultLabel(Lang.Join(Lang.Poor, Lang.Lactating));
        EMDefOf.EM_Lactation_Poor.labelShortAdj = EMDefOf.EM_Lactation_Poor.label;
        EMDefOf.EM_Lactation_Poor.SetDefaultDesc();

        EMDefOf.EM_Permanent_Lactation.SetDefaultLabel(Lang.Join(Lang.Permanent, Lang.Lactating));
        EMDefOf.EM_Permanent_Lactation.labelShortAdj = EMDefOf.EM_Permanent_Lactation.label;
        EMDefOf.EM_Permanent_Lactation.SetDefaultDesc();

        EMDefOf.EM_MilkingPump.SetDefaultLabel(Lang.Join(Lang.Milking.CapitalizeFirst(), Lang.Pump));
        EMDefOf.EM_MilkingPump.SetDefaultDesc(Lang.DesignatedFor.Replace("{0}", Lang.Join(Lang.Milking, $"({Lang.Self})")));
        EMDefOf.EM_MilkingElectric.SetDefaultLabel(Lang.Join(Lang.Electric, Lang.Milking, Lang.Pump));
        EMDefOf.EM_MilkingElectric.SetDefaultDesc(Lang.DesignatedFor.Replace("{0}", Lang.Join(Lang.Milking, $"({Lang.Self})")));
    }
    public static DefInjectionPackage.DefInjection DefInjectionFor(this Def def, string field)
    {
        foreach (DefInjectionPackage defInjectionPackage in LanguageDatabase.activeLanguage.defInjections.Where(x => x.defType == def.GetType()))
        {
            if (defInjectionPackage.injections.TryGetValue(def.defName + "." + field, out DefInjectionPackage.DefInjection defInjection))
            {
                return defInjection;
            }
        }
        return null;
    }
    public static void SetDefaultLabel(this Def def, string label = null)
    {
        if (DefInjectionFor(def, "label") != null) { return; }
        if (label == null) { label = def.defName; }
        def.label = label;
    }
    public static void SetDefaultDesc(this Def def, string desc = null)
    {
        if (DefInjectionFor(def, "description") != null) { return; }
        if (desc == null) { desc = def.label; }
        def.description = desc;
    }
    // Static official translations
    public static string Add => "Add".Translate();
    public static string Adult => "Adult".Translate();
    public static string Advanced => "Advanced".Translate();
    public static string All => "All".Translate();
    public static string Allow => "CommandAllow".Translate();
    public static string Always => "ShowWeapons_Always".Translate();
    public static string Animal => "Animal".Translate();
    public static string AnimalFemaleAdult => "AnimalFemaleAdult".Translate();
    public static string Assign => "BuildingAssign".Translate();
    public static string AutofeedSetting => "AutofeedSectionHeader".Translate();
    public static string Baby => "Baby".Translate();
    public static string Buildings => "Buildings".Translate();
    public static string Cancel => "Cancel".Translate();
    public static string Child => "Child".Translate();
    public static string Choose => "WorldChooseButton".Translate();
    public static string ClickToEdit => "ClickToEdit".Translate();
    public static string ClickToSelect => "ModClickToSelect".Translate();
    public static string Colonist => "Colonist".Translate();
    public static string Confirm => "Confirm".Translate();
    public static string Days => "Days".Translate();
    public static string Default => "Default".Translate();
    public static string Delete => "Delete".Translate();
    public static string DesignatedFor => "DesignatedFor".Translate();
    public static string DevelopmentMode => "DevelopmentMode".Translate();
    public static string Disable => "Disable".Translate();
    public static string Edit => "Edit".Translate();
    public static string Efficiency => "Efficiency".Translate();
    public static string Energy = "MechEnergy".Translate();
    public static string Enhanced => "Enhanced".Translate();
    public static string Entities => "EntitiesSection".Translate();
    public static string Fed => "HungerLevel_Fed".Translate();
    public static string Forbid => "CommandForbid".Translate();
    public static string Forced => "ApparelForcedLower".Translate();
    public static string Gene => "Gene".Translate();
    public static string Genes => "Genes".Translate();
    public static string Human => "TargetHuman".Translate();
    public static string HungerRate => "HungerRate".Translate();
    public static string Hungry => "HungerLevel_Hungry".Translate();
    public static string InstallImplantAlreadyMaxLevel = "InstallImplantAlreadyMaxLevel".Translate();
    public static string Item => "ItemsTab".Translate();
    public static string MarketValue => "MarketValue".Translate();
    public static string Mechanoid => "Mechanoid".Translate();
    public static string Menu => "Menu".Translate();
    public static string MilkType => "Stat_Animal_MilkType".Translate();
    public static string MilkTypeDesc => "Stat_Animal_MilkTypeDesc".Translate();
    public static string MilkAmount => "Stat_Animal_MilkAmount".Translate();
    public static string MilkAmountDesc => "Stat_Animal_MilkAmountDesc".Translate();
    public static string MilkFullness => "MilkFullness".Translate();
    public static string MilkGrowthTime => "Stat_Animal_MilkGrowthTime".Translate();
    public static string MilkGrowthTimeDesc => "Stat_Animal_MilkGrowthTimeDesc".Translate();
    public static string Misc => "MiscRecordsCategory".Translate();
    public static string Nutrition => "Nutrition".Translate();
    public static string Off => "Off".Translate();
    public static string Overseer => "Overseer".Translate();
    public static string Pain => "Pain".Translate();
    public static string Pawn => "PawnsTabShort".Translate();
    public static string Permanent => "Permanent".Translate();
    public static string Poor => "Poor".Translate();
    public static string Prisoner => "Prisoner".Translate();
    public static string ProductWasted => "TextMote_ProductWasted".Translate();
    public static string Race => "Race".Translate();
    public static string Rename => "Rename".Translate();
    public static string Reset => "ResetBinding".Translate();
    public static string ResetAll => "ResetAll".Translate();
    public static string Self => "TargetSelf".Translate();
    public static string ShowAnimalNames => "ShowAnimalNames".Translate();
    public static string Storage => "TabStorage".Translate();
    public static string Slave => "Slave".Translate();
    public static string Starving => "HungerLevel_Starving".Translate();
    public static string Time => "TimeRecordsCategory".Translate();
    public static string UrgentlyHungry => "HungerLevel_UrgentlyHungry".Translate();
    public static string Unassign => "BuildingUnassign".Translate();
    // Translation From Defs
    public static string Age => DefDatabase<PawnColumnDef>.GetNamed("Age").label;
    public static string Breastfeed => DefDatabase<WorkGiverDef>.GetNamedSilentFail("BreastfeedBaby").verb.CapitalizeFirst();
    public static string Container => SameWordsIn("EmptyWasteContainer".DefLabel<WorkGiverDef>(), "ContainedGenepacksDesc".Translate());
    public static string Electric => SameWordsIn("ElectricStove".DefLabel<ThingDef>(), "ElectricSmithy".DefLabel<ThingDef>());
    public static string Lactating => HediffDefOf.Lactating.label.CapitalizeFirst();
    public static string Milk => "Milk".DefLabel<ThingDef>().CapitalizeFirst();
    public static string Milking => DefDatabase<WorkGiverDef>.GetNamed("Milk").gerund.CapitalizeFirst();
    public static string Inject => DefDatabase<ThingDef>.GetNamed("GoJuice").ingestible.ingestCommandString;
    public static string Injecting => DefDatabase<ThingDef>.GetNamed("GoJuice").ingestible.ingestReportString;
    public static string Pump = SameWordsIn("PollutionPump".DefLabel<ThingDef>(), "MoisturePump".DefLabel<ThingDef>());
    public static string Spot = SameWordsIn("CaravanPackingSpot".DefLabel<ThingDef>(), "PartySpot".DefLabel<ThingDef>());
    public static string SpotDesc = SameWordsIn("CaravanPackingSpot".DefDesc<ThingDef>(), "PartySpot".DefDesc<ThingDef>());
    public static string StatFactor = "StatFactor".DefLabel<ScenPartDef>();

    public static string Label(this HungerCategory hungerCategory)
    {
        switch (hungerCategory)
        {
            case HungerCategory.Fed:
                return Fed;
            case HungerCategory.Hungry:
                return Hungry;
            case HungerCategory.UrgentlyHungry:
                return UrgentlyHungry;
            case HungerCategory.Starving:
                return Starving;
            default:
                return Fed;
        }
    }
    public static string Of(this string noun, string adj)
    {
        return "ThingMadeOfStuffLabel".Translate(adj, noun);
    }
    public static string GiveTo(string from, string to)
    {
        string translated = "GiveItemsTo".Translate();
        return translated.Replace("{0}", from).Replace("{1_nameDef}", to);
    }
    public static string DisplayText(this ThingDef def)
    {
        return def.label.CapitalizeFirst() + " (" + def.defName + ")";
    }
    public static string RemoveTailingLines(this string str, int lines)
    {
        string[] linesArray = str.Split('\n');
        string result = string.Join("\n", linesArray.Take(linesArray.Count() >= 2 ? linesArray.Count() - lines : 0));
        return result;
    }
    public static string RemoveLeadingLines(this string str, int lines)
    {
        string[] linesArray = str.Split('\n');
        string result = string.Join("\n", linesArray.Skip(lines));
        return result;
    }
    public static string DefLabel<T>(this string defName) where T : Def
    {
        return DefDatabase<T>.GetNamed(defName)?.label ?? defName.Translate();
    }
    public static string DefDesc<T>(this string defName) where T : Def
    {
        return DefDatabase<T>.GetNamed(defName)?.description ?? "";
    }
    public static string Join(params string[] strings)
    {
        string[] taggedStrings = strings.ToArray();
        taggedStrings[0] = taggedStrings[0].CapitalizeFirst();
        string space = SpaceString(taggedStrings);
        return string.Join(space, taggedStrings); ;
    }

    private static string SpaceString(string[] taggedStrings)
    {
        return taggedStrings.Any(s => s.Any(c => IsCJKCharacter(c))) ? "" : " ";
    }
    public static string SameWordsIn(string s1, string s2)
    {
        string result = string.Empty;
        s1 = s1.ToLower().Replace(".", "").Replace(",", "");
        s2 = s2.ToLower().Replace(".", "").Replace(",", "");
        if (s1.Any(c => IsCJKCharacter(c)) || s2.Any(c => IsCJKCharacter(c)))
        {
            int index1 = -1;
            int index2 = -1;
            foreach (char c in s1)
            {
                if (s2.IndexOf(c) != -1)
                {
                    index1 = s1.IndexOf(c);
                    index2 = s2.IndexOf(c);
                    break;
                }
            }
            if (index1 == -1 || index2 == -1)
            {
                return result;
            }
            while (index1 < s1.Length && index2 < s2.Length && s1[index1] == s2[index2])
            {
                result += s1[index1];
                index1++;
                index2++;
            }
            return result;

        }
        string[] words1 = s1.Split(' ');
        string[] words2 = s2.Split(' ');
        string[] sameWords = words1.Intersect(words2).ToArray();
        result = string.Join(" ", sameWords);
        return result;
    }
    private static bool IsCJKCharacter(char c)
    {
        // Check if the character is within the CJK (Chinese, Japanese, Korean) Unicode ranges
        return (c >= 0x4E00 && c <= 0x9FFF) || // CJK Unified Ideographs
               (c >= 0x3400 && c <= 0x4DBF) || // CJK Unified Ideographs Extension A
               (c >= 0x20000 && c <= 0x2A6DF) || // CJK Unified Ideographs Extension B
               (c >= 0x2A700 && c <= 0x2B73F) || // CJK Unified Ideographs Extension C
               (c >= 0x2B740 && c <= 0x2B81F) || // CJK Unified Ideographs Extension D
               (c >= 0x2B820 && c <= 0x2CEAF) || // CJK Unified Ideographs Extension E
               (c >= 0xF900 && c <= 0xFAFF) ||   // CJK Compatibility Ideographs
               (c >= 0x2F800 && c <= 0x2FA1F);   // CJK Compatibility Ideographs Supplement
    }
}