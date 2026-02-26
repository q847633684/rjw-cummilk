#pragma warning disable CS0626, CS0824, CS0114, CS0108, CS0067, CS0649, CS0169, CS0414, CS0109
using System;
using System.Collections.Generic;
using Verse;

namespace rjw
{
    public static class RJWSettings { public static bool DevMode; public static float sexNeedRate; public static float fluidMultiplier; public static bool LustEnabled; }
    public static class xxx
    {
        public static bool is_human(Pawn p) => false;
        public static bool is_animal(Pawn p) => false;
        public static bool is_mechanoid(Pawn p) => false;
        public static bool has_penis(Pawn p) => false;
        public static bool has_vagina(Pawn p) => false;
        public static bool has_breasts(Pawn p) => false;
        public static bool has_anus(Pawn p) => false;
        public static bool is_insect(Pawn p) => false;
        public static float GetCumVolume(Pawn p) => 0;
        public static Hediff GetGenitalHediff(Pawn p) => null;
        public static List<Hediff> GetGenitals(Pawn p) => null;
        public static bool is_female(Pawn p) => false;
        public static bool is_male(Pawn p) => false;

        public enum rjwSextype { None, Vaginal, Anal, Oral, Masturbation, DoublePenetration, Boobjob, Handjob, Footjob, Scissoring, MutualMasturbation, Fisting, MechImplant, Fingering, Sixtynine }
    }

    public class SexProps
    {
        public Pawn pawn;
        public Pawn partner;
        public xxx.rjwSextype sexType;
        public bool isRape;
        public bool isWhoring;
        public bool isConsensual;
        public bool usedCondom;
        public BodyPartRecord partSelf;
        public BodyPartRecord partPartner;
    }

    public static class SexUtility
    {
        public static float GetFluidAmount(Pawn pawn) => 0;
        public static void SetFluidAmount(Pawn pawn, float amount) { }
        public static void AddFluid(Pawn pawn, float amount) { }
        public static bool CanSexWith(Pawn pawn, Pawn partner) => false;
        public static float GetSexSatisfaction(Pawn pawn) => 0;
        public static void SatisfyPersonal(Pawn pawn, Pawn partner, xxx.rjwSextype sextype, SexProps props = null) { }
        public static float GetLust(Pawn pawn) => 0;
        public static void SetLust(Pawn pawn, float amount) { }
        public static void TransferFluids(SexProps props) { }
    }

    public class Hediff_BasePregnancy : HediffWithComps { }
    public class Hediff_PartBaseNatural : HediffWithComps { public float Severity { get; set; } }
    public class Hediff_PartBaseArtificial : HediffWithComps { }
    public class CompRJW : ThingComp { }

    public static class Genital_Helper
    {
        public static bool has_genitals(Pawn pawn) => false;
        public static Hediff get_penis(Pawn pawn) => null;
        public static Hediff get_vagina(Pawn pawn) => null;
        public static Hediff get_anus(Pawn pawn) => null;
        public static Hediff get_breasts(Pawn pawn) => null;
        public static List<Hediff> get_AllGenitals(Pawn pawn) => null;
    }

    public class RaceGroupDef : Def { }
    public class FluidDef : Def { public ThingDef fluidDef; }
    public class RJWDefOf { public static HediffDef Hediff_Lust; public static HediffDef Hediff_BreastSize; }

    public static class CasualSex_Helper
    {
        public static Thing FindNearbyBucketToMasturbate(Pawn pawn) => null;
        public static Verse.IntVec3 FindSexLocation(Pawn pawn, Pawn partner = null) => default;
    }

    public class HediffDef_SexPart : HediffDef { }
    public interface ISexPartHediff { }
    public class SexFluidDef : Def { public ThingDef fluidThingDef; }
    public class SexFluidIngestionDoer { public virtual void DoIngestionOutcome(Pawn pawn, Thing ingested, int count) { } public virtual void Ingested(Pawn pawn, SexFluidDef fluidDef, float amount, ISexPartHediff source, ISexPartHediff target) { } }
}

namespace rjw.Modules.Interactions
{
    public static class InteractionHelper { }
}

namespace rjw.Modules.Interactions.Extensions
{
    public static class SexPropsExtensions
    {
        public static Verse.BodyPartRecord GetPartSelf(this rjw.SexProps props) => null;
        public static Verse.BodyPartRecord GetPartPartner(this rjw.SexProps props) => null;
    }
}

namespace rjw.Modules.Interactions.Helpers
{
    public static class SexInteractionHelper { }
}

namespace rjw.Modules.Shared.Logs
{
    public static class ModLog
    {
        public static void Message(string msg) { }
        public static void Warning(string msg) { }
        public static void Error(string msg) { }
        public static void Debug(string msg) { }
    }
}

