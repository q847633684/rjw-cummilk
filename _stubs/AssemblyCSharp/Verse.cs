#pragma warning disable CS0626, CS0824, CS0114, CS0108, CS0067, CS0649, CS0169, CS0414, CS0109
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Verse
{
    public interface IExposable { void ExposeData(); }
    public interface ILoadReferenceable { string GetUniqueLoadID(); }
    public interface IThingHolder { }
    public interface IStoreSettingsParent { bool StorageTabVisible { get; } StorageSettings GetStoreSettings(); StorageSettings GetParentStoreSettings(); }
    public interface ISlotGroupParent { }
    public interface IHaulDestination { }

    public class Def : IExposable { public string defName; public string label; public string description; public ushort shortHash; public ushort index; public string modContentPack; public Type modExtensions; public List<DefModExtension> modExtensionsList; public virtual void ExposeData() { } public T GetModExtension<T>() where T : DefModExtension => default; public bool HasModExtension<T>() where T : DefModExtension => false; public override string ToString() => defName ?? ""; public TaggedString LabelCap => default; }
    public class DefModExtension { public virtual IEnumerable<string> ConfigErrors() => null; }

    public class ThingDef : Def { public Type thingClass; public ThingCategory category; public string useHitPoints; public StatModifier statBases; public GraphicData graphicData; public List<ThingDefCountClass> costList; public bool destroyable; public bool rotatable; public bool canOverlapZones; public int stackLimit; public SoundDef soundDrop; public SoundDef soundPickup; public bool drawGUIOverlay; public IngestibleProperties ingestible; public BuildingProperties building; public ApparelProperties apparel; public bool alwaysHaulable; public bool hasInteractionCell; public IntVec3 interactionCellOffset; public float fillPercent; public string comps; public List<CompProperties> comps2; public bool MadeFromStuff; public string stuffCategories; public float BaseMarketValue; public GasType? gasDamage; public StuffProperties stuffProps; public RecipeMakerProperties recipeMaker; public ThingDef butcherProducts; public bool EverHaulable => false; }
    public class BuildingProperties { public int maxItemsInCell; public bool isSittable; }
    public class IngestibleProperties { public ThingDef sourceDef; public FoodTypeFlags foodType; public FoodPreferability preferability; public float CachedNutrition; public float joy; public JoyKindDef joyKind; public List<IngestionOutcomeDoer> outcomeDoers; public bool IsMeal => false; }
    public class ApparelProperties { public List<BodyPartGroupDef> bodyPartGroups; }
    public class StuffProperties { }
    public class RecipeMakerProperties { }
    public class StatModifier { public StatDef stat; public float value; }
    public class ThingDefCountClass { public ThingDef thingDef; public int count; }
    public class GraphicData { public string texPath; public Type graphicClass; public Color color; public Color colorTwo; public Vector2 drawSize; }
    public class JoyKindDef : Def { }
    public enum ThingCategory { Item, Building, Pawn, Plant, Projectile, Filth, Ethereal, Mote, Attachment }
    public enum FoodTypeFlags { None = 0, VegetableOrFruit = 1, Meat = 2, Meal = 4, AnimalProduct = 8, Seed = 16, Liquor = 32, Tree = 64, Plant = 128, Fluid = 256, Kibble = 512, Corpse = 1024, ProcessedFood = 2048 }
    public enum FoodPreferability { Undefined, NeverForNutrition, DesperateOnly, DesperateOnlyForHumanlikes, RawBad, RawTasty, MealAwful, MealSimple, MealFine, MealLavish }
    public enum GasType { BlindSmoke, ToxGas, RotStink, DeadlifeDust }

    public class Thing : IExposable, ILoadReferenceable
    {
        public ThingDef def;
        public int stackCount;
        public int HitPoints;
        public Map Map => null;
        public Map MapHeld => null;
        public IntVec3 Position => default;
        public IntVec3 PositionHeld => default;
        public Faction Faction => null;
        public string Label => "";
        public string LabelCap => "";
        public string LabelNoCount => "";
        public string LabelShort => "";
        public Graphic Graphic => null;
        public bool Spawned => false;
        public bool Destroyed => false;
        public ThingDef Stuff => null;
        public int thingIDNumber;
        public virtual void ExposeData() { }
        public virtual string GetUniqueLoadID() => "";
        public virtual void Tick() { }
        public virtual void TickRare() { }
        public virtual void TickLong() { }
        public virtual void Destroy(DestroyMode mode = DestroyMode.Vanish) { }
        public virtual void SpawnSetup(Map map, bool respawningAfterLoad) { }
        public virtual void DeSpawn(DestroyMode mode = DestroyMode.Vanish) { }
        public virtual IEnumerable<Gizmo> GetGizmos() => null;
        public virtual string GetInspectString() => "";
        public T TryGetComp<T>() where T : ThingComp => default;
        public List<ThingComp> AllComps => null;
        public virtual void PostMake() { }
        public virtual void PreTraded(TradeAction action, Pawn playerNegotiator, ITrader trader) { }
        public virtual bool BlocksPawn(Pawn p) => false;
        public ThingOwner holdingOwner;
        public Region GetRegion(RegionType type = RegionType.Set_All) => null;
        public IntVec3 InteractionCell => default;
    }
    public enum DestroyMode { Vanish, WillReplace, KillFinalize, KillFinalizeLeavingsOnly, Deconstruct, FailConstruction, Cancel, Refund, QuestLogic }
    public enum TradeAction { None, PlayerBuys, PlayerSells }
    public interface ITrader { }
    public class Region { }
    public enum RegionType { Normal = 1, ImpassableFreeform = 2, Set_All = 3, Portal = 4, Set_Passable = 5 }
    public class Faction { public string Name => ""; public FactionDef def; public bool IsPlayer => false; public bool HostileTo(Faction other) => false; }
    public class FactionDef : Def { }

    public class ThingWithComps : Thing
    {
        public List<ThingComp> comps;
        public override void SpawnSetup(Map map, bool respawningAfterLoad) { }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish) { }
        public override void ExposeData() { }
        public override void Tick() { }
        public override void TickRare() { }
        public virtual void InitializeComps() { }
        public ThingComp GetComp(Type type) => null;
        public new T TryGetComp<T>() where T : ThingComp => default;
        public BillStack BillStack => null;
    }
    public class BillStack { public List<Bill> Bills => null; }

    public class Building : ThingWithComps
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad) { }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish) { }
    }

    public class Pawn : ThingWithComps
    {
        public Pawn_HealthTracker health;
        public Pawn_NeedsTracker needs;
        public Pawn_JobTracker jobs;
        public AI.Pawn_MindState mindState;
        public Pawn_StoryTracker story;
        public Pawn_GeneTracker genes;
        public Pawn_RelationsTracker relations;
        public Pawn_AgeTracker ageTracker;
        public Pawn_ApparelTracker apparel;
        public Pawn_InventoryTracker inventory;
        public Pawn_FilthTracker filth;
        public Pawn_TrainingTracker training;
        public Pawn_AbilityTracker abilities;
        public Pawn_DraftController drafter;
        public Pawn_EquipmentTracker equipment;
        public Pawn_CarryTracker carryTracker;
        public RaceProperties RaceProps => null;
        public Name Name { get; set; }
        public Gender gender;
        public bool Dead => false;
        public bool Downed => false;
        public bool IsFreeColonist => false;
        public bool IsColonist => false;
        public bool IsPrisoner => false;
        public bool IsSlave => false;
        public bool IsColonistPlayerControlled => false;
        public bool Awake() => false;
        public bool Drafted => false;
        public BodyDef BodyDef => null;
        public float BodySize => 1f;
        public float GetStatValue(StatDef stat, bool applyPostProcess = true, int cacheStaleAfterTicks = -1) => 0f;
        public virtual void Kill(DamageInfo? dinfo, Hediff exactCulprit = null) { }
        public Corpse Corpse => null;
        public PawnKindDef kindDef;
        public IntVec3 PositionHeld => default;
        public DevelopmentalStage DevelopmentalStage => default;
        public LifeStageDef CurLifeStage => null;
        public bool InBed() => false;
        public Building_Bed CurrentBed() => null;
        public bool Spawned => false;
    }
    public class Building_Bed : Building { public List<Pawn> OwnersForReading => null; public bool ForPrisoners { get; set; } public bool Medical { get; set; } }
    public class Corpse : ThingWithComps { public Pawn InnerPawn => null; }
    public enum DevelopmentalStage { Baby = 1, Child = 2, Adult = 4 }
    public class LifeStageDef : Def { }
    public class Name { public string ToStringFull => ""; public string ToStringShort => ""; }
    public enum Gender { None, Male, Female }
    public class RaceProperties { public bool Humanlike; public bool Animal; public bool IsMechanoid; public FleshTypeDef FleshType; public ThingDef corpseDef; public BodyDef body; public float baseBodySize; public float baseHealthScale; public List<AnimalBiomeRecord> wildBiomes; public bool IsFlesh => false; public FoodTypeFlags foodType; public float lifeExpectancy; public float gestationPeriodDays; public bool hasGenders; public string meatDef; public string leatherDef; public Intelligence intelligence; public virtual IEnumerable<RimWorld.StatDrawEntry> SpecialDisplayStats(ThingDef parentDef, RimWorld.StatRequest req) => null; }
    public enum Intelligence { Animal, ToolUser, Humanlike }
    public class FleshTypeDef : Def { }
    public class AnimalBiomeRecord { }
    public class BodyDef : Def { public List<BodyPartRecord> AllParts => null; public BodyPartRecord corePart; }
    public class PawnKindDef : Def { public ThingDef race; public float combatPower; }

    public class Pawn_HealthTracker { public HediffSet hediffSet; public HealthState State => default; public bool InPainShock => false; public void AddHediff(Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageResult result = null) { } public void RemoveHediff(Hediff hediff) { } public float capacities => 0; public PawnCapacitiesHandler capacitiesHandler; }
    public class PawnCapacitiesHandler { public float GetLevel(PawnCapacityDef cap) => 0f; }
    public class PawnCapacityDef : Def { }
    public enum HealthState { Mobile, Down, Dead }
    public class DamageResult { }
    public class HediffSet { public List<Hediff> hediffs; public bool HasHediff(HediffDef def, bool mustBeVisible = false) => false; public Hediff GetFirstHediffOfDef(HediffDef def, bool mustBeVisible = false) => null; public float GetFirstHediffOfDefSeverity(HediffDef def) => 0f; public IEnumerable<Hediff> GetHediffs<T>() => null; public void DirtyCache() { } public List<BodyPartRecord> GetNotMissingParts(BodyPartHeight height = BodyPartHeight.Undefined, BodyPartDepth depth = BodyPartDepth.Undefined, BodyPartTagDef tag = null, BodyPartRecord partParent = null) => null; public BodyPartRecord GetBrain() => null; }
    public class Pawn_NeedsTracker { public NeedDef AllNeeds => null; public T TryGetNeed<T>() where T : class => default; public void SetInitialLevels() { } }
    public class NeedDef : Def { }
    public class Pawn_StoryTracker { public List<Trait> traits => null; public Backstory childhood; public Backstory adulthood; public BodyTypeDef bodyType; public HeadTypeDef headType; public HairDef hairDef; public Color hairColor; public Color skinColor; public float melanin; public TraitSet traitSet; }
    public class TraitSet { public bool HasTrait(TraitDef tDef) => false; public Trait GetTrait(TraitDef tDef) => null; }
    public class Backstory { }
    public class BodyTypeDef : Def { }
    public class HeadTypeDef : Def { }
    public class HairDef : Def { }
    public class Trait { public TraitDef def; public int Degree => 0; }
    public class TraitDef : Def { }
    public class Pawn_GeneTracker { public bool HasActiveGene(GeneDef geneDef) => false; public Gene GetGene(GeneDef geneDef) => null; public Gene GetFirstGeneOfType<T>() where T : Gene => default; public List<Gene> GenesListForReading => null; }
    public class Pawn_RelationsTracker { }
    public class Pawn_AgeTracker { public int AgeBiologicalYears => 0; public long ageBiologicalTicks; public float AgeChronologicalYearsFloat => 0; }
    public class Pawn_ApparelTracker { public List<Apparel> WornApparel => null; }
    public class Pawn_InventoryTracker { public ThingOwner innerContainer => null; }
    public class Pawn_FilthTracker { }
    public class Pawn_TrainingTracker { }
    public class Pawn_AbilityTracker { }
    public class Pawn_DraftController { public bool Drafted { get; set; } }
    public class Pawn_EquipmentTracker { }
    public class Pawn_CarryTracker { public Thing CarriedThing => null; public int TryStartCarry(Thing item, int count = -1, bool reserve = true) => 0; public bool TryDropCarriedThing(IntVec3 dropLoc, ThingPlaceMode mode, out Thing resultingThing, Action<Thing, int> placedAction = null) { resultingThing = null; return false; } }
    public class Apparel : ThingWithComps { public Pawn Wearer => null; }
    public class Pawn_JobTracker { public AI.Job curJob; public AI.JobDriver curDriver; public void StartJob(AI.Job newJob, AI.JobCondition condition = AI.JobCondition.None, AI.ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true, ThinkTreeDef thinkTree = null, AI.JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false) { } public void EndCurrentJob(AI.JobCondition condition, bool startNewJob = true, bool canReturnToPool = true) { } public bool IsCurrentJobPlayerForced() => false; }

    public class GeneDef : Def { public Type geneClass; public string iconPath; public float displayOrder; public GeneCategoryDef displayCategory; public List<GeneDef> prerequisiteGenes; public float biostatCpx; public float biostatMet; public int biostatArc; }
    public class GeneCategoryDef : Def { }
    public class Gene : IExposable { public GeneDef def; public Pawn pawn; public bool Active => true; public virtual void ExposeData() { } public virtual void PostAdd() { } public virtual void PostRemove() { } public virtual void Tick() { } public virtual float Value { get; set; } public virtual void Reset() { } }

    public class JobDef : Def { public Type driverClass; public bool casualInterruptible; public bool neverFleeFromEnemies; public bool playerInterruptible; public bool suspendable; public bool allowOpportunisticPrefix; public string reportString; }
    public class RecipeDef : Def { public Type workerClass; public Type workerCounterClass; public ThingDef unfinishedThingDef; public List<IngredientCount> ingredients; public ThingFilter fixedIngredientFilter; public List<ThingDefCountClass> products; public float workAmount; public List<StatModifier> workSpeedStat; public bool surgerySuccessChanceFactor; public List<BodyPartDef> appliedOnFixedBodyParts; }
    public class IngredientCount { public ThingFilter filter; public float count; }
    public class ThinkTreeDef : Def { }

    public class Map
    {
        public int uniqueID;
        public int Tile => 0;
        public IntVec3 Size => default;
        public MapInfo info;
        public ThingOwner spawnedThings;
        public ListerThings listerThings;
        public ListerBuildings listerBuildings;
        public MapPawns mapPawns;
        public ResourceCounter resourceCounter;
        public RegionGrid regionGrid;
        public ZoneManager zoneManager;
        public AreaManager areaManager;
        public WeatherManager weatherManager;
        public ReservationManager reservationManager;
        public DesignationManager designationManager;
        public GameConditionManager gameConditionManager;
        public bool IsPlayerHome => false;
        public SlotGroupManager haulDestinationManager;
    }
    public class MapInfo { }
    public class ListerThings { public List<Thing> ThingsInGroup(ThingRequestGroup r) => null; public List<Thing> ThingsOfDef(ThingDef d) => null; public List<Thing> AllThings => null; }
    public class ListerBuildings { public List<Building> allBuildingsColonist => null; public List<Building> allBuildingsColonistCombatTargets => null; }
    public class MapPawns { public List<Pawn> AllPawns => null; public List<Pawn> AllPawnsSpawned => null; public List<Pawn> FreeColonists => null; public List<Pawn> FreeColonistsAndPrisoners => null; public List<Pawn> PrisonersOfColony => null; public List<Pawn> SlavesOfColonySpawned => null; public List<Pawn> SpawnedPawnsInFaction(Faction f) => null; }
    public class ResourceCounter { }
    public class RegionGrid { }
    public class ZoneManager { }
    public class AreaManager { }
    public class WeatherManager { }
    public class ReservationManager { public bool CanReserve(Pawn p, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false) => false; public bool Reserve(Pawn p, AI.Job job, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool errorOnFailed = true) => false; }
    public class ReservationLayerDef : Def { }
    public class DesignationManager { public Designation DesignationOn(Thing t, DesignationDef d = null) => null; public void RemoveDesignation(Designation d) { } public void AddDesignation(Designation d) { } }
    public class Designation { public DesignationDef def; public LocalTargetInfo target; public Designation() { } public Designation(LocalTargetInfo target, DesignationDef def) { } }
    public class DesignationDef : Def { }
    public class GameConditionManager { }
    public class SlotGroupManager { }
    public enum ThingRequestGroup { Everything, HaulableAlways, HaulableEver, FoodSource, FoodSourceNotPlantOrTree, Pawn, BuildingArtificial, Bed, ThingHolder, Corpse, Blueprint, Chunk, StoneChunk, Filth, HaulableAlwaysMap, Refuelable }

    public class MapComponent : IExposable { public Map map; public MapComponent(Map map) { this.map = map; } public virtual void ExposeData() { } public virtual void MapComponentTick() { } public virtual void MapComponentUpdate() { } public virtual void MapComponentOnGUI() { } public virtual void FinalizeInit() { } }
    public class GameComponent : IExposable { public GameComponent(Game game) { } public virtual void ExposeData() { } public virtual void GameComponentTick() { } public virtual void GameComponentUpdate() { } public virtual void GameComponentOnGUI() { } public virtual void FinalizeInit() { } public virtual void StartedNewGame() { } public virtual void LoadedGame() { } }
    public class Game { public TickManager tickManager; public static void InitNewGame() { } public static void LoadGame() { } }
    public class TickManager { public int TicksGame => 0; public int TicksAbs => 0; }
    public class WorldComponent : IExposable { public virtual void ExposeData() { } }

    public class Hediff : IExposable, ILoadReferenceable
    {
        public HediffDef def;
        public Pawn pawn;
        public BodyPartRecord Part { get; set; }
        public float Severity { get; set; }
        public int ageTicks;
        public virtual string Label => "";
        public virtual string LabelCap => "";
        public virtual bool ShouldRemove => false;
        public virtual bool Visible => true;
        public virtual float PainOffset => 0;
        public virtual float PainFactor => 1;
        public virtual void ExposeData() { }
        public virtual string GetUniqueLoadID() => "";
        public virtual void PostTick() { }
        public virtual void PostAdd(DamageInfo? dinfo) { }
        public virtual void PostRemoved() { }
        public virtual void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null) { }
        public virtual string TipStringExtra => "";
        public virtual string TipString => "";
        public virtual TextureAndColor StateIcon => default;
        public virtual HediffStage CurStage => null;
        public virtual int CurStageIndex => 0;
        public virtual bool TryMergeWith(Hediff other) => false;
        public virtual void Tick() { }
    }
    public struct TextureAndColor { public Texture2D Texture; public Color Color; }
    public class HediffWithComps : Hediff { public List<HediffComp> comps; public T TryGetComp<T>() where T : HediffComp => default; public override void PostTick() { } public override void ExposeData() { } }
    public class HediffDef : Def { public Type hediffClass; public float initialSeverity; public float maxSeverity; public float lethalSeverity; public bool isBad; public bool makesAlert; public bool tendable; public string defaultLabelColor; public List<HediffCompProperties> comps; public List<HediffStage> stages; public bool IsAddiction => false; public bool priceImpact; }
    public class HediffComp : IExposable { public HediffCompProperties props; public HediffWithComps parent; public Pawn Pawn => null; public Hediff ParentHediff => null; public virtual void CompExposeData() { } public virtual void CompPostTick(ref float severityAdjustment) { } public virtual void CompPostTickInterval(ref float severityAdjustment, int interval) { } public virtual void CompPostPostAdd(DamageInfo? dinfo) { } public virtual void CompPostPostRemoved() { } public virtual string CompLabelInBracketsExtra => null; public virtual string CompTipStringExtra => null; public virtual void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null) { } public virtual void ExposeData() { } public virtual string CompDebugString() => ""; }
    public class HediffComp_SeverityModifierBase : HediffComp { public virtual float SeverityChangePerDay() => 0; }
    public class HediffCompProperties { public Type compClass; }
    public class HediffStage { public string label; public float minSeverity; public float painOffset; public float painFactor; public List<StatModifier> statOffsets; public List<StatModifier> statFactors; public float partEfficiencyOffset; public bool becomeVisible; }

    public class CompProperties { public Type compClass; public virtual IEnumerable<string> ConfigErrors(ThingDef parentDef) => null; }
    public class ThingComp : IExposable { public ThingWithComps parent; public CompProperties props; public virtual void PostExposeData() { } public virtual void PostSpawnSetup(bool respawningAfterLoad) { } public virtual void PostDeSpawn(Map map) { } public virtual void CompTick() { } public virtual void CompTickRare() { } public virtual void CompTickLong() { } public virtual void PostDestroy(DestroyMode mode, Map previousMap) { } public virtual IEnumerable<Gizmo> CompGetGizmosExtra() => null; public virtual string CompInspectStringExtra() => null; public virtual void PostDraw() { } public virtual void ReceiveCompSignal(string signal) { } public virtual void Initialize(CompProperties props) { } public virtual void ExposeData() { } public virtual void Notify_SignalReceived(Signal signal) { } public virtual string TransformLabel(string label) => label; public virtual bool AllowStackWith(Thing other) => true; public virtual IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn) => null; public virtual void Notify_Equipped(Pawn pawn) { } public virtual void Notify_Unequipped(Pawn pawn) { } public virtual void PostIngested(Pawn ingester) { } }
    public class Signal { public string tag; }

    public class ModSettings : IExposable { public virtual void ExposeData() { } }
    public class Mod { public ModContentPack Content => null; public ModSettings settings; public Mod(ModContentPack content) { } public virtual void DoSettingsWindowContents(Rect inRect) { } public virtual string SettingsCategory() => ""; public virtual void WriteSettings() { } public T GetSettings<T>() where T : ModSettings, new() => default; }
    public class ModContentPack { public string Name => ""; public string PackageId => ""; public string RootDir => ""; }

    public class Window
    {
        public virtual Vector2 InitialSize => new Vector2(500, 500);
        public bool forcePause;
        public bool absorbInputAroundWindow;
        public bool closeOnAccept;
        public bool closeOnCancel;
        public bool closeOnClickedOutside;
        public bool doCloseButton;
        public bool doCloseX;
        public bool draggable;
        public bool resizeable;
        public SoundDef soundAppear;
        public SoundDef soundClose;
        public SoundDef soundAmbient;
        public float layer;
        public Rect windowRect;
        public virtual void DoWindowContents(Rect inRect) { }
        public virtual void PreOpen() { }
        public virtual void PostOpen() { }
        public virtual void PreClose() { }
        public virtual void PostClose() { }
        public virtual void WindowUpdate() { }
        public virtual void WindowOnGUI() { }
        public virtual void OnAcceptKeyPressed() { }
        public virtual void OnCancelKeyPressed() { }
        public void Close(bool doCloseSound = true) { }
        public bool IsOpen => false;
    }

    public class IngestionOutcomeDoer { public virtual void DoIngestionOutcome(Pawn pawn, Thing ingested, int count) { } protected virtual void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int count) { } }
    public class SpecialThingFilterWorker { public virtual bool Matches(Thing t) => false; public virtual bool AlwaysMatches(ThingDef def) => false; public virtual bool CanEverMatch(ThingDef def) => false; }
    public class StorageSettings : IExposable { public ThingFilter filter; public void ExposeData() { } public void CopyFrom(StorageSettings other) { } public StoragePriority Priority { get; set; } }
    public enum StoragePriority { Unstored, Low, Normal, Preferred, Important, Critical }

    public class BodyPartRecord { public BodyPartDef def; public List<BodyPartRecord> parts; public BodyPartRecord parent; public bool IsInGroup(BodyPartGroupDef groupDef) => false; public string Label => ""; public string LabelCap => ""; public BodyPartHeight height; public BodyPartDepth depth; }
    public class BodyPartDef : Def { public List<BodyPartTagDef> tags; }
    public class BodyPartGroupDef : Def { }
    public class BodyPartTagDef : Def { }
    public enum BodyPartHeight { Undefined, Bottom, Middle, Top }
    public enum BodyPartDepth { Undefined, Outside, Inside }

    public struct IntVec3 { public int x, y, z; public IntVec3(int x, int y, int z) { this.x = x; this.y = y; this.z = z; } public static IntVec3 Invalid => default; public bool IsValid => false; public bool InBounds(Map map) => false; public Thing GetFirstThing(Map map, ThingDef def) => null; public Room GetRoom(Map map) => null; public float DistanceTo(IntVec3 other) => 0; public int DistanceToSquared(IntVec3 other) => 0; public List<Thing> GetThingList(Map map) => null; public Building GetEdifice(Map map) => null; public bool Standable(Map map) => false; public bool Walkable(Map map) => false; public override string ToString() => ""; public static IntVec3 Zero => default; public static IntVec3 North => default; public static IntVec3 South => default; public static IntVec3 East => default; public static IntVec3 West => default; public static IntVec3 operator +(IntVec3 a, IntVec3 b) => default; public static IntVec3 operator -(IntVec3 a, IntVec3 b) => default; public Vector3 ToVector3() => default; public Vector3 ToVector3Shifted() => default; public Vector3 ToVector3ShiftedWithAltitude(AltitudeLayer alt) => default; }
    public enum AltitudeLayer { Terrain, Floor, FloorEmplacement, Zone, Conduits, Blueprint, LowPlant, MoteLow, Item, ItemImportant, Haulable, BuildingOnTop, Building, PawnUnused, Pawn, PawnState, MoteOverhead, FlyingItem, Projectile, Skyfaller, Weather, FogOfWar, WorldClimateCells, WorldDataOverlay, MetaOverlays }
    public class Room { }
    public struct CellRect { public int minX, maxX, minZ, maxZ; public CellRect(int minX, int minZ, int w, int h) { this.minX = minX; this.maxX = minX + w - 1; this.minZ = minZ; maxZ = minZ + h - 1; } public int Width => maxX - minX + 1; public int Height => maxZ - minZ + 1; public IntVec3 CenterCell => default; public CellRect ContractedBy(int i) => default; public CellRect ExpandedBy(int i) => default; public bool Contains(IntVec3 c) => false; public IEnumerable<IntVec3> Cells => null; }
    public struct LocalTargetInfo { public Thing Thing; public IntVec3 Cell; public bool HasThing => Thing != null; public bool IsValid => true; public Map Map => null; public Pawn Pawn => null; public static implicit operator LocalTargetInfo(Thing t) => default; public static implicit operator LocalTargetInfo(IntVec3 c) => default; }
    public struct TargetInfo { public Thing Thing; public IntVec3 Cell; public Map Map; public static implicit operator TargetInfo(Thing t) => default; }
    public struct GlobalTargetInfo { }

    public class StatDef : Def { public StatCategoryDef category; public float defaultBaseValue; public bool neverDisabled; }
    public class StatCategoryDef : Def { }
    public class SoundDef : Def { public void PlayOneShot(Sound.SoundInfo info) { } public void PlayOneShotOnCamera(Map map = null) { } }
    public class EffecterDef : Def { public Effecter Spawn() => null; public Effecter Spawn(Thing target, Map map, float scale = 1f) => null; public Effecter Spawn(IntVec3 target, Map map, float scale = 1f) => null; }
    public class Effecter { public void EffectTick(TargetInfo a, TargetInfo b) { } public void Trigger(TargetInfo a, TargetInfo b, int overrideSpawnTick = -1) { } public void Cleanup() { } }
    public class FleckDef : Def { }
    public class DamageDef : Def { }
    public class RulePackDef : Def { }
    public class ResearchProjectDef : Def { }
    public class MentalStateDef : Def { }
    public class WorkTypeDef : Def { }
    public class LetterDef : Def { }
    public class ThingCategoryDef : Def { }

    public struct DamageInfo { public DamageDef Def => null; public float Amount => 0; public Pawn Instigator => null; public Thing IntendedTarget => null; }

    public struct TaggedString { public TaggedString(string s) { } public string RawText => ""; public string Resolve() => ""; public static implicit operator TaggedString(string s) => default; public static implicit operator string(TaggedString s) => ""; public override string ToString() => ""; public static TaggedString operator +(TaggedString a, string b) => default; public static TaggedString operator +(string a, TaggedString b) => default; }
    public struct NamedArgument { public static implicit operator NamedArgument(string s) => default; public static implicit operator NamedArgument(int i) => default; public static implicit operator NamedArgument(float f) => default; public static implicit operator NamedArgument(Thing t) => default; }

    public class Graphic { public Material MatSingle => null; public Material MatAt(Rot4 rot, Thing thing = null) => null; public void Draw(Vector3 loc, Rot4 rot, Thing thing, float extraRotation = 0) { } public Color Color => default; }
    public class GraphicDatabase { public static Graphic Get(Type type, string path, Shader shader, Vector2 drawSize, Color color) => null; public static Graphic Get<T>(string path, Shader shader, Vector2 drawSize, Color color) where T : Graphic => null; }
    public struct Rot4 { public static Rot4 North => default; public static Rot4 South => default; public static Rot4 East => default; public static Rot4 West => default; public int AsInt => 0; public bool IsHorizontal => false; public Rot4 Opposite => default; }

    public class Gizmo { }
    public class Command : Gizmo { public string defaultLabel; public string defaultDesc; public Texture2D icon; public SoundDef activateSound; }
    public class Command_Action : Command { public Action action; }
    public class Command_Toggle : Command { public Func<bool> isActive; public Action toggleAction; }

    public class ThingFilter : IExposable { public void ExposeData() { } public bool Allows(ThingDef def) => false; public bool Allows(Thing t) => false; public void SetAllow(ThingDef def, bool allow) { } public void SetAllow(SpecialThingFilterDef sfDef, bool allow) { } public void SetAllowAll(ThingFilter parentFilter, bool includeNonStorable = false) { } public void SetDisallowAll(IEnumerable<ThingDef> exceptedDefs = null, IEnumerable<SpecialThingFilterDef> exceptedFilters = null) { } public string Summary => ""; public ThingDef AnyAllowedDef => null; public IEnumerable<ThingDef> AllowedThingDefs => null; }
    public class SpecialThingFilterDef : Def { }
    public class ThingOwner : IExposable, IList<Thing> { public int Count => 0; public void ExposeData() { } public bool TryAdd(Thing item, bool canMergeWithExistingStacks = true) => false; public bool TryDrop(Thing thing, IntVec3 dropLoc, Map map, ThingPlaceMode mode, int count, out Thing resultingThing, Action<Thing, int> placedAction = null, Predicate<IntVec3> nearPlaceValidator = null) { resultingThing = null; return false; } public bool TryDrop(Thing thing, IntVec3 dropLoc, Map map, ThingPlaceMode mode, out Thing lastResultingThing, Action<Thing, int> placedAction = null, Predicate<IntVec3> nearPlaceValidator = null) { lastResultingThing = null; return false; } public bool Contains(Thing item) => false; public Thing this[int index] { get => null; set { } } public int IndexOf(Thing item) => -1; public void Insert(int index, Thing item) { } public void RemoveAt(int index) { } public void Add(Thing item) { } public void Clear() { } public void CopyTo(Thing[] array, int arrayIndex) { } public bool Remove(Thing item) => false; public bool IsReadOnly => false; public IEnumerator<Thing> GetEnumerator() => null; System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null; public Thing InnerListForReading => null; public int TotalStackCount => 0; }
    public enum ThingPlaceMode { Near, Direct }

    public class Listing { }
    public class Listing_Standard : Listing
    {
        public Listing_Standard() { }
        public void Begin(Rect rect) { }
        public void End() { }
        public void Label(string label, float maxHeight = -1, string tooltip = null) { }
        public void Label(TaggedString label, float maxHeight = -1, string tooltip = null) { }
        public bool ButtonText(string label, string tooltip = null) => false;
        public bool ButtonTextLabeled(string label, string buttonLabel) => false;
        public void CheckboxLabeled(string label, ref bool checkOn, string tooltip = null) { }
        public string TextEntry(string text, int lineCount = 1) => "";
        public string TextEntryLabeled(string label, string text, int lineCount = 1) => "";
        public void TextFieldNumericLabeled<T>(string label, ref T val, ref string buffer, float min = 0, float max = float.MaxValue) where T : struct { }
        public float Slider(float val, float min, float max) => 0;
        public void Gap(float gapHeight = 12) { }
        public void GapLine(float gapHeight = 12) { }
        public float CurHeight => 0;
        public Rect GetRect(float height) => default;
        public bool RadioButton(string label, bool active, float tabIn = 0, string tooltip = null, float? tooltipDelay = null) => false;
        public float ColumnWidth { get; set; }
    }

    public static class Log { public static void Message(string text) { } public static void Warning(string text) { } public static void Error(string text) { } public static void ErrorOnce(string text, int key) { } }
    public static class Scribe_Values { public static void Look<T>(ref T value, string label, T defaultValue = default, bool forceSave = false) { } }
    public static class Scribe_References { public static void Look<T>(ref T refee, string label, bool saveDestroyedThings = false) where T : ILoadReferenceable { } }
    public static class Scribe_Defs { public static void Look<T>(ref T value, string label) where T : Def { } }
    public static class Scribe_Collections { public static void Look<T>(ref List<T> list, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs) { } public static void Look<K, V>(ref Dictionary<K, V> dict, string label, LookMode keyLookMode = LookMode.Undefined, LookMode valueLookMode = LookMode.Undefined) { } }
    public static class Scribe_Deep { public static void Look<T>(ref T target, string label, params object[] ctorArgs) where T : IExposable { } }
    public enum LookMode { Undefined, Value, Deep, Reference, Def, BodyPart, LocalTargetInfo, TargetInfo, GlobalTargetInfo }

    public static class Gen { public static int HashCombine(int seed, object obj) => 0; public static int HashCombineStruct<T>(int seed, T obj) where T : struct => 0; public static int HashCombineInt(int seed, int value) => 0; public static bool IsHashIntervalTick(this Thing t, int interval) => false; public static void DestroyOrPassToWorld(this Thing t, DestroyMode mode = DestroyMode.Vanish) { } }
    public static class GenMath { public static float LerpDouble(float inFrom, float inTo, float outFrom, float outTo, float x) => 0; public static float RoundedHundredth(float f) => 0; public static int RoundRandom(float f) => 0; public static float Factorial(int x) => 0; public static float SmootherStep(float edge0, float edge1, float x) => 0; public static float InverseLerp(float a, float b, float value) => 0; }
    public static class GenDraw { public static void DrawLineBetween(Vector3 a, Vector3 b) { } public static void DrawLineBetween(Vector3 a, Vector3 b, Color c) { } public static void DrawFieldEdges(List<IntVec3> cells, Color color = default) { } }
    public static class GenText { public static string CapitalizeFirst(this string s) => s; public static string ToStringPercent(this float f) => ""; public static string Truncate(this string s, float maxWidth, Dictionary<string, string> cache = null) => s; public static string ToCommaList(this IEnumerable<string> items, bool useAnd = false) => ""; public static TaggedString Colorize(this TaggedString s, Color c) => default; public static string Colorize(this string s, Color c) => s; public static string StripTags(this string s) => s; }
    public static class GenThing { public static bool TryDropAndSetForbidden(Thing th, IntVec3 pos, Map map, ThingPlaceMode mode, out Thing resultingThing, bool forbidden) { resultingThing = null; return false; } public static int StackCountForThing(ThingDef def) => 1; }
    public static class GenAdj { public static List<IntVec3> CellsAdjacent8Way(Thing t) => null; public static List<IntVec3> CellsOccupiedBy(Thing t) => null; }
    public static class GenGrid { }
    public static class GenSpawn { public static Thing Spawn(ThingDef def, IntVec3 loc, Map map, WipeMode wipeMode = WipeMode.Vanish) => null; public static Thing Spawn(Thing newThing, IntVec3 loc, Map map, Rot4 rot = default, WipeMode wipeMode = WipeMode.Vanish, bool respawningAfterLoad = false, bool forbidLeavings = false) => null; }
    public enum WipeMode { Vanish, VanishOrMoveAside, FullRefund }
    public static class GenClosest { public static Thing ClosestThingReachable(IntVec3 root, Map map, ThingRequest req, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0, int searchRegionsMax = -1, bool forceAllowGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false) => null; }
    public struct ThingRequest { public static ThingRequest ForDef(ThingDef def) => default; public static ThingRequest ForGroup(ThingRequestGroup group) => default; }
    public enum PathEndMode { None, OnCell, Touch, ClosestTouch, InteractionCell }
    public struct TraverseParms { public static TraverseParms For(Pawn pawn, Danger maxDanger = Danger.Deadly, TraverseMode mode = TraverseMode.ByPawn, bool canBashDoors = false, bool alwaysUseAvoidGrid = false, bool canBashFences = false) => default; }
    public enum Danger { None, Some, Deadly }
    public enum TraverseMode { ByPawn, PassDoors, NoPassClosedDoors, NoPassClosedDoorsOrWater, PassAllDestroyableThings, PassAllDestroyableThingsNotWater }
    public static class GenRadial { public static IEnumerable<IntVec3> RadialCellsAround(IntVec3 center, float radius, bool useCenter) => null; }
    public static class GenTypes { }
    public static class GenLabel { public static string ThingLabel(ThingDef entDef, ThingDef stuffDef, int stackCount = 1) => ""; }
    public static class GenCollection { public static T RandomElement<T>(this IEnumerable<T> source) => default; public static bool TryRandomElement<T>(this IEnumerable<T> source, out T result) { result = default; return false; } public static T RandomElementByWeight<T>(this IEnumerable<T> source, Func<T, float> weightSelector) => default; public static IEnumerable<T> InRandomOrder<T>(this IEnumerable<T> source, IList<T> workingList = null) => null; }
    public static class GenStep { }

    [AttributeUsage(AttributeTargets.Class)] public class StaticConstructorOnStartupAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class DefOfAttribute : Attribute { }

    public class Sustainer { public void End() { } }
    public class TabRecord { public TabRecord(string label, Action clickedAction, bool selected) { } }
    public class QuickSearchWidget { public void OnGUI(Rect rect, Action<string> onFilterChanged = null) { } public string filter => ""; }
    public class Filth : Thing { }
    public class DefGenerator { public static void GenerateImpliedDefs_PreResolve() { } public static void GenerateImpliedDefs_PostResolve() { } }
    public class DefInjectionPackage { public class DefInjection { public string path; public string injection; public bool isPlaceholder; public string fullListInjection; public string normalizedPath; public bool fileSource; } public List<DefInjection> injections; }

    public enum TextureFormat { RGBA32, ARGB32, RGB24, Alpha8 }

    public static class Current { public static Game Game => null; public static Map Map => null; }
    public static class Find { public static Game Game => null; public static World World => null; public static Storyteller Storyteller => null; public static TickManager TickManager => null; public static WindowStack WindowStack => null; public static LetterStack LetterStack => null; public static PlaySettings PlaySettings => null; public static CameraDriver CameraDriver => null; public static Selector Selector => null; public static CurrentMap CurrentMap => null; }
    public class Storyteller { }
    public class World { }
    public class WindowStack { public void Add(Window window) { } public bool IsOpen(Type type) => false; public bool IsOpen<T>() where T : Window => false; public T WindowOfType<T>() where T : Window => null; public void TryRemove(Type type, bool doCloseSound = true) { } }
    public class LetterStack { public void ReceiveLetter(string label, string text, LetterDef textLetterDef, LookTargets lookTargets = null, Faction relatedFaction = null, Quest relatedQuest = null, List<ThingDef> hyperlinkThingDefs = null, string debugInfo = null) { } }
    public class LookTargets { public LookTargets() { } public LookTargets(Thing t) { } public LookTargets(IntVec3 c, Map map) { } }
    public class Quest { }
    public class PlaySettings { }
    public class CameraDriver { }
    public class Selector { public List<object> SelectedObjects => null; public Thing SingleSelectedThing => null; }
    public class CurrentMap { }

    public static class LoadedModManager { public static T GetMod<T>() where T : Mod => null; public static ModContentPack RunningModsListForReading => null; }
    public static class ContentFinder<T> where T : class { public static T Get(string itemPath, bool reportFailure = true) => default; }
    public static class DefDatabase<T> where T : Def, new() { public static T GetNamed(string defName, bool errorOnFail = true) => null; public static IEnumerable<T> AllDefs => null; public static IEnumerable<T> AllDefsListForReading => null; }

    public class ThingMaker { public static Thing MakeThing(ThingDef def, ThingDef stuff = null) => null; }
    public static class HediffMaker { public static Hediff MakeHediff(HediffDef def, Pawn pawn, BodyPartRecord part = null) => null; public static T MakeHediff<T>(HediffDef def, Pawn pawn, BodyPartRecord part = null) where T : Hediff => null; }

    public static class Rand { public static float Value => 0; public static bool Chance(float chance) => false; public static float Range(float min, float max) => 0; public static int Range(int min, int max) => 0; public static int RangeInclusive(int min, int max) => 0; public static float Gaussian(float centerX, float widthFactor) => 0; public static bool MTBEventOccurs(float mtb, float mtbUnit, float checkDuration) => false; public static T Element<T>(T a, T b) => a; public static T Element<T>(T a, T b, T c) => a; }

    public static class Messages { public static void Message(string text, MessageTypeDef def, bool historical = true) { } public static void Message(string text, LookTargets lookTargets, MessageTypeDef def, bool historical = true) { } }
    public class MessageTypeDef : Def { }
    public static class MessageTypeDefOf { public static MessageTypeDef NeutralEvent; public static MessageTypeDef PositiveEvent; public static MessageTypeDef NegativeEvent; public static MessageTypeDef TaskCompletion; public static MessageTypeDef SilentInput; public static MessageTypeDef RejectInput; public static MessageTypeDef CautionInput; public static MessageTypeDef ThreatBig; public static MessageTypeDef ThreatSmall; }
    public static class LetterDefOf { public static LetterDef NeutralEvent; public static LetterDef PositiveEvent; public static LetterDef NegativeEvent; public static LetterDef ThreatBig; public static LetterDef ThreatSmall; public static LetterDef Death; }

    public class FloatRange : IExposable { public float min; public float max; public FloatRange() { } public FloatRange(float min, float max) { this.min = min; this.max = max; } public float RandomInRange => 0; public bool Includes(float f) => false; public void ExposeData() { } }
    public class IntRange : IExposable { public int min; public int max; public IntRange() { } public IntRange(int min, int max) { this.min = min; this.max = max; } public int RandomInRange => 0; public bool Includes(int i) => false; public void ExposeData() { } }

    public static class PawnUtility { public static bool ShouldSendNotificationAbout(Pawn p) => false; }
    public static class TranslatorFormattedStringExtensions { public static TaggedString Translate(this string key, params NamedArgument[] args) => default; public static bool TryTranslate(this string key, out TaggedString result) { result = default; return false; } }
    public static class TooltipHandler { public static void TipRegion(Rect rect, string tip) { } public static void TipRegion(Rect rect, TipSignal tip) { } }
    public struct TipSignal { public string text; public int uniqueId; public TipSignal(string text, int uniqueId = 0) { this.text = text; this.uniqueId = uniqueId; } public static implicit operator TipSignal(string s) => default; }

    public static class Text { public static GameFont Font { get; set; } public static TextAnchor Anchor { get; set; } public static Color CurFontStyle => default; public static float CalcHeight(string text, float width) => 0; public static Vector2 CalcSize(string text) => default; public static GUIStyle CurTextFieldStyle => null; public static GUIStyle CurTextAreaStyle => null; public static GUIStyle CurTextAreaReadOnlyStyle => null; }
    public enum GameFont { Tiny, Small, Medium }
    public static class Widgets
    {
        public static void Label(Rect rect, string label) { }
        public static void Label(Rect rect, TaggedString label) { }
        public static bool ButtonText(Rect rect, string label, bool drawBackground = true, bool doMouseoverSound = true, bool active = true, TextAnchor? overrideTextAnchor = null) => false;
        public static bool ButtonInvisible(Rect rect, bool doMouseoverSound = true) => false;
        public static bool ButtonImage(Rect butRect, Texture2D tex, bool doMouseoverSound = true, Color? overrideColor = null) => false;
        public static bool ButtonImage(Rect butRect, Texture2D tex, Color baseColor, Color mouseoverColor, bool doMouseoverSound = true) => false;
        public static void DrawHighlightIfMouseover(Rect rect) { }
        public static void DrawHighlight(Rect rect) { }
        public static void DrawBox(Rect rect, int thickness = 1, Texture2D boxTex = null) { }
        public static void DrawBoxSolid(Rect rect, Color color) { }
        public static void DrawLine(Vector2 from, Vector2 to, Color color, float width) { }
        public static void DrawTextureFitted(Rect outerRect, Texture tex, float scale, Vector2 texProportions = default, Rect texCoords = default, float angle = 0, Material mat = null) { }
        public static void DrawTexturePart(Rect drawRect, Rect uvRect, Texture2D tex) { }
        public static void DrawWindowBackground(Rect rect) { }
        public static void DrawMenuSection(Rect rect) { }
        public static void DrawShadowAround(Rect rect) { }
        public static bool CheckboxLabeled(Rect rect, string label, ref bool checkOn, bool disabled = false, Texture2D texChecked = null, Texture2D texUnchecked = null, bool placeCheckboxNearText = false) => false;
        public static void Checkbox(Vector2 leftTop, ref bool checkOn, float size = 24, bool disabled = false, bool paintable = false, Texture2D texChecked = null, Texture2D texUnchecked = null) { }
        public static void CheckboxDraw(float x, float y, bool active, bool disabled, float size = 24, Texture2D texChecked = null, Texture2D texUnchecked = null) { }
        public static float HorizontalSlider(Rect rect, float value, float leftValue, float rightValue, bool middleAlignment = false, string label = null, string leftAlignedLabel = null, string rightAlignedLabel = null, float roundTo = -1) => 0;
        public static void FillableBar(Rect rect, float fillPercent, Texture2D fillTex = null, Texture2D bgTex = null, bool doBorder = true) { }
        public static string TextField(Rect rect, string text) => "";
        public static string TextArea(Rect rect, string text, bool readOnly = false) => "";
        public static void TextFieldNumeric<T>(Rect rect, ref T val, ref string buffer, float min = 0, float max = float.MaxValue) where T : struct { }
        public static Rect EndScrollView(ref Vector2 scrollPosition) => default;
        public static void BeginScrollView(Rect outRect, ref Vector2 scrollPosition, Rect viewRect, bool showScrollbars = true) { }
        public static void EndScrollView() { }
        public static bool RadioButton(float x, float y, bool chosen) => false;
        public static bool RadioButton(Rect rect, bool chosen) => false;
        public static bool RadioButtonLabeled(Rect rect, string labelText, bool chosen) => false;
        public static void DrawRectFast(Rect position, Color color, GUIContent content = null) { }
        public static void DrawLineHorizontal(float x, float y, float length) { }
        public static void DrawLineVertical(float x, float y, float length) { }
        public static void ThingIcon(Rect rect, Thing thing, float alpha = 1, Rot4? rot = null) { }
        public static void ThingIcon(Rect rect, ThingDef thingDef, ThingDef stuffDef = null, float scale = 1) { }
        public static void DefIcon(Rect rect, Def def, ThingDef stuffDef = null, float scale = 1, Color? drawColor = null) { }
        public static void InfoCardButton(float x, float y, Def def) { }
        public static void InfoCardButton(float x, float y, Thing thing) { }
        public static bool CloseButtonFor(Rect rectToClose) => false;
        public static void NoneLabelCenteredVertically(Rect rect, string label = null) { }
        public static void NoneLabel(Rect rect, float y, string label = null) { }
        public static Rect BeginScrollView(Rect outRect, ref Vector2 scrollPosition, Rect viewRect) { return default; }
        public static void DrawAtlas(Rect rect, Texture2D atlas) { }
        public static Color NormalOptionColor => Color.white;
        public static Color InactiveColor => default;
        public static Color MouseoverOptionColor => default;
        public static void ListSeparator(ref float curY, float width, string label) { }
        public static bool LongLabel(float x, float width, string label, ref float curY) => false;
        public static void IntEntry(Rect rect, ref int value, ref string editBuffer, int multiplier = 1) { }
        public static void FloatRange(Rect rect, int id, ref Verse.FloatRange range, float min = 0, float max = 1, string labelKey = null, ToStringStyle valueStyle = ToStringStyle.FloatTwo, float gap = 0, GameFont sliderLabelFont = GameFont.Tiny, Color? sliderLabelColor = null) { }
        public static void IntRange(Rect rect, int id, ref Verse.IntRange range, int min = 0, int max = 100, string labelKey = null, int minWidth = 0) { }
        public static void DropdownMenu<Payload>(Rect rect, Payload target, Func<Payload, string> getPayloadLabel, Action<Payload> payloadSetter, IEnumerable<Payload> options, string buttonLabel) { }
        public static void Dropdown<Target, Payload>(Rect rect, Target target, Func<Target, Payload> getPayload, Func<Target, IEnumerable<DropdownMenuElement<Payload>>> menuGenerator, string buttonLabel = null, Texture2D buttonIcon = null, string dragLabel = null, Texture2D dragIcon = null, Action dropdownOpened = null, bool paintable = false) { }
    }
    public struct DropdownMenuElement<Payload> { public FloatMenuOption option; public Payload payload; }
    public enum ToStringStyle { Integer, FloatOne, FloatTwo, FloatThree, FloatMaxOne, FloatMaxTwo, FloatMaxThree, PercentZero, PercentOne, PercentTwo, Temperature, TemperatureOffset, WorkAmount, Money }
    public class FloatMenuOption { public string Label; public Action action; public bool Disabled; public string tooltip; public FloatMenuOption(string label, Action action, MenuOptionPriority priority = MenuOptionPriority.Default, Action<Rect> mouseoverGuiAction = null, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0) { Label = label; this.action = action; } }
    public class WorldObject { }
    public enum MenuOptionPriority { DisabledOption, VeryLow, Low, Default, High, GoHere, RescueImportant, InitiateSocial }
    public class FloatMenu : Window { public FloatMenu(List<FloatMenuOption> options) { } }

    public class Bill { }
}

namespace Verse.AI
{
    public class Job : Verse.IExposable
    {
        public Verse.JobDef def;
        public Verse.LocalTargetInfo targetA;
        public Verse.LocalTargetInfo targetB;
        public Verse.LocalTargetInfo targetC;
        public int count;
        public int maxNumMeleeAttacks;
        public int maxNumStaticAttacks;
        public List<Verse.LocalTargetInfo> targetQueueA;
        public List<Verse.LocalTargetInfo> targetQueueB;
        public float expiryInterval;
        public bool checkOverrideOnExpire;
        public bool playerForced;
        public Verse.Bill bill;
        public bool haulOpportunisticDuplicates;
        public HaulMode haulMode;
        public Verse.ThingDef plantDefToSow;
        public int takeExtraIngestibles;
        public RecursiveProximityCheckMode recursiveProximityCheckMode;
        public virtual void ExposeData() { }
        public Job() { }
        public Job(Verse.JobDef def) { this.def = def; }
        public Job(Verse.JobDef def, Verse.LocalTargetInfo targetA) { this.def = def; this.targetA = targetA; }
        public Job(Verse.JobDef def, Verse.LocalTargetInfo targetA, Verse.LocalTargetInfo targetB) { this.def = def; this.targetA = targetA; this.targetB = targetB; }
        public Job(Verse.JobDef def, Verse.LocalTargetInfo targetA, Verse.LocalTargetInfo targetB, Verse.LocalTargetInfo targetC) { this.def = def; this.targetA = targetA; this.targetB = targetB; this.targetC = targetC; }
        public Job(Verse.JobDef def, Verse.LocalTargetInfo targetA, int count) { this.def = def; this.targetA = targetA; this.count = count; }
    }
    public enum HaulMode { Undefined, ToCellNonStorage, ToCellStorage, ToContainer }
    public enum RecursiveProximityCheckMode { None, ExpandPossible, SameRoom }

    public class JobDriver : Verse.IExposable
    {
        public Verse.Pawn pawn;
        public Job job;
        public virtual bool TryMakePreToilReservations(bool errorOnFailed) => false;
        protected virtual IEnumerable<Toil> MakeNewToils() => null;
        public virtual void ExposeData() { }
        public virtual string GetReport() => "";
        public virtual void Notify_Starting() { }
        public virtual RandomSocialMode DesiredSocialMode() => RandomSocialMode.Normal;
        public virtual bool CanBeginNowWhileLyingDown() => false;
        public Verse.LocalTargetInfo TargetA => default;
        public Verse.LocalTargetInfo TargetB => default;
        public Verse.LocalTargetInfo TargetC => default;
        public Verse.Thing TargetThingA { get; set; }
        public Verse.Thing TargetThingB { get; set; }
        public Map Map => null;
        public int ticksLeftThisToil;
        public PathEndMode rotateToFace;
        public void ReadyForNextToil() { }
        public void EndJobWith(JobCondition condition) { }
        public virtual void Notify_DamageTaken(Verse.DamageInfo dinfo) { }
        public virtual void Cleanup() { }
    }
    public enum RandomSocialMode { Off, Normal, SuperActive }

    public class ThinkNode { public virtual ThinkResult TryIssueJobPackage(Verse.Pawn pawn, JobIssueParams jobParams) => default; public virtual ThinkNode DeepCopy(bool resolve = true) => null; }
    public class ThinkNode_JobGiver : ThinkNode { protected virtual Job TryGiveJob(Verse.Pawn pawn) => null; public virtual float GetPriority(Verse.Pawn pawn) => 0; }
    public class ThinkNode_Conditional : ThinkNode { protected virtual bool Satisfied(Verse.Pawn pawn) => false; }
    public struct ThinkResult { public Job Job; public ThinkNode SourceNode; public Verse.ThinkTreeDef Tag; public bool IsValid => Job != null; public static ThinkResult NoJob => default; }
    public struct JobIssueParams { }
    public enum JobCondition { None, Succeeded, Ongoing, Errored, Incompletable, InterruptForced, InterruptOptional, QueuedNoLongerValid, InterruptForced_SelfInterrupt }

    public class Toil
    {
        public Func<Verse.LocalTargetInfo> handlingFacing;
        public Action initAction;
        public Action tickAction;
        public Action<JobCondition> AddFinishAction;
        public Func<JobCondition> endCondition;
        public int defaultDuration;
        public SkillDef activeSkill;
        public bool atomicWithPrevious;
        public RandomSocialMode socialMode;
        public Verse.EffecterDef activeEffecter;
        public Toil WithProgressBarToilDelay(Verse.AI.TargetIndex ind, bool interpolateBetweenActorAndTarget = false, float offsetZ = -0.5f) => this;
        public Toil PlaySustainerOrSound(Verse.SoundDef soundDef) => this;
        public Toil FailOnDespawnedOrNull(TargetIndex ind) => this;
        public Toil FailOnDespawnedNullOrForbidden(TargetIndex ind) => this;
        public Toil FailOnBurningImmobile(TargetIndex ind) => this;
        public Toil FailOnCannotTouch(TargetIndex ind, Verse.PathEndMode mode) => this;
        public Toil FailOnSomeonePhysicallyInteracting(TargetIndex ind) => this;
        public Toil FailOn(Func<bool> condition) => this;
        public Toil WithEffect(Verse.EffecterDef effecterDef, Func<Verse.LocalTargetInfo> effectTargetGetter) => this;
        public Toil WithEffect(Verse.EffecterDef effecterDef, TargetIndex ind) => this;
    }
    public enum TargetIndex { None, A, B, C }
    public class SkillDef : Verse.Def { }

    public class Pawn_MindState : Verse.IExposable
    {
        public bool wantsToTradeWithColony;
        public bool IsIdle => false;
        public List<AutoFeeder> autoFeeders;
        public virtual void ExposeData() { }
        public RimWorld.AutofeedMode AutofeedSetting(Verse.Pawn baby) => default;
        public void SetAutofeeder(Verse.Pawn baby, Verse.Pawn feeder) { }
    }
    public class AutoFeeder { }

    public class Pawn_JobTracker
    {
        public Job curJob;
        public JobDriver curDriver;
        public void StartJob(Job newJob, JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true, Verse.ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false) { }
        public void EndCurrentJob(JobCondition condition, bool startNewJob = true, bool canReturnToPool = true) { }
        public bool IsCurrentJobPlayerForced() => false;
    }
    public enum JobTag { Misc, MiscWork, Fieldwork, Idle, InMentalState, SatisfyingNeeds, DraftedOrder, UnspecifiedLordDuty, ManhunterPack, Escaping, MapExit, Trading }

    public static class Toils_General { public static Toil Wait(int ticks, Verse.AI.TargetIndex face = TargetIndex.None) => new Toil(); public static Toil DoAtomic(Action action) => new Toil(); public static Toil Open(TargetIndex containerInd) => new Toil(); public static Toil PutCarriedThingInInventory() => new Toil(); public static Toil WaitWith(TargetIndex ind, int ticks, bool useProgressBar = false, bool maintainPosture = false, bool maintainSleep = false, TargetIndex face = TargetIndex.None) => new Toil(); }
    public static class Toils_Goto { public static Toil GotoThing(TargetIndex ind, Verse.PathEndMode peMode) => new Toil(); public static Toil GotoCell(TargetIndex ind, Verse.PathEndMode peMode) => new Toil(); public static Toil Goto(TargetIndex ind, Verse.PathEndMode peMode) => new Toil(); }
    public static class Toils_Haul { public static Toil StartCarryThing(TargetIndex haulableInd, bool putRemainderInQueue = false, bool subtractNumTakenFromJobCount = false, bool failIfStackCountLessThanJobCount = false, bool reserve = true) => new Toil(); public static Toil PlaceHauledThingInCell(TargetIndex cellInd, Toil nextToilOnPlaceFailOrIncomplete, bool storageMode, bool tryStoreInSameStorageIfSpotCantHoldWholeStack = false) => new Toil(); public static Toil CarryHauledThingToCell(TargetIndex ind) => new Toil(); }
    public static class Toils_Reserve { public static Toil Reserve(TargetIndex ind, int maxPawns = 1, int stackCount = -1, Verse.ReservationLayerDef layer = null) => new Toil(); public static Toil Release(TargetIndex ind) => new Toil(); }
    public static class Toils_Jump { public static Toil JumpIf(Func<bool> condition, Toil jumpToil) => new Toil(); public static Toil Jump(Toil jumpToil) => new Toil(); }
    public static class Toils_Ingest { public static Toil FinalizeIngest(Verse.Pawn ingester, TargetIndex ingestibleInd) => new Toil(); public static Toil ChewIngestible(Verse.Pawn chewer, float durationMultiplier, TargetIndex ingestibleInd, TargetIndex tableOrChairInd = TargetIndex.B) => new Toil(); }

    public class MentalStateHandler { }
    public class MentalBreaker { }
}

namespace Verse.AI.Group
{
    public class Lord { public List<Verse.Pawn> ownedPawns; }
}

namespace Verse.Sound
{
    public struct SoundInfo { public static implicit operator SoundInfo(Verse.Thing t) => default; public static SoundInfo InMap(Verse.TargetInfo target, MaintenanceType maint = MaintenanceType.None) => default; }
    public enum MaintenanceType { None, PerTick }
    public static class SoundStarter { public static void PlayOneShot(this Verse.SoundDef def, SoundInfo info) { } public static void PlayOneShotOnCamera(this Verse.SoundDef def, Verse.Map map = null) { } }
}

namespace Verse.Noise
{
    public abstract class ModuleBase { public abstract double GetValue(double x, double y, double z); }
}
