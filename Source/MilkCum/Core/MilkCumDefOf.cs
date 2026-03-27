using RimWorld;
using Verse;

namespace MilkCum.Core;

/// <summary>本 mod 的 Def 统一由此类引用（EM = Equal Milking）。技能/文档中的 EMDefOf 即指此类。</summary>
[DefOf]
public static class MilkCumDefOf
{
    public static EffecterDef EM_Milk;
    public static ThingDef EM_Prolactin;
    public static ThingDef EM_Lucilactin;
    public static ThingDef EM_HumanMilk;
    public static ThingDef EM_HumanMilkPartial;
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
    public static PawnColumnDef Milk_RemainingDays;
    public static MainButtonDef Milk_MainButton;

    // 成瘾系统新增定义
    public static ChemicalDef EM_Prolactin_Chemical;
    public static HediffDef EM_Prolactin_Tolerance;
    public static HediffDef EM_Prolactin_Addiction;
    public static NeedDef Chemical_EM_Prolactin_Chemical;
    public static ThoughtDef EM_Prolactin_Joy;
    public static HediffDef EM_Prolactin_High;
    public static ThoughtDef EM_Prolactin_HighThought;
    /// <summary>成瘾心情：ThoughtWorker_Hediff(hediff=EM_Prolactin_Addiction)，阶段 0=满足 +1、阶段 1=戒断 -12。</summary>
    public static ThoughtDef EM_ProlactinAddictionThought;
    public static ThoughtDef EM_HadSexWhileLactating;
    public static ThoughtDef EM_ForcedMilking;
    public static ThoughtDef EM_PartnerAteMyProduct;
    public static ThoughtDef EM_AllowedMilking;
    public static ThoughtDef EM_MilkPoolFull;
    public static ThoughtDef EM_LongTimeNotMilked;
    public static HediffDef EM_Mastitis;
    public static HediffDef EM_LactationalMilkStasis;
    public static HediffDef EM_BreastAbscess;
    public static ThoughtDef EM_Mastitis_Thought;
    public static HediffDef EM_BreastsEngorged;
    public static ThoughtDef EM_MilkOverflow;
    public static ThoughtDef EM_HighToleranceLowMilk;
    /// <summary>首次药物诱发泌乳成就类记忆。</summary>
    public static ThoughtDef EM_FirstLactationDrug;
    /// <summary>首次分娩泌乳成就类记忆。</summary>
    public static ThoughtDef EM_FirstLactationBirth;
    /// <summary>长期药物泌乳情境心情（药物诱发且持续约 15 天以上）。</summary>
    public static ThoughtDef EM_LongTermDrugLactation;
    /// <summary>3.1：被哺乳者获得「被某人哺乳」记忆，用于社交/关系。</summary>
    public static ThoughtDef EM_NursedBy;
    /// <summary>3.1：哺乳者获得「哺乳了某人」记忆，用于社交/关系。</summary>
    public static ThoughtDef EM_NursedSomeone;
    public static HediffDef EM_DrugLactationBurden;
    public static HediffDef EM_LactatingGain;
    public static HediffDef EM_AbsorptionDelay;

    // 管道系统（VE Framework 可选）：仅主 mod 可解析的 Def 类型；DesignatorDropdownGroupDef/PipeNetDef 在 PipeSystem 项目中用 GetNamed
    public static DesignationCategoryDef EM_PipeNetworks;
    public static ThingDef EM_MilkTap;
    public static ThingDef EM_HumanMilkTap;
    public static ThingDef EM_MilkPipe;
    public static ThingDef EM_UndergroundMilkPipe;
    public static ThingDef EM_MilkValve;
    public static ThingDef EM_MilkContainer;
}
