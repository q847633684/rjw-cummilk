using Cumpilation.Common;
using MilkCum.Milk.Comps;
using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Cumpilation.Gathering
{
    /// <summary>
    /// Unlike `FluidGatheringBuilding` that works upon nearby sex, the PassiveFluidGatherer is trying to clean nearby floors from Filth.
    /// Due to logic, they must be in the same room. 
    /// 
    /// Important: For this to work properly, the ThingWithComp needs to have a `<tickerType>Normal</tickerType>` to run CompTick
    /// </summary>
    public class PassiveFluidGathererComp : ThingComp
    {
        /// <summary>
        /// Stores the currently running information on how much of which filth was soaked in (by filth def and producer for bed-linked filtering).
        /// Resets on load.
        /// </summary>
        public Dictionary<(ThingDef filthDef, Pawn producer), int> GatheredFilth = new Dictionary<(ThingDef filthDef, Pawn producer), int>();

        private PassiveFluidGathererCompProperties Props
        {
            get { return (PassiveFluidGathererCompProperties)this.props;}
        }

        //DevNote: I could also try to make a `CompTickRare`, then you need a `<tickerType>Rare</tickerType>` and the other Code is never run. But I don't know how that would go with HashIntervalTicks.
        public override void CompTick()
        {
            base.CompTick();

            if (parent.IsHashIntervalTick(Props.tickIntervall))
            {
                // onlyFluidFilth: false 以便同时吸收 EM_HumanMilkFilth 等（由 FluidGatheringDef.filth 支持的污物），高级桶可收集母乳污物
                var sexFluidFilths = GatheringUtility.GetNearbyFilth(this.parent, false, Props.range);
               // ModLog.Message($"{parent.def}@{parent.PositionHeld}:Found {filths.Count()} filths and {sexFluidFilths.Count()} Fluid-Associated Filths in range {properties.range}");
                CleanFilth(sexFluidFilths);
                FilthToItem();
            }
        }

        /// <summary>
        /// Checks the current `GatheredFilth` and spawns any item inside me if there is enough Filth gathered of the right kind.
        /// The logic for how much and what you need is done by `FluidGatheringDef`s. 
        /// </summary>
        public void FilthToItem()
        {
            var copy = new Dictionary<(ThingDef filthDef, Pawn producer), int>(GatheredFilth);

            foreach (var kv in copy)
            {
                var fluidFilthType = kv.Key.filthDef;
                var producer = kv.Key.producer;
                var amount = kv.Value;
                var fgDef = GatheringUtility.LookupGatheringDef(fluidFilthType);
                if (fgDef == null || fgDef.filthNecessaryForOneUnit <= 0) continue;

                int toSpawn = amount / fgDef.filthNecessaryForOneUnit;
                if (toSpawn <= 0) continue;

                MilkCum.HarmonyPatches.CumpilationIntegration.CumProducerForNextSpawn = producer;
                try
                {
                    Thing gatheredFluid = ThingMaker.MakeThing(fgDef.thingDef);
                    gatheredFluid.stackCount = toSpawn;
                    GenPlace.TryPlaceThing(gatheredFluid, parent.PositionHeld, parent.Map, ThingPlaceMode.Direct, out _);
                }
                finally
                {
                    MilkCum.HarmonyPatches.CumpilationIntegration.CumProducerForNextSpawn = null;
                }

                int remainder = amount % fgDef.filthNecessaryForOneUnit;
                var key = (fluidFilthType, producer);
                if (remainder > 0)
                    GatheredFilth[key] = remainder;
                else
                    GatheredFilth.Remove(key);
            }
        }

        /// <summary>
        /// Sucks up supported Filths. If bucket is linked to a bed with an owner, only absorbs filth recorded as that owner's; otherwise absorbs all (mixed).
        /// </summary>
        public void CleanFilth(IEnumerable<Filth> filths)
        {
            var link = parent.TryGetComp<CompCumBucketLink>();
            var bedOwner = link?.LinkedBed != null ? link.Owner : null;

            foreach (var filth in filths.Where(f => GatheringUtility.IsSupportedFilthType(f, Props.supportedFluids)))
            {
                if (Rand.Value > Props.cleanChance) continue;

                Pawn producer = FilthProducerRegistry.GetAndRemove(filth.Map, filth.Position, filth.def);
                if (bedOwner != null && producer != null && producer != bedOwner)
                    continue;

                var key = (filth.def, producer);
                filth.DeSpawn();
                if (GatheredFilth.TryGetValue(key, out var existing))
                    GatheredFilth[key] = existing + filth.stackCount;
                else
                    GatheredFilth[key] = filth.stackCount;

                if (Props.cleanAtmostOne) return;
            }
        }

    }
}
