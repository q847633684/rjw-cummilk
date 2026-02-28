using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using RimWorld;
using Verse;

namespace MilkCum.Milk.Data;
public class Gene_MilkTypeData : IExposable
{
    private static readonly FieldInfo cachedIcon = AccessTools.Field(typeof(GeneDef), "cachedIcon");
    public ThingDef ThingDef => thingDefName == null ? null : DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
    public string thingDefName;
    public int biostatArc = 0;
    public int biostatCpx = 0;
    public int biostatMet = 0;
    public float milkEfficiencyOffset = 0f;
    public float milkAmountOffset = 0f;
    public float milkEfficiencyFactor = 1f;
    public float milkAmountFactor = 1f;
    public void ExposeData()
    {
        Scribe_Values.Look(ref thingDefName, "thingDefName");
        Scribe_Values.Look(ref biostatArc, "biostatArc", 0);
        Scribe_Values.Look(ref biostatCpx, "biostatCpx", 0);
        Scribe_Values.Look(ref biostatMet, "biostatMet", 0);
        Scribe_Values.Look(ref milkEfficiencyOffset, "milkEfficiencyOffset", 0);
        Scribe_Values.Look(ref milkAmountOffset, "milkAmountOffset", 0);
        Scribe_Values.Look(ref milkEfficiencyFactor, "milkEfficiencyFactor", 1);
        Scribe_Values.Look(ref milkAmountFactor, "milkAmountFactor", 1);
    }
    public void SetMilkType(ThingDef thingDef)
    {
        if (thingDef == null) return;
        thingDefName = thingDef.defName;
    }
    public GeneDef GenGeneDef()
    {
        string defName = Constants.MILK_TYPE_PREFIX + ThingDef.defName;
        GeneDef geneDef = DefDatabase<GeneDef>.GetNamed(defName, errorOnFail: false) ?? new GeneDef();
        geneDef.defName = defName;
        geneDef.geneClass = typeof(Gene);
        geneDef.label = Lang.Join(Lang.MilkType, ":", ThingDef.label);
        geneDef.description = geneDef.label;
        geneDef.labelShortAdj = geneDef.label;
        geneDef.selectionWeight = 0f;
        geneDef.biostatCpx = this.biostatCpx;
        geneDef.biostatMet = this.biostatMet;
        geneDef.biostatArc = this.biostatArc;
        geneDef.displayCategory = DefDatabase<GeneCategoryDef>.GetNamed("Milk");
        geneDef.modContentPack = EqualMilkingMod.equalMilkingMod;
        geneDef.exclusionTags = new List<string> { "MilkType" };
        geneDef.statFactors = new List<StatModifier>();
        if (milkAmountFactor != 1f)
        {
            geneDef.statFactors.Add(new StatModifier { stat = EMDefOf.EM_Milk_Amount_Factor, value = milkAmountFactor });
        }
        if (milkEfficiencyFactor != 1f)
        {
            geneDef.statFactors.Add(new StatModifier { stat = EMDefOf.EM_Lactating_Efficiency_Factor, value = milkEfficiencyFactor });
        }
        geneDef.statOffsets = new List<StatModifier>();
        if (milkAmountOffset != 0f)
        {
            geneDef.statOffsets.Add(new StatModifier { stat = EMDefOf.EM_Milk_Amount_Factor, value = milkAmountOffset });
        }
        if (milkEfficiencyOffset != 0f)
        {
            geneDef.statOffsets.Add(new StatModifier { stat = EMDefOf.EM_Lactating_Efficiency_Factor, value = milkEfficiencyOffset });
        }
        if (geneDef.Icon == BaseContent.BadTex)
        {
            cachedIcon.SetValue(geneDef, TextureHelper.GenMilkGeneIcon(ThingDef));
        }
        return geneDef;
    }
    public void CopyFrom(Gene_MilkTypeData other)
    {
        if (other?.thingDefName == null) { this.thingDefName = null; return; }
        // Remove duplicates
        if (EqualMilkingSettings.genes.Contains(this))
        {
            foreach (Gene_MilkTypeData data in EqualMilkingSettings.genes.ToList())
            {
                if (data != this && data.thingDefName == other.thingDefName)
                {
                    EqualMilkingSettings.genes.Remove(data);
                }
            }
        }
        this.thingDefName = other.thingDefName;
        this.biostatArc = other.biostatArc;
        this.biostatCpx = other.biostatCpx;
        this.biostatMet = other.biostatMet;
        this.milkEfficiencyOffset = other.milkEfficiencyOffset;
        this.milkAmountOffset = other.milkAmountOffset;
        this.milkEfficiencyFactor = other.milkEfficiencyFactor;
        this.milkAmountFactor = other.milkAmountFactor;
    }
    public string StatSummary()
    {
        string str = "";
        if (milkAmountFactor != 1f || milkAmountOffset != 0f)
        {
            str += Lang.MilkAmount + ": ";
            if (milkAmountOffset != 0f)
            {
                str += (milkAmountOffset > 0f ? "+" : "") + milkAmountOffset.ToStringByStyle(ToStringStyle.PercentZero) + " ";
            }
            if (milkAmountFactor != 1f)
            {
                str += "x" + milkAmountFactor.ToStringByStyle(ToStringStyle.PercentZero); ;
            }

        }
        if (milkEfficiencyFactor != 1f || milkEfficiencyOffset != 0f)
        {
            if (str != "") { str += "\n"; }
            str += Lang.Join(Lang.Lactating, Lang.Efficiency) + ": ";
            if (milkEfficiencyOffset != 0f)
            {
                str += (milkEfficiencyOffset > 0f ? "+" : "") + milkEfficiencyOffset.ToStringByStyle(ToStringStyle.PercentZero) + " ";
            }
            if (milkEfficiencyFactor != 1f)
            {
                str += "x" + milkEfficiencyFactor.ToStringByStyle(ToStringStyle.PercentZero);
            }
        }
        return str;
    }
}