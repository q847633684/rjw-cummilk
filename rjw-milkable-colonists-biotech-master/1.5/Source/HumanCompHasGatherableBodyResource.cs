using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using rjw;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static rjw.Dialog_Sexcard;
using static rjw.GenderHelper;
using static rjw.xxx;

namespace Milk
{

    // possible to do's:  Interactions.  Records.  Thoughts.

    public abstract class HumanCompHasGatherableBodyResource : ThingComp
    {
        protected abstract float GatherResourcesIntervalDays { get; }

        protected abstract float ResourceAmount { get; }
        protected abstract float ResourceAmountBase { get; }

        protected abstract ThingDef ResourceDef { get; }

        protected abstract string SaveKeyFullness { get; }
        protected abstract string SaveKeyBottleCount { get; }

        public static readonly ThoughtDef wasBreastfedAdult = DefDatabase<ThoughtDef>.GetNamed("wasBreastfedAdult");
        public static readonly ThoughtDef wasBreastfedAdultHucowIdeo = DefDatabase<ThoughtDef>.GetNamed("wasBreastfedAdultHucowIdeo");
        public static readonly ThoughtDef didBreastfeedAdultGood = DefDatabase<ThoughtDef>.GetNamed("didBreastfeedAdultGood");
        public static readonly ThoughtDef didBreastfeedAdultBad = DefDatabase<ThoughtDef>.GetNamed("didBreastfeedAdultBad");
        public static readonly ThoughtDef didBreastfeedAdultGoodHucowIdeo = DefDatabase<ThoughtDef>.GetNamed("didBreastfeedAdultGoodHucowIdeo");

        public float BreastSize = 1f;

        public float BreastSizeDays = 1f;

        public float milkOverflowCount = 5f; //how often milk will drip when over full. 1 is a little too quick.

        protected float fullness;
        public float Fullness
        {
            get
            {
                return this.fullness;
            }
        }

        protected float bottleCount;
        public float BottleCount
        {
            get
            {
                return this.bottleCount;
            }
        }

        private static readonly ThingDef filthMilkLeak = ThingDef.Named("FilthMilkLeak");

        protected float fullnessLeak;
        public float FullnessLeak
        {
            get
            {
                return this.fullnessLeak;
            }
        }

        public virtual bool Active
		{
			get
			{
				if (this.parent is Pawn)
				{
					var p = this.parent as Pawn;
					if (!p.InContainerEnclosed) //TODO: someday milking in bioreactor/vat/milkmachine?
						if (!p.Dead && p.Map != null)
                            return p.IsColonist || p.IsPrisoner;
                    //return p.IsColonist || p.IsPrisoner || (p.Map != null && p.Map.IsPlayerHome);
				}

				return false;
			}
		}

        public bool ActiveAndFull
		{
			get
			{
				return this.Active && this.fullness >= 1f;
			}
		}

        public override void PostExposeData()
		{
			base.PostExposeData();
            Scribe_Values.Look<float>(ref this.fullness, this.SaveKeyFullness, 0f, false);
            Scribe_Values.Look<float>(ref this.bottleCount, this.SaveKeyBottleCount, 0f, false);

        }
        

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!MilkSettings.enableDebugFillButton)
                yield break;
            //if (!Prefs.DevMode)
            //    yield break;
            //if (this.fullness == 0)
            //    yield break;
            float amount = 1.5f;
            if (this.fullness == 0.001f) amount = 1.0f;
            else if (this.fullness == 1.5f) amount = 0.001f;

            yield return new Command_Action
            {
                Order = 101,
                defaultLabel = "DebugFillMilkGizmoLabel".Translate(),
                defaultDesc = "DebugFillMilkGizmoDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Things/Item/Food/MilkBottle/MilkBottle_b"),
                action = () => SetMilk(amount)

            };
        }

        public void SetMilk(float amount)
        {
            this.fullness = amount;
        }

        public void DebugSetMilkFullness(float amount)
        {
            this.fullness = amount;
        }

        public void HucowNeedsFulfill(Pawn pawn, float amount)
        {
            if (pawn != null && pawn.needs != null)
            {
                //this hucow pawn gains comfort and other things when breastfed from.
                if (pawn.needs.comfort != null) pawn.needs.comfort.CurLevel += amount; //comfort
                if (pawn.needs.joy != null) pawn.needs.joy.CurLevel += amount; //recreation
                if (pawn.needs.beauty != null) pawn.needs.beauty.CurLevel += (amount/2); //smaller beauty gains

                if (pawn.needs.indoors != null) pawn.needs.indoors.CurLevel += amount;
                if (pawn.needs.outdoors != null) pawn.needs.outdoors.CurLevel += amount;
            
            //var sex_need = pawn.needs.TryGetNeed<Need_Sex>();
            //if (sex_need != null) sex_need.CurLevel += amount;
            }
        }

        public bool BelongsToHucowIdeology(Pawn pawn)
        {
            if (!ModsConfig.IdeologyActive) return false;

            Ideo ideo = pawn.Ideo;
            if (ideo == null || IdeoDefOf.MemeHucow == null) return false;

            if (ideo.HasMeme(IdeoDefOf.MemeHucow)) return true;

            return false;
        }

        public bool shouldAdd = true;
        public bool ShouldRedress = false;

        //moving into a shared location. 
        public float TotalBreastSevCalc(Pawn pawn, List<Hediff> breastList)
        {

            var totalBreastSevCalc = 0f;
            if (!breastList.NullOrEmpty())
                foreach (var breasts in breastList.Where(x => !x.TryGetComp<HediffComp_SexPart>().Fluid.defName.NullOrEmpty()))
                {
                    var thisSev = 0f;
                    if (MilkSettings.flatChestGivesMilk)
                    {
                        thisSev = Math.Max(0.1f, breasts.Severity);
                    }
                    else
                    {
                        thisSev = breasts.Severity;
                    }

                    //modifiers for artificial breasts. changes how much they can store. by default, less than normal but you can change the amount in options
                    var thisBreastDefString = breasts.def.ToString();
                    if (thisBreastDefString == "HydraulicBreasts") { thisSev = thisSev * MilkSettings.breastMilkModifierHydraulic; }
                    if (thisBreastDefString == "BionicBreasts") { thisSev = thisSev * MilkSettings.breastMilkModifierBionic; }
                    if (thisBreastDefString == "ArchotechBreasts") { thisSev = thisSev * MilkSettings.breastMilkModifierArchotech; }

                    totalBreastSevCalc += thisSev;
                }

            return totalBreastSevCalc;
        }
        

        public void TryMakeBottle(Pawn pawn, Thing target, float fullfillAmount)
        {
            //partner isn't used?
            int loopCount = 1;
            //if (MilkSettings.workSpeedMult >= 1) loopCount = MilkSettings.workSpeedMult;

            var breastList = pawn.GetBreastList();

            var totalBreastSev = TotalBreastSevCalc(pawn, breastList);

            if (totalBreastSev == 0)
            {
                // we have no breasts of milk making capacity, leave early
                this.fullness = 0;
                return;
            }
            //bool shouldAdd = true;
            float basemilkgen = 2.5f; //fill up 2.5 times a day

            //this would be the max per day if you could pump at full perfectly
            float moddedLactationYieldMult = pawn.GetStatValue(MilkHediffModStats.MilkProductionYield);
            float milkPerDay = ((this.ResourceAmountBase * breastList.Count) + (this.ResourceAmount * totalBreastSev)  /* *bodysize */ ) * (1f / this.GatherResourcesIntervalDays) * moddedLactationYieldMult;
            float milkCountAtOneHundredFull = milkPerDay * (1f / basemilkgen);  //this should be milk count at 40%    (1/2.5)

            float lactationSpeedMult = pawn.GetStatValue(MilkHediffModStats.MilkProductionSpeed); //if you lactate faster you'll get to 100 fullness quicker
            float tickMod = MilkSettings.milkUpdateInterval / 60000f; //200/60000 = 1/300
            float fullnessPerTick = basemilkgen * tickMod * lactationSpeedMult;  //fill up 2.5 times aday (*lactMult). Full does not equal milk per day!    100% fulness = 40% of milk per day  (1.0 / 2.5)

            float milkTotalBottles = milkCountAtOneHundredFull * this.fullness;
            float fullnessPerBottle = 1f / milkCountAtOneHundredFull;

            this.bottleCount = milkTotalBottles;

            bool isHucow = false;
            if (HediffDefOf.HediffHucow != null) isHucow = pawn.health.hediffSet.HasHediff(HediffDefOf.HediffHucow);

            for (int i = 0; i < loopCount; i++)
            {
                if (this.fullness > fullnessPerBottle)
                {
                    this.fullness -= fullnessPerBottle;
                    shouldAdd = false; //don't add to fullness
                    Thing thing = ThingMaker.MakeThing(this.ResourceDef, null);
                    thing.stackCount = 1;
                    GenPlace.TryPlaceThing(thing, target.Position, target.Map, ThingPlaceMode.Near, null, null, default(Rot4));

                    this.bottleCount -= 1;

                    //this hucow pawn gains comfort and other things when breastfed from. med gain, being milked by another pawn
                    if (isHucow) HucowNeedsFulfill(pawn, fullfillAmount);

                }
            }

            SexUtility.DrawNude(pawn);
            ShouldRedress = true;
            shouldAdd = false;

            if (ModsConfig.BiotechActive && RJWSettings.sounds_enabled)
            {
                SoundInfo sound = new TargetInfo(pawn.Position, pawn.Map);
                sound.volumeFactor = RJWSettings.sounds_sex_volume;
                SoundDef.Named("BreastfeedingAdult").PlayOneShot(sound);
            }

        }

        public void Gathered(Pawn doer)
        {
            //hacking in a routine so other mods are more compatible with this updated version.
            //old version of the mod did everything in a Gathered function that counted bottles and set milk to 0.
            for (int i = 0; i < bottleCount; i++)
            {
                TryMakeBottle(doer, doer, 0.03f);
            }
            SetMilk(0f);

            if (MilkSettings.enableDebugLogging){
                Log.Message("A mod called the older 'Gathered' function in Milk.HumanCompHasGatherableBodyResource. This has been re-added to prevent errors but may not work as expected.");
            }
        }
        public void TryFeedPartner(Pawn pawn, Pawn partner)
        {
            int loopCount = 1;
            //if (MilkSettings.workSpeedMult >= 1) loopCount = MilkSettings.workSpeedMult;

            var breastList = pawn.GetBreastList();

            var totalBreastSev = TotalBreastSevCalc(pawn, breastList);

            if (totalBreastSev == 0)
            {
                // we have no breasts of milk making capacity, leave early
                this.fullness = 0;
                return;
            }
            //bool shouldAdd = true;
            float basemilkgen = 2.5f; //fill up 2.5 times a day

            //this would be the max per day if you could pump at full perfectly
            float moddedLactationYieldMult = pawn.GetStatValue(MilkHediffModStats.MilkProductionYield);
            float milkPerDay = ((this.ResourceAmountBase * breastList.Count) + (this.ResourceAmount * totalBreastSev)  /* *bodysize */ ) * (1f / this.GatherResourcesIntervalDays) * moddedLactationYieldMult;
            float milkCountAtOneHundredFull = milkPerDay * (1f / basemilkgen);  //this should be milk count at 40%    (1/2.5)

            float lactationSpeedMult = pawn.GetStatValue(MilkHediffModStats.MilkProductionSpeed); //if you lactate faster you'll get to 100 fullness quicker
            float tickMod = MilkSettings.milkUpdateInterval / 60000f; //200/60000 = 1/300
            float fullnessPerTick = basemilkgen * tickMod * lactationSpeedMult;  //fill up 2.5 times aday (*lactMult). Full does not equal milk per day!    100% fulness = 40% of milk per day  (1.0 / 2.5)

            float milkTotalBottles = milkCountAtOneHundredFull * this.fullness;
            float fullnessPerBottle = 1f / milkCountAtOneHundredFull;

            this.bottleCount = milkTotalBottles;

            bool isHucow = false;
            if (HediffDefOf.HediffHucow != null) isHucow = pawn.health.hediffSet.HasHediff(HediffDefOf.HediffHucow);

            float removeAmount = fullnessPerBottle / 5; //divide by 5 here and below for nutrition

            float modNutrition = 10f;
            if (MilkSettings.adultBreastfeedNutritionModAmount > 0f) modNutrition = MilkSettings.adultBreastfeedNutritionModAmount;

            float thingNutrition = 0f;
            //I've been assuming milk here. Added this to take the 'thing' and get the nutrition amount from it instead.
            if (this.ResourceDef.IsNutritionGivingIngestible) thingNutrition = this.ResourceDef.GetStatValueAbstract(StatDefOf.Nutrition);
            //Log.Message("nutrition value " + thingNutrition.ToString());
            thingNutrition /= 5; //we're taking smaller sips when breastfeeding, you cant drink the entire bottle in one gulp, too small of a dispenser.
            if (thingNutrition == 0f) thingNutrition = 0.01f; //just in case something goes wrong in the math. Maybe the nutrition value isn't set or something.

            for (int i = 0; i < loopCount; i++)
            {
                if (this.fullness > 0f) this.fullness = Math.Max(this.fullness - removeAmount, 0);

                partner.needs.food.CurLevel += thingNutrition * modNutrition;

                if (isHucow) HucowNeedsFulfill(pawn, 0.06f);

            }

            //thoughts!
            bool pawnBelongsToHucowIdeo = BelongsToHucowIdeology(pawn); 
            bool partnerBelongsToHucowIdeo = BelongsToHucowIdeology(partner); 
            if (pawnBelongsToHucowIdeo)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(didBreastfeedAdultGoodHucowIdeo);
                if (partnerBelongsToHucowIdeo)
                    partner.needs.mood.thoughts.memories.TryGainMemory(wasBreastfedAdultHucowIdeo);
                else
                    partner.needs.mood.thoughts.memories.TryGainMemory(wasBreastfedAdult);
            }
            else
            {
                if (pawn.IsColonist && partner.IsColonist)
                    pawn.needs.mood.thoughts.memories.TryGainMemory(didBreastfeedAdultGood);
                else if (!pawn.IsColonist && partner.IsColonist)
                    pawn.needs.mood.thoughts.memories.TryGainMemory(didBreastfeedAdultBad);

                if (partnerBelongsToHucowIdeo)
                    partner.needs.mood.thoughts.memories.TryGainMemory(wasBreastfedAdultHucowIdeo);
                else
                    partner.needs.mood.thoughts.memories.TryGainMemory(wasBreastfedAdult);
            }

            //see if this pawn belongs to a hucow ideology. if so, make some of the regular milkings also reset the regular hediff severitys
            //see if this will work even without ideology installed?
            //see if it will work without RIA installed?
            if (pawnBelongsToHucowIdeo)
            {
                var phs = pawn.health.hediffSet;
                Hediff hediffLactBT = null;
                if (ModsConfig.BiotechActive)
                {
                    hediffLactBT = phs.GetFirstHediffOfDef(HediffDef.Named("Lactating"));
                    if (hediffLactBT != null) hediffLactBT.Severity = 1;
                }
                Hediff hediffLactNatural = phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Natural"));
                if (hediffLactNatural != null) hediffLactNatural.Severity = 1;
            }

            SexUtility.DrawNude(pawn);
            ShouldRedress = true;
            shouldAdd = false;

            if (ModsConfig.BiotechActive && RJWSettings.sounds_enabled)
            {
                SoundInfo sound = new TargetInfo(pawn.Position, pawn.Map);
                sound.volumeFactor = RJWSettings.sounds_sex_volume;
                SoundDef.Named("BreastfeedingAdult").PlayOneShot(sound);
            }

        }
        
        public override void CompTick()
		{
			if (!parent.IsHashIntervalTick(MilkSettings.milkUpdateInterval))
			{
				return;
			}
			if (this.Active)
			{
				var pawn = this.parent as Pawn;
                if (pawn == null) return;

                //Log.Message("CompTick " + pawn.ToString());

                //Log.Message(pawn.Name.ToString() + " map " + pawn.Map.ToString());

                var breastList = pawn.GetBreastList();

                var totalBreastSev = TotalBreastSevCalc(pawn, breastList);

                if (totalBreastSev == 0)
				{
                    // we have no breasts of milk making capacity, leave early
                    //Log.Message("totalbreastsev0");
					this.fullness = 0;
					return;
				}

                var phs = pawn.health.hediffSet;
                bool isLactatingBT = false;
                bool isPregnant = false;
                if (ModsConfig.BiotechActive)
                {
                    isLactatingBT = phs.HasHediff(HediffDef.Named("Lactating"));
                    isPregnant = phs.HasHediff(HediffDef.Named("PregnantHuman"));
                }
                bool isLactatingDrug = phs.HasHediff(HediffDef.Named("Lactating_Drug"));
                bool isLactatingNatural = phs.HasHediff(HediffDef.Named("Lactating_Natural"));
                bool isLactatingPermanent = phs.HasHediff(HediffDef.Named("Lactating_Permanent"));
                bool isHeavyLactatingPermanent = phs.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent"));

                bool isMalnourished = phs.HasHediff(HediffDef.Named("Malnutrition"));

                bool isHucow = false;
                if (HediffDefOf.HediffHucow != null) isHucow = phs.HasHediff(HediffDefOf.HediffHucow);

                bool pawnBelongsToHucowIdeo = BelongsToHucowIdeology(pawn);

                Hediff pregnantHediff = null;
                Hediff hediffLactBT = null;
                if (ModsConfig.BiotechActive)
                {
                    hediffLactBT = phs.GetFirstHediffOfDef(HediffDef.Named("Lactating"));
                    pregnantHediff = phs.GetFirstHediffOfDef(HediffDef.Named("PregnantHuman"));
                }
                Hediff hediffLactDrug = phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Drug"));
                Hediff hediffLactNatural = phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Natural"));
                Hediff hediffLactPermanent = phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Permanent"));
                Hediff hediffLactHeavyPermanent = phs.GetFirstHediffOfDef(HediffDef.Named("Heavy_Lactating_Permanent"));

                //clean up first

                //remove the regular permanent if you have heavy permanent
                if (isHeavyLactatingPermanent && isLactatingPermanent)
                {
                    //pawn.health.RemoveHediff(phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Permanent")));
                    pawn.health.RemoveHediff(hediffLactPermanent);
                    isLactatingPermanent = false;
                    hediffLactPermanent = null;
                }

                //remove the standard drug/old hediffs if you have either permanent one.
                if (isHeavyLactatingPermanent || isLactatingPermanent)
                {
                    if (isLactatingDrug)
                    {
                        //pawn.health.RemoveHediff(phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Drug")));
                        pawn.health.RemoveHediff(hediffLactDrug);
                        isLactatingDrug = false;
                        hediffLactDrug = null;
                    }
                    if (isLactatingNatural)
                    {
                        //pawn.health.RemoveHediff(phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Natural")));
                        pawn.health.RemoveHediff(hediffLactNatural);
                        isLactatingNatural = false;
                        hediffLactNatural = null;
                    }
                }
                //add lactating if pawn has any other hediff and arent lacating. May have gained through drugs, or debugs.
                if (!isLactatingBT && !isMalnourished && (isLactatingPermanent || isHeavyLactatingPermanent) && pawn.IsDesignatedMilkAllowBfBaby())
                {
                    hediffLactBT = pawn.health.AddHediff(HediffDef.Named("Lactating"), null, null, null);
                    isLactatingBT = true;
                }

                if (isLactatingDrug && isLactatingNatural)
                {
                    //remove the naturalold if you took a drug
                    pawn.health.RemoveHediff(hediffLactNatural);
                    isLactatingNatural = false;
                    hediffLactNatural = null;
                }

                //add the biotech lactation hediff if you have the lactating_drug hediff and it isn't about to expire
                if (!isLactatingBT && isLactatingDrug && pawn.IsDesignatedMilkAllowBfBaby())
                {
                    //check that it isn't about to expire
                    //Hediff drugHediff = phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Drug"));
                    if (hediffLactDrug != null)
                    {
                        if (hediffLactDrug.Severity > 0.01)
                        {
                            //we're good, lets add lactating
                            hediffLactBT = pawn.health.AddHediff(HediffDef.Named("Lactating"), null, null, null);
                            //match the severity for timers
                            hediffLactBT.Severity = hediffLactDrug.Severity;
                            isLactatingBT = true;
                        }
                    }
                }

                //Log.Message("test message1 " + isLactatingBT.ToString() + " " + isLactatingNatural.ToString() + " " + pawn.IsDesignatedMilkAllowBfBaby().ToString());
                //add the biotech lactation hediff if you have the lactating_natural hediff and it isn't about to expire
                if (!isLactatingBT && isLactatingNatural && pawn.IsDesignatedMilkAllowBfBaby())
                {
                    //check that it isn't about to expire
                    //Hediff oldHediff = phs.GetFirstHediffOfDef(HediffDef.Named("Lactating_Natural"));
                    if (hediffLactNatural != null)
                    {
                        if (hediffLactNatural.Severity > 0.01)
                        {
                            //we're good, lets add biotech lactating
                            hediffLactBT = pawn.health.AddHediff(HediffDef.Named("Lactating"), null, null, null);
                            //match the severity for timers
                            hediffLactBT.Severity = hediffLactNatural.Severity;
                            isLactatingBT = true;
                        }
                    }
                }

                //if you have biotech lactating but no others, add the MC natural old hediff for older mods to use
                if (isLactatingBT && !(isLactatingDrug || isLactatingNatural || isLactatingPermanent || isHeavyLactatingPermanent))
                {
                    hediffLactNatural = pawn.health.AddHediff(HediffDef.Named("Lactating_Natural"), null, null, null);
                    //match the severity
                    hediffLactNatural.Severity = hediffLactBT.Severity;
                    isLactatingNatural = true;
                }

                //late stage pregnancy makes you lactate
                if (isPregnant && !(isLactatingBT || isLactatingDrug || isLactatingNatural || isLactatingPermanent || isHeavyLactatingPermanent))
                {
                    if (pregnantHediff.Severity > 0.8)
                    {
                        //pregnancy adding lactating hediff in late stage
                        hediffLactNatural = pawn.health.AddHediff(HediffDef.Named("Lactating_Natural"), null, null, null);
                        isLactatingNatural = true;
                        //match the severity
                        float tempSev = 1f;

                        //only add the biotech lactation hediff if the pawn is allowed to BF babies.
                        if (pawn.IsDesignatedMilkAllowBfBaby())
                        {
                            hediffLactBT = pawn.health.AddHediff(HediffDef.Named("Lactating"), null, null, null);
                            isLactatingBT = true;
                            tempSev = hediffLactBT.Severity;
                        }
                        
                        hediffLactNatural.Severity = tempSev;

                    }
                }
                //remove hediff if not allowed to BF babies.
                if (isLactatingBT && !pawn.IsDesignatedMilkAllowBfBaby())
                {
                    pawn.health.RemoveHediff(hediffLactBT);
                    isLactatingBT = false;
                }

                //if we have lact_drug or lact_nat make their severity match the BT hediff. This is so breastfeeding resets the pawn's timers correctly
                if (isLactatingBT && isLactatingNatural)
                {
                    hediffLactNatural.Severity = hediffLactBT.Severity;
                }

                if (isLactatingPermanent)
                {
                    hediffLactPermanent.CurStage.hungerRateFactorOffset = MilkSettings.lactPermHungerRateOffset;
                }

                if (isHeavyLactatingPermanent)
                {
                    hediffLactHeavyPermanent.CurStage.hungerRateFactorOffset = MilkSettings.lactHeavyPermHungerRateOffset;
                }

                /* --Removed. Basic drug hediff will not get lengthened. drug could fall off and lactation can revert back to natural now
                else if (isLactatingBT && isLactatingDrug)
                {
                    hediffLactDrug.Severity = hediffLactBT.Severity;
                }*/
                if (isLactatingBT)
                {
                    //adjust fertility
                    hediffLactBT.CurStage.fertilityFactor = MilkSettings.fertilityFactorOverride;
                }

                //now that we've worked out if we should be lactating or not, lets get down to business.
                if (isLactatingBT || isLactatingDrug || isLactatingNatural || isLactatingPermanent || isHeavyLactatingPermanent)
                {
                    //permanent hediffs reset BT hediff timeout
                    if (isHeavyLactatingPermanent || isLactatingPermanent || isHucow)
                    {
                        if (isLactatingBT)
                        {
                            hediffLactBT.Severity = 1;
                        }
                    }


                    //bool shouldAdd = true;
                    float basemilkgen = 2.5f; //fill up 2.5 times a day

                    //this would be the max per day if you could pump at full perfectly
                    float moddedLactationYieldMult = pawn.GetStatValue(MilkHediffModStats.MilkProductionYield);
                    float milkPerDay = ((this.ResourceAmountBase * breastList.Count) + (this.ResourceAmount * totalBreastSev)  /* *bodysize */ ) * (1f / this.GatherResourcesIntervalDays) * moddedLactationYieldMult;
                    float milkCountAtOneHundredFull = milkPerDay * (1f / basemilkgen);  //this should be milk count at 40%    (1/2.5)

                    float lactationSpeedMult = pawn.GetStatValue(MilkHediffModStats.MilkProductionSpeed); //if you lactate faster you'll get to 100 fullness quicker
                    float tickMod = MilkSettings.milkUpdateInterval / 60000f; //200/60000 = 1/300
                    float fullnessPerTick = basemilkgen * tickMod * lactationSpeedMult;  //fill up 2.5 times aday (*lactMult). Full does not equal milk per day!    100% fulness = 40% of milk per day  (1.0 / 2.5)
                    
                    float milkTotalBottles = milkCountAtOneHundredFull * this.fullness;
                    float fullnessPerBottle = 1f / milkCountAtOneHundredFull;

                    this.bottleCount = milkTotalBottles;


                    int loopCount = 1;
                    //leaving this loopCount enabled. This is for the feed baby job, and I'm doing hacky things to it.
                    if (MilkSettings.workSpeedMult >= 1) { loopCount = MilkSettings.workSpeedMult;  } 

                    if (MilkSettings.enableDebugLoggingMilkGrowth)
                    {
                        Log.Message(pawn.Name.ToString() + " Speed mod " + lactationSpeedMult.ToString());
                        Log.Message(pawn.Name.ToString() + " Yield mod " + moddedLactationYieldMult.ToString());

                        Log.Message(pawn.Name.ToString() + " Milk per day " + milkPerDay.ToString());
                        Log.Message(pawn.Name.ToString() + " Milk at 100 fullness " + milkCountAtOneHundredFull.ToString());
                        Log.Message(pawn.Name.ToString() + " Tickmod " + tickMod.ToString());
                        Log.Message(pawn.Name.ToString() + " fullness per tick " + fullnessPerTick.ToString());

                        Log.Message(pawn.Name.ToString() + " current bottle count " + milkTotalBottles.ToString());
                    }


                    /*  //moved into jobdriver!
                    
                    //milk self
                    if (pawn.jobs?.curJob?.def.ToString() == "MilkSelf")
                    {
                        //speed multiplier, just do the same thing multiple times
                        for (int i = 0; i < loopCount; i++)
                        {
                            if (this.fullness > fullnessPerBottle)
                            {
                                this.fullness -= fullnessPerBottle;
                                shouldAdd = false; //don't add to fullness
                                Thing thing = ThingMaker.MakeThing(this.ResourceDef, null);
                                thing.stackCount = 1;
                                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, null, default(Rot4));

                                //this hucow pawn gains comfort and other things when breastfed from. lowest gain. milking self
                                if (isHucow) HucowNeedsFulfill(pawn, 0.02f);

                            }
                        }

                        //be naked
                        SexUtility.DrawNude(pawn);
                        ShouldRedress = true;
                    }
                    */

                    //breastfeeding baby
                    //I have no idea how to do a harmony code patch. This will work for now.
                    if ((pawn.jobs?.curDriver.CurToilIndex == 6) && (pawn.jobs?.curJob?.def.ToString() == "Breastfeed"))
                    {
                        //New math
                        //After all the math, I believe this works out as taking 0.05 nutrition
                        float removeAmount = 1f / (milkCountAtOneHundredFull / 0.05f);

                        this.fullness = Math.Max(this.fullness - removeAmount, 0);
                        shouldAdd = false; //don't add to fullness
                        //Log.Message("breastfeeding " + removeAmount.ToString());
                        SexUtility.DrawNude(pawn);
                        ShouldRedress = true;

                        //this hucow pawn gains comfort and other things when breastfed from. med gain, breastfeed baby
                        if (isHucow) HucowNeedsFulfill(pawn, 0.03f);

                        //hack for speed breastfeeding babies using the multiplier.
                        //This only does extra work if this is above 1 for basic breastfeeding as this isn't the way regular breastfeeding works normally
                        if (loopCount > 1)
                        {
                            Pawn baby = pawn.CurJob.targetA.Pawn;
                            if (baby != null)
                            {
                                for (int i = 1; i < loopCount; i++)
                                {
                                    if (this.fullness > 0)
                                    {
                                        this.fullness = Math.Max(this.fullness - removeAmount, 0);
                                        baby.needs.food.CurLevel += 0.01f;

                                        //this hucow pawn gains comfort and other things when breastfed from. med gain, breastfeed baby
                                        if (isHucow) HucowNeedsFulfill(pawn, 0.03f);
                                    }
                                }
                            }
                        }
                    }

                    /* //moved into jobdriver!
                    
                    //breastfeeding adult   -- toil 1, BreastfeederAdult, nil
                    if ((pawn.jobs?.curJob?.def.ToString() == "BreastfeederAdult") && (pawn.jobs?.curDriver.CurToilIndex == 1))
                    {
                        Pawn partner = pawn.CurJob.targetA.Pawn;

                        if (partner != null)
                        {

                            //Log.Message(pawn.Name.ToString() + " is breastfeeder");
                            bool partnerBelongsToHucowIdeo = BelongsToHucowIdeology(partner);

                            float removeAmount = fullnessPerBottle / 5;
                            //float removeAmount = 1f / (milkCountAtOneHundredFull / 0.25f);
                            float modNutrition = 10f;
                            if (MilkSettings.adultBreastfeedNutritionModAmount > 0f)
                            {
                                modNutrition = MilkSettings.adultBreastfeedNutritionModAmount;
                            }

                            //speed multiplier, just do the same thing multiple times
                            for (int i = 0; i < loopCount; i++)
                            {
                                if (this.fullness > 0f)
                                {
                                    //float removeAmount = (1.0f - modifiedSeverity) * 0.5f / moddedLactationYieldMult; //5 times faster for bf adult
                                    this.fullness = Math.Max(this.fullness - removeAmount, 0);
                                    shouldAdd = false; //don't add to fullness

                                    //hack so adults don't starve when breastfeeding
                                    partner.needs.food.CurLevel += 0.01f * modNutrition;

                                    //this hucow pawn gains comfort and other things when breastfed from. best gain, breastfeed
                                    if (isHucow) HucowNeedsFulfill(pawn, 0.06f);

                                }
                            }
                                
                            //thoughts!
                            if (pawnBelongsToHucowIdeo)
                            {
                                pawn.needs.mood.thoughts.memories.TryGainMemory(didBreastfeedAdultGoodHucowIdeo);
                                if (partnerBelongsToHucowIdeo)
                                    partner.needs.mood.thoughts.memories.TryGainMemory(wasBreastfedAdultHucowIdeo);
                                else
                                    partner.needs.mood.thoughts.memories.TryGainMemory(wasBreastfedAdult);
                            }
                            else
                            {
                                if (pawn.IsColonist && partner.IsColonist)
                                    pawn.needs.mood.thoughts.memories.TryGainMemory(didBreastfeedAdultGood);
                               else if (!pawn.IsColonist && partner.IsColonist)
                                    pawn.needs.mood.thoughts.memories.TryGainMemory(didBreastfeedAdultBad);

                                if (partnerBelongsToHucowIdeo)
                                    partner.needs.mood.thoughts.memories.TryGainMemory(wasBreastfedAdultHucowIdeo);
                                else
                                    partner.needs.mood.thoughts.memories.TryGainMemory(wasBreastfedAdult);
                            }

                            //see if this pawn belongs to a hucow ideology. if so, make some of the regular milkings also reset the regular hediff severitys
                            //see if this will work even without ideology installed?
                            //see if it will work without RIA installed?
                            if (pawnBelongsToHucowIdeo)
                            {
                                //reset the severity of the lacation hediffs
                                if (hediffLactNatural != null) hediffLactNatural.Severity = 1;
                                //if (hediffLactDrug != null) hediffLactDrug.Severity = 1; //dont reset the drug!
                                if (ModsConfig.BiotechActive)
                                {
                                    if (hediffLactBT != null) hediffLactBT.Severity = 1;
                                }
                            }
                                                   
                            //Log.Message(partner.needs.food.CurLevel.ToString());

                            //partner.needs.food
                            //if (hunger.CurLevelPercentage > 0.99f)
                            SexUtility.DrawNude(pawn);
                            ShouldRedress = true;

                            //sounds. using the biotech suckle sound, so don't play if you dont have biotech to avoid an error
                            if (ModsConfig.BiotechActive && RJWSettings.sounds_enabled)
                            {
                                SoundInfo sound = new TargetInfo(pawn.Position, pawn.Map);
                                sound.volumeFactor = RJWSettings.sounds_sex_volume;
                                SoundDef.Named("BreastfeedingAdult").PlayOneShot(sound);
                            }
                        }
                        else
                        {
                            //no partner! kill job? or just wait for the job to do that itself?
                        }


                    }
                    */



                    /* //moved into jobdriver!
                    
                    //getting milked   -- toil 1, MilkedHuman, nil
                    if ((pawn.jobs?.curJob?.def.ToString() == "MilkedHuman") && (pawn.jobs?.curDriver.CurToilIndex == 1))
                    {
                        //Log.Message(pawn.Name.ToString() + " is breastfeeder");
                        //float moddedLactationYieldMult = Math.Max(pawn.GetStatValue(MilkHediffModStats.MilkProductionSpeed), 0.01f); //so we dont divide by 0.
                        //float removeAmount = (1.0f - modifiedSeverity) * 0.5f / moddedLactationYieldMult; //5 times faster for bf adult
                        //float removeAmount = 1f / (milkCountAtOneHundredFull / 0.05f);


                        //speed multiplier, just do the same thing multiple times
                        for (int i = 0; i < loopCount; i++)
                        {
                            if (this.fullness > fullnessPerBottle)
                            {
                                this.fullness -= fullnessPerBottle;
                                shouldAdd = false; //don't add to fullness
                                Thing thing = ThingMaker.MakeThing(this.ResourceDef, null);
                                thing.stackCount = 1;
                                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, null, default(Rot4));

                                //this hucow pawn gains comfort and other things when breastfed from. med gain, being milked by another pawn
                                if (isHucow) HucowNeedsFulfill(pawn, 0.03f);

                            }
                        }

                        //currently there are no thoughts for this. 
                        //add thoughts for milkee
                        //Pawn partner = pawn.CurJob.targetA.Pawn;
                        //if (partner != null) add thoughts for milker

                        //be naked
                        SexUtility.DrawNude(pawn);
                        ShouldRedress = true;

                        //sounds. using the biotech suckle sound, so don't play if you dont have biotech to avoid an error
                        if (ModsConfig.BiotechActive && RJWSettings.sounds_enabled)
                        {
                            SoundInfo sound = new TargetInfo(pawn.Position, pawn.Map);
                            sound.volumeFactor = RJWSettings.sounds_sex_volume;
                            SoundDef.Named("BreastfeedingAdult").PlayOneShot(sound);
                        }
                    }
                    //Hediff lactHediff = phs.GetFirstHediffOfDef(HediffDefOf.Lactating);

                    */

                    //we're lactating and we weren't breastfeeding, lets add to the saved amount
                    if (shouldAdd)
                    {

                        this.fullness = Math.Min(this.fullness + fullnessPerTick, 1.5f);
                        if (ShouldRedress)
                        {
                            //pawn.Drawer.renderer.SetAllGraphicsDirty(); // .graphics.ResolveApparelGraphics();
                           // GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);

                            if (xxx.is_human(pawn))
                            {
                                var comp = CompRJW.Comp(pawn);
                                if (comp != null)
                                {
                                    comp.drawNude = false;
                                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                                }
                            }
                            GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);

                            //so that we don't do this forever and mess up other things, like rjw sex.
                            ShouldRedress = false;

                        }

                    }
                    else
                    {
                        //set this so next tick it will reset.  Unless it gets set to false again during work.
                        shouldAdd = true;
                    }

                    if (isLactatingBT) 
                    {
                        //update the biotech hediff charge, this is ugly but it works.
                        HediffComp_Lactating lactComp = hediffLactBT.TryGetComp<HediffComp_Lactating>();
                        if (lactComp != null)
                        {
                            float lactAmount = this.fullness * -0.125f; //for faking the biotech hediff. In the data 0.125 is the max charge.
                            lactComp.GreedyConsume(1f); //remove it all to start
                            lactComp.GreedyConsume(lactAmount); //put some back
                        }
                    }
                }

                //swapped around a little. no need to do leak calculations if leaking isn't enabled.
                if (MilkSettings.enableMilkDrip)
                {
                    if (this.fullness >= MilkSettings.enabledMilkDripAmount)
                    {
                        //so we don't leak too fast if someone sets the leak amount low
                        this.fullnessLeak += Math.Max(0.25f,(this.fullness - MilkSettings.enabledMilkDripAmount));

                        if (this.fullnessLeak >= milkOverflowCount)
                        {

                            {
                                //leak
                                int milkLeakCount = (int)Math.Floor(this.fullnessLeak / milkOverflowCount);
                                FilthMaker.TryMakeFilth(pawn.PositionHeld, pawn.MapHeld, filthMilkLeak, pawn.LabelIndefinite(), milkLeakCount);
                                this.fullnessLeak -= (milkLeakCount * milkOverflowCount);
                                milkLeakCount = 0;
                            }
                        }
                    }
                }
                    
               
                if (this.fullness >= 1.0f)
                { 
                    if (!MilkSettings.breastGrowthDisabled)
					{
                        //unlikely reach here unless milking work is disabled
                        //expand breasts if over max value

                        if (pawn.IsHashIntervalTick(60000))
							if (!breastList.NullOrEmpty())
								foreach (var breasts in breastList)
								{
										breasts.Severity += (breasts.Severity * 0.01f);
								}
						//TODO: add breast pain?
					}

                }

                if (!breastList.NullOrEmpty() && MilkSettings.forceCapMaxBreastSize)
                {
                    float maxBreastSize = 1f;
                    if (MilkSettings.breastGrowthMaxSize != 0)
                        maxBreastSize = MilkSettings.breastGrowthMaxSize;

                    foreach (var breasts in breastList)
                    {
                        if (breasts.Severity > maxBreastSize)
                            breasts.Severity = maxBreastSize;
                    }
                    
                }
            }
		}

    }
}
