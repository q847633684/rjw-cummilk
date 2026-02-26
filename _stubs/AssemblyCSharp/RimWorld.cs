#pragma warning disable CS0626, CS0824, CS0114, CS0108, CS0067, CS0649, CS0169, CS0414, CS0109
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class Alert { public virtual string GetLabel() => ""; public virtual TaggedString GetExplanation() => default; public virtual AlertReport GetReport() => default; public Alert() { } public float lastBellTime; }
    public struct AlertReport { public bool active; public IEnumerable<Thing> AllCulprits => null; public static AlertReport CulpritIs(Thing t) => default; public static AlertReport CulpritsAre(IEnumerable<Thing> culprits) => default; public static implicit operator AlertReport(bool b) => default; public static implicit operator AlertReport(Thing t) => default; }

    public class Building_Storage : Building, IStoreSettingsParent, ISlotGroupParent, IHaulDestination
    {
        public StorageSettings settings;
        public virtual bool StorageTabVisible => true;
        public virtual StorageSettings GetStoreSettings() => settings;
        public virtual StorageSettings GetParentStoreSettings() => null;
    }

    public class Need : IExposable
    {
        public NeedDef def;
        public Pawn pawn;
        public float CurLevel { get; set; }
        public float CurLevelPercentage { get; set; }
        public float MaxLevel => 1;
        public virtual void ExposeData() { }
        public virtual void NeedInterval() { }
        public virtual void SetInitialLevel() { }
        public virtual string GetTipString() => "";
        public virtual void DrawOnGUI(Rect rect, int maxThresholdMarkers = 2147483647, float customMargin = -1, bool drawArrows = true, bool doTooltip = true, Rect? customRect = null, bool drawLabel = true) { }
    }
    public class Need_MechEnergy : Need { public float BaseFallPerDay => 0; }
    public class Need_Food : Need { public float FoodFallPerTick => 0; public Pawn_FoodTracker foodTracker; }
    public class Pawn_FoodTracker { }

    public class JobDriver_Breastfeed : JobDriver
    {
        public Pawn Breastfeed => null;
        public override bool TryMakePreToilReservations(bool errorOnFailed) => false;
        protected override IEnumerable<Toil> MakeNewToils() => null;
    }
    public class JobDriver_Milk : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => false;
        protected override IEnumerable<Toil> MakeNewToils() => null;
        protected virtual CompHasGatherableBodyResource GetComp(Pawn pawn) => null;
    }
    public class JobDriver_FeedBaby : JobDriver
    {
        public Pawn Baby => null;
        public override bool TryMakePreToilReservations(bool errorOnFailed) => false;
        protected override IEnumerable<Toil> MakeNewToils() => null;
    }
    public class JobDriver_Vomit : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => false;
        protected override IEnumerable<Toil> MakeNewToils() => null;
    }
    public class WorkGiver : Def { public WorkTypeDef workType; public virtual ThingRequest PotentialWorkThingRequest => default; }
    public class WorkGiver_Scanner : WorkGiver { public virtual IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => null; public virtual bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => false; public virtual Job JobOnThing(Pawn pawn, Thing t, bool forced = false) => null; public virtual bool ShouldSkip(Pawn pawn, bool forced = false) => false; public virtual Danger MaxPathDanger(Pawn pawn) => Danger.Some; public virtual PathEndMode PathEndMode => PathEndMode.Touch; public virtual IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn) => null; public virtual bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false) => false; public virtual Job JobOnCell(Pawn pawn, IntVec3 cell, bool forced = false) => null; }
    public class WorkGiver_FeedBabyManually : WorkGiver_Scanner { public static bool CanCreateManualFeedingJob(Pawn pawn, Pawn baby, out Thing food, bool forced = false) { food = null; return false; } }
    public class ITab_Pawn_Feeding : ITab { public class BabyFeederPair { public Pawn baby; public Pawn feeder; } public static FloatMenuOption GenerateFloatMenuOption(Pawn pawn, Pawn target) => null; public static void DrawRow(Rect rect, Pawn baby, Pawn feeder, ref float curY) { } }
    public class ITab : Verse.Window { public Pawn SelPawnForGear => null; }
    public class FloatMenuMakerMap { public static void AddHumanlikeOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts) { } }

    public class PawnColumnWorker { public PawnColumnDef def; public virtual void DoCell(Rect rect, Pawn pawn, PawnTable table) { } public virtual void DoHeader(Rect rect, PawnTable table) { } public virtual int GetMinWidth(PawnTable table) => 0; public virtual int GetMaxWidth(PawnTable table) => 0; public virtual int GetOptimalWidth(PawnTable table) => 0; public virtual int GetMinHeaderHeight(PawnTable table) => 0; public virtual int Compare(Pawn a, Pawn b) => 0; protected virtual string GetHeaderTip(PawnTable table) => ""; public virtual bool VisibleCurrently => true; }
    public class PawnColumnWorker_Checkbox : PawnColumnWorker { protected virtual bool GetValue(Pawn pawn) => false; protected virtual void SetValue(Pawn pawn, bool value, PawnTable table) { } protected virtual bool HasCheckbox(Pawn pawn) => true; protected virtual string GetTip(Pawn pawn) => ""; }
    public class PawnColumnWorker_Text : PawnColumnWorker { protected virtual string GetTextFor(Pawn pawn) => ""; }
    public class PawnColumnWorker_Icon : PawnColumnWorker { protected virtual Texture2D GetIconFor(Pawn pawn) => null; protected virtual string GetIconTip(Pawn pawn) => null; protected virtual Color GetIconColor(Pawn pawn) => Color.white; protected virtual void ClickedIcon(Pawn pawn) { } }
    public class PawnTable { public List<Pawn> PawnsListForReading => null; }
    public class PawnTable_PlayerPawns : PawnTable { protected virtual IEnumerable<Pawn> LabelSortFunction(IEnumerable<Pawn> pawns) => pawns; }
    public class PawnTableDef : Def { }
    public class PawnColumnDef : Def { public Type workerClass; public PawnColumnWorker Worker => null; }
    public class MainTabWindow : Verse.Window { protected virtual float ExtraBottomSpace => 0; protected virtual float ExtraTopSpace => 0; protected virtual IEnumerable<Pawn> Pawns => null; public override void DoWindowContents(Rect inRect) { } }
    public class MainTabWindow_PawnTable : MainTabWindow { protected new virtual PawnTableDef PawnTableDef => null; public PawnTableDef pawnTableDef; }

    public class CompProperties_BiosculpterPod_BaseCycle : CompProperties { public float durationDays; public virtual TaggedString CycleDescription => default; public virtual void CompleteCycle(Pawn pawn) { } }

    public static class ThingDefOf { public static ThingDef Milk; public static ThingDef MilkHuman; public static ThingDef Filth_Vomit; public static ThingDef Filth_AmnioticFluid; public static ThingDef Mote_FeedbackGoto; public static ThingDef Mote_FeedbackEquip; public static ThingDef Silver; public static ThingDef Steel; public static ThingDef WoodLog; public static ThingDef Gold; public static ThingDef Plasteel; public static ThingDef Hyperweave; public static ThingDef MealSurvivalPack; public static ThingDef MealNutrientPaste; public static ThingDef MealSimple; public static ThingDef MealFine; public static ThingDef MealLavish; public static ThingDef BabyFood; public static ThingDef HemogenPack; public static ThingDef Meat_Human; }
    public static class JobDefOf { public static JobDef Wait; public static JobDef Wait_MaintainPosture; public static JobDef GotoWander; public static JobDef Goto; public static JobDef Ingest; public static JobDef HaulToCell; public static JobDef HaulToContainer; public static JobDef Vomit; public static JobDef Breastfeed; public static JobDef FeedBaby; public static JobDef BottleFeedBaby; public static JobDef Milk; public static JobDef Research; public static JobDef DoBill; public static JobDef Clean; }
    public static class StatDefOf { public static StatDef MoveSpeed; public static StatDef WorkSpeedGlobal; public static StatDef MedicalSurgerySuccessChance; public static StatDef FoodPoisonChanceFixedHuman; public static StatDef MarketValue; public static StatDef Mass; public static StatDef Flammability; public static StatDef Beauty; public static StatDef NutritionIngestionSpeed; public static StatDef AnimalGatherYield; public static StatDef AnimalGatherSpeed; public static StatDef MaxHitPoints; public static StatDef MeatAmount; public static StatDef LeatherAmount; }
    public static class HediffDefOf { public static HediffDef Pregnant; public static HediffDef Malnutrition; public static HediffDef Hypothermia; public static HediffDef Heatstroke; public static HediffDef FoodPoisoning; public static HediffDef Lactating; public static HediffDef PsychicBond; public static HediffDef CryptosleepSickness; }
    public static class BodyPartGroupDefOf { public static BodyPartGroupDef Torso; public static BodyPartGroupDef FullHead; public static BodyPartGroupDef UpperHead; public static BodyPartGroupDef Eyes; public static BodyPartGroupDef Legs; }
    public static class PawnCapacityDefOf { public static PawnCapacityDef Consciousness; public static PawnCapacityDef Moving; public static PawnCapacityDef Manipulation; public static PawnCapacityDef Breathing; public static PawnCapacityDef BloodFiltration; public static PawnCapacityDef BloodPumping; public static PawnCapacityDef Metabolism; public static PawnCapacityDef Sight; public static PawnCapacityDef Hearing; public static PawnCapacityDef Talking; public static PawnCapacityDef Eating; }

    public class SurgeryOutcome { }

    public static class StoreUtility { public static bool TryFindBestBetterStoreCellFor(Thing thing, Pawn carrier, Map map, StoragePriority currentPriority, Faction faction, out IntVec3 foundCell, bool needAccurateResult = true) { foundCell = default; return false; } public static bool IsInAnyStorage(this Thing t) => false; }
    public enum StoragePriority { Unstored, Low, Normal, Preferred, Important, Critical }

    public static class GenRecipe { public static IEnumerable<Thing> MakeRecipeProducts(RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver, Precept_ThingStyle precept = null, QuestPart_LootPolicy lootPolicy = null, bool forceHighQuality = false, List<ThingDefCount> outLeavings = null) => null; }
    public class IBillGiver { }
    public class Precept_ThingStyle { }
    public class QuestPart_LootPolicy { }
    public class ThingDefCount { public ThingDef thingDef; public int count; }

    public static class FoodUtility { public static float GetBodyPartNutrition(Corpse corpse, BodyPartRecord part) => 0; public static float GetNutrition(Thing foodSource, ThingDef foodDef) => 0; public static bool IsAcceptableFood(Thing food, Pawn eater) => false; public static float NutritionForEater(Pawn eater, Thing food) => 0; }

    public static class AnimalProductionUtility { public static bool ProducesOrHarvestable(Pawn p) => false; }

    public class StatWorker { public virtual float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true) => 0; public virtual string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense) => ""; public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req) => null; }
    public struct StatRequest { public Thing Thing => null; public ThingDef StuffDef => null; public static StatRequest For(Thing thing) => default; public static StatRequest For(ThingDef thingDef, ThingDef stuffDef) => default; public Pawn Pawn => null; public ThingDef Def => null; public bool HasThing => false; }
    public enum ToStringNumberSense { Undefined, Absolute, Factor, Offset }

    public class Pawn_NeedsTracker { public Need_Food food; public T TryGetNeed<T>() where T : Need => default; }
    public class Pawn_StoryTracker { public TraitSet traitSet; }
    public class Pawn_GeneTracker { public bool HasActiveGene(GeneDef geneDef) => false; public Gene GetGene(GeneDef geneDef) => null; public Gene GetFirstGeneOfType<T>() where T : Gene => default; public List<Gene> GenesListForReading => null; public XenotypeDef Xenotype => null; }
    public class XenotypeDef : Def { }
    public class Pawn_RelationsTracker { }
    public class Pawn_AgeTracker { public int AgeBiologicalYears => 0; public DevelopmentalStage CurLifeStageRace => default; }
    public class Pawn_ApparelTracker { public List<Apparel> WornApparel => null; }
    public class Pawn_InventoryTracker { public ThingOwner innerContainer => null; }
    public class Pawn_FilthTracker { public void GainFilth(ThingDef filthDef, IEnumerable<string> sources = null) { } }
    public class Pawn_TrainingTracker { }

    public class MainButtonDef : Def { }
    public class DesignationCategoryDef : Def { }
    public class TaleDef : Def { }
    public class ThoughtDef : Def { }
    public class MentalBreakDef : Def { }
    public class IncidentDef : Def { }
    public class RitualBehaviorDef : Def { }
    public class PreceptDef : Def { }

    public class HealthCardUtility { }
    public class CharacterCardUtility { }

    public static class PawnGenerator { public static Pawn GeneratePawn(PawnGenerationRequest request) => null; }
    public struct PawnGenerationRequest { public PawnKindDef KindDef; public Faction Faction; }

    public class CompUseEffect : ThingComp { public virtual void DoEffect(Pawn usedBy) { } public virtual bool CanBeUsedBy(Pawn p, out string failReason) { failReason = null; return true; } }

    public class Recipe_Surgery { public virtual bool AvailableOnNow(Thing thing, BodyPartRecord part = null) => true; public virtual void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill) { } public virtual string GetLabelWhenUsedOn(Pawn pawn, BodyPartRecord part) => ""; public virtual IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe) => null; }

    public class ITab_Storage : ITab { }

    public static class FilthMaker { public static bool TryMakeFilth(IntVec3 c, Map map, ThingDef filthDef, int count = 1, FilthSourceFlags additionalFlags = FilthSourceFlags.None) => false; public static bool TryMakeFilth(IntVec3 c, Map map, ThingDef filthDef, string source, int count = 1, FilthSourceFlags additionalFlags = FilthSourceFlags.None) => false; }
    public enum FilthSourceFlags { None = 0, Natural = 1, Unnatural = 2, Any = 3 }

    public class CompQuality : ThingComp { public QualityCategory Quality => default; }
    public enum QualityCategory { Awful, Poor, Normal, Good, Excellent, Masterwork, Legendary }

    public static class CompTemperatureRuinable { }
    public static class GenClamor { }

    public class DrugPolicy { }
    public class FoodRestriction { }
    public class ApparelPolicy { }

    public class ThingSetMakerDef : Def { }
    public class HistoryEventDef : Def { }
    public class AbilityDef : Def { }
    public class IssueDef : Def { }
    public class TechLevel { }
    public class Ideo { }

    public class Bill_Production : Bill { public RecipeDef recipe; public RepeatMode repeatMode; public int repeatCount; public int targetCount; public bool suspended; public ThingFilter ingredientFilter; public bool paused; public virtual void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients) { } }
    public class RepeatMode { public static RepeatMode RepeatCount; public static RepeatMode TargetCount; public static RepeatMode Forever; }

    public class CompHasGatherableBodyResource : ThingComp { protected virtual int ResourceAmount => 0; protected virtual ThingDef ResourceDef => null; public virtual bool ActiveAndFull => false; public float Fullness { get; set; } public virtual string SaveKey => ""; protected virtual int GatherResourcesIntervalDays => 1; protected virtual bool Active => true; }
    public class CompMilkable : CompHasGatherableBodyResource { }
    public class CompProperties_Milkable : CompProperties { public int milkAmount; public int milkIntervalDays; public ThingDef milkDef; }

    public class CompBiosculpterPod_Cycle : ThingComp { public virtual void CycleCompleted(Pawn pawn) { } }

    public enum AutofeedMode { Allowed, Never, Urgent }
    public enum HungerCategory { Fed, Hungry, UrgentlyHungry, Starving }
    public static class ChildcareUtility
    {
        public enum BreastfeedFailReason { None, NotLactating, TooFarAway, NotEnoughMilk, Busy, UnwillingToFeed }
        public static bool CanSuckle(Pawn baby, out BreastfeedFailReason reason) { reason = default; return false; }
        public static bool CanBreastfeed(Pawn mother, out BreastfeedFailReason reason) { reason = default; return false; }
        public static AutofeedMode GetAutoFeedMode(Pawn baby) => AutofeedMode.Allowed;
        public static bool CanFeed(Pawn mother, Pawn baby, out BreastfeedFailReason failReason) { failReason = default; return false; }
        public static Pawn FindAutofeedBaby(Pawn mother, AutofeedMode mode) => null;
        public static bool CanFeedBaby(Pawn mother, Pawn baby, out BreastfeedFailReason failReason) { failReason = default; return false; }
        public static bool SuckleFromLactatingPawn(Pawn baby, Pawn mother, Job job = null) => false;
        public static string Translate(string key) => "";
        public static IntVec3 SafePlaceForBaby(Pawn baby, Pawn carrier, Map map) => default;
    }

    public class ChemicalDef : Def { }
    public class WorkGiver_Breastfeed : WorkGiver_Scanner { }
    public class WorkGiver_Milk : WorkGiver_Scanner { protected virtual CompHasGatherableBodyResource GetComp(Pawn pawn) => null; }
    public class WorkGiverDef : Def { public Type giverClass; public WorkTypeDef workType; }
    public class RecordDef : Def { }
    public class WorkGiver_PlayWithBaby : WorkGiver_Scanner { }
    public class JobGiver_GetFood : ThinkNode_JobGiver { }

    public class FloatMenuOptionProvider
    {
        public virtual IEnumerable<FloatMenuOption> GetFloatMenuOptions(FloatMenuContext context) => null;
        public virtual IEnumerable<FloatMenuOption> GetOptionsFor(Thing target, FloatMenuContext context) => null;
        public virtual bool TargetThingValid(Thing target, FloatMenuContext context) => true;
        protected virtual bool CanSelfTarget => false;
        public virtual bool CanTargetDespawned => false;
        protected virtual bool Drafted => false;
        protected virtual bool Undrafted => true;
        protected virtual bool MechanoidCanDo => false;
        protected virtual bool Multiselect => false;
        protected virtual bool RequiresManipulation => false;
    }
    public class FloatMenuContext { public Pawn pawn; public Thing target; public Vector3 clickPos; public bool drafted; }

    public class IngestionOutcomeDoer_GiveHediff : IngestionOutcomeDoer { public HediffDef hediffDef; public float severity; public ChemicalDef toleranceChemical; }

    public class Thought_Memory { public ThoughtDef def; public Pawn pawn; public float moodPowerFactor; public int age; public int forcedStage; public virtual void Init() { } public virtual bool TryMergeWithExistingMemory(out bool showBubble) { showBubble = false; return false; } public virtual bool ShouldDiscard => false; public virtual int CurStageIndex => 0; public virtual float MoodOffset() => 0; }
    public class ThoughtState { public static ThoughtState ActiveDefault => default; public static ThoughtState ActiveAtStage(int stage) => default; public static ThoughtState Inactive => default; public bool Active => false; }
    public class ThoughtWorker { protected virtual ThoughtState CurrentStateInternal(Pawn p) => default; protected virtual ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn) => default; }

    public class StatDrawEntry { public StatDrawEntry(StatCategoryDef category, string label, string valueStringFor, string reportText, int displayPriorityWithinCategory = 0, string overrideReportTitle = null, IEnumerable<Dialog_InfoCard.Hyperlink> hyperlinks = null, bool forceUnfinalizedMode = false) { } }
    public class Dialog_InfoCard : Window { public class Hyperlink { } public override void DoWindowContents(Rect inRect) { } }

    public class PawnDrawParms { }
    public class PawnRenderer { }
    public class CachedTexture { public Texture2D Texture; public CachedTexture(string texPath) { } public static implicit operator Texture2D(CachedTexture ct) => null; }

    public static class HaulAIUtility { public static Job HaulToStorageJob(Pawn p, Thing t) => null; public static bool PawnCanAutomaticallyHaulFast(Pawn p, Thing t, bool forced) => false; public static Job HaulToCellStorageJob(Pawn p, Thing t, IntVec3 storeCell, bool fitInStoreCell) => null; }
    public static class GenStuff { public static IEnumerable<ThingDef> AllowedStuffsFor(BuildableDef bd, TechLevel techLevel = default) => null; }
    public class BuildableDef : Def { }
    public class TechLevelDef : Def { }

    public class HediffComp_Lactating : HediffComp { public float CurMilkAmount => 0; }
    public class HediffComp_Chargeable : HediffComp { public bool GreedyConsume(Pawn pawn, float amount) => false; }
    public class HediffCompProperties_Chargeable : HediffCompProperties { }
    public class Hediff_Pregnant : HediffWithComps { public virtual void DoBirthSpawn(Pawn mother, Pawn father) { } }
    public class Hediff_LaborPushing : HediffWithComps { public virtual void PreRemoved() { } }

    [AttributeUsage(AttributeTargets.Field)] public class MayRequireIdeologyAttribute : Attribute { }

    public class Pawn_PsychicEntropyTracker { }
    public class Pawn_RoyaltyTracker { }
    public class Pawn_StyleTracker { }
}

namespace RimWorld.BaseGen
{
    public class SymbolStack { }
}

namespace RimWorld.Planet
{
    public class WorldObject : Def { }
}

namespace Verse.Grammar
{
    public class GrammarRequest { }
    public static class GrammarResolver { public static string Resolve(string rootKeyword, GrammarRequest request, string debugLabel = null, bool forceLog = false, string extraTags = null, List<string> extraTagsForAI = null, List<string> outTags = null, bool capitalize = true) => ""; }
}

namespace RimWorld.QuestGen
{
    public class QuestNode { }
}
