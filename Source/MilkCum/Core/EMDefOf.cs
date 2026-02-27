using RimWorld;
using Verse;
namespace MilkCum.Core;
[DefOf]
public static class EMDefOf
{
    public static EffecterDef EM_Milk;
    public static StatDef EM_Milk_Amount_Factor;
    public static StatDef EM_Lactating_Efficiency_Factor;
    public static ThingDef EM_Prolactin;
    public static ThingDef EM_Lucilactin;
    public static ThingDef EM_HumanMilk;
    public static ThingDef EM_MilkingPump;
    public static ThingDef EM_MilkingElectric;
    public static JobDef EM_InjectLactatingDrug;
    public static JobDef EM_ForcedBreastfeed;
    public static JobDef EM_ActiveSuckle;
    public static GeneDef EM_Lactation_Enhanced;
    public static GeneDef EM_Lactation_Poor;
    public static GeneDef EM_Permanent_Lactation;
    public static WorkGiverDef EM_MilkEntity;
    public static PawnTableDef Milk_PawnTable;
    public static PawnColumnDef Milk_MilkType;
    public static PawnColumnDef Milk_Lactating;
    public static PawnColumnDef Milk_Fullness;
    public static MainButtonDef Milk_MainButton;
    
    // 成瘾系统新增定义
    public static ChemicalDef EM_Prolactin_Chemical;
    public static HediffDef EM_Prolactin_Tolerance;
    public static HediffDef EM_Prolactin_Addiction;
    public static NeedDef Chemical_EM_Prolactin;
    public static ThoughtDef EM_Prolactin_Joy;
    public static ThoughtDef EM_Prolactin_Withdrawal;
    public static HediffDef EM_Prolactin_High;
    public static ThoughtDef EM_Prolactin_HighThought;
    public static ThoughtDef EM_HadSexWhileLactating;
    public static ThoughtDef EM_ForcedMilking;
    public static ThoughtDef EM_PartnerAteMyProduct;
    public static ThoughtDef EM_AllowedMilking;
    public static ThoughtDef EM_AddictionSatisfied;
    public static ThoughtDef EM_MilkPoolFull;
    public static ThoughtDef EM_LongTimeNotMilked;
    public static HediffDef EM_Mastitis;
    public static ThoughtDef EM_Mastitis_Thought;
    public static HediffDef EM_BreastsEngorged;
    public static HediffDef EM_DrugLactationBurden;
    public static HediffDef EM_LactatingGain;
    public static HediffDef EM_AbsorptionDelay;
}