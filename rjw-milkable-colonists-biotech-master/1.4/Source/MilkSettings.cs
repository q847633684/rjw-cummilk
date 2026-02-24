using HarmonyLib;
using HugsLib.Settings;
using RimWorld;
using rjw;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Milk
{
    
    public class MilkSettings : ModSettings
    {
        
        public const int milkUpdateInterval = 200; // Putting this here so it's more global. Not going to let player change it though. probably.

        public static bool breastGrowthDisabled = false;
        public static float breastGrowthMaxSize = 1.0f;
        public static bool forceCapMaxBreastSize = true;
        public static bool ignoreReproductiveStage = false;
        public static bool flatChestGivesMilk = false;
        public static bool enableMilkDrip = true;
        public static float enabledMilkDripAmount = 1.0f;
        public static float adultBreastfeedNutritionModAmount = 5.0f;
        public static int workSpeedMult = 1;

        public static float fullnessMilkBottleAmount = 1.25f;
        public static float fullnessMilkBreastfeedAmount = 0.95f;
        public static float fullnessMilkSelfAmount = 1.5f;
        public static float fullnessMilkMachineAmount = 1.4f;
        public static float fullnessHungerAmount = 0.5f;

        public static bool enableDebugLogging = false;
        public static bool enableDebugLoggingMilkGrowth = false;

        public static bool enableDebugFillButton = false;

        public static bool enableLessInteruptions = true;

        public static float breastMilkModifierHydraulic = 0.5f;
        public static float breastMilkModifierBionic = 0.75f;
        public static float breastMilkModifierArchotech = 1.25f;

        public static float lactPermHungerRateOffset = 0.4f;
        public static float lactHeavyPermHungerRateOffset = 0.8f;
        public static float fertilityFactorOverride = 0.05f;
        
        private static Vector2 scrollPosition;
        //private static Vector2 scrollPosition2;
        private static float height_modifier = 1500f;


        public static void DoWindowContents(Rect inRect)
        {
            breastGrowthMaxSize = Mathf.Clamp(breastGrowthMaxSize, 0.2f, 5.0f);
            adultBreastfeedNutritionModAmount = Mathf.Clamp(adultBreastfeedNutritionModAmount, 1.0f, 100.0f);

            fullnessMilkBottleAmount = Mathf.Clamp(fullnessMilkBottleAmount, 0.5f, 1.5f);
            fullnessMilkBreastfeedAmount = Mathf.Clamp(fullnessMilkBreastfeedAmount, 0.5f, 1.5f);
            fullnessMilkSelfAmount = Mathf.Clamp(fullnessMilkSelfAmount, 0.5f, 1.5f);
            fullnessMilkMachineAmount = Mathf.Clamp(fullnessMilkMachineAmount, 0.5f, 1.5f);
            fullnessHungerAmount = Mathf.Clamp(fullnessHungerAmount, 0.1f, 0.9f);

            //are these even needed?
            //breastMilkModifierHydraulic = Mathf.Clamp(breastMilkModifierHydraulic, 0.0f, 1f);
            //breastMilkModifierBionic = Mathf.Clamp(breastMilkModifierBionic, 0.0f, 1f);
            //breastMilkModifierArchotech = Mathf.Clamp(breastMilkModifierArchotech, 0.0f, 2f);

            //30f for top page description and bottom close button
            Rect outRect = new Rect(0f, 30f, inRect.width, inRect.height - 30f);

            //-16 for slider, height_modifier - additional height for hidden options toggles
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, inRect.height + height_modifier);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect); // scroll

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.maxOneColumn = true;
            listingStandard.ColumnWidth = viewRect.width-10f; // / 2.05f;
            listingStandard.Begin(viewRect); 
            listingStandard.Gap(5f);

            listingStandard.CheckboxLabeled("MilkableColonists.breastGrowthDisabled".Translate(), ref breastGrowthDisabled, "MilkableColonists.breastGrowthDisabledDesc".Translate());
            listingStandard.Gap(5f);


            listingStandard.CheckboxLabeled("MilkableColonists.forceCapMaxBreastSize".Translate(), ref forceCapMaxBreastSize, "MilkableColonists.forceCapMaxBreastSizeDesc".Translate());
            listingStandard.Gap(5f);

            if (forceCapMaxBreastSize)
            {
                listingStandard.Label("MilkableColonists.breastGrowthMaxSize".Translate() + ": " + Math.Round(breastGrowthMaxSize, 2), -1f, "MilkableColonists.breastGrowthMaxSizeDesc".Translate());
                breastGrowthMaxSize = (float)listingStandard.Slider(breastGrowthMaxSize, 0.2f, 5f);
                listingStandard.Gap(5f);
            }

            listingStandard.Gap(5f);
            listingStandard.CheckboxLabeled("MilkableColonists.flatChestGivesMilk".Translate(), ref flatChestGivesMilk, "MilkableColonists.flatChestGivesMilkDesc".Translate());
            listingStandard.Gap(10f);
            listingStandard.CheckboxLabeled("MilkableColonists.enableMilkDrip".Translate(), ref enableMilkDrip, "MilkableColonists.enableMilkDripDesc".Translate());
            listingStandard.Gap(5f);
            listingStandard.Label("MilkableColonists.enabledMilkDripAmount".Translate() + ": " + Math.Round(enabledMilkDripAmount * 100, 1) + "%", -1f, "MilkableColonists.enabledMilkDripAmountDesc".Translate());
            enabledMilkDripAmount = (float)listingStandard.Slider(enabledMilkDripAmount, 0.5f, 1.5f);
            listingStandard.Gap(5f);
            listingStandard.Gap(20f);

            listingStandard.Label("MilkableColonists.fullnessMilkBottleAmount".Translate() + ": " + Math.Round(fullnessMilkBottleAmount * 100, 1) + "%", -1f, "MilkableColonists.fullnessMilkBottleAmountDesc".Translate());
            fullnessMilkBottleAmount = (float)listingStandard.Slider(fullnessMilkBottleAmount, 0.5f, 1.5f);
            listingStandard.Gap(5f);
            listingStandard.Gap(20f);

            listingStandard.Label("MilkableColonists.fullnessHungerAmount".Translate() + ": " + Math.Round(fullnessHungerAmount * 100, 1) + "%", -1f, "MilkableColonists.fullnessHungerAmountDesc".Translate());
            fullnessHungerAmount = (float)listingStandard.Slider(fullnessHungerAmount, 0.1f, 0.9f);
            listingStandard.Gap(5f);

            listingStandard.Label("MilkableColonists.fullnessMilkBreastfeedAmount".Translate() + ": " + Math.Round(fullnessMilkBreastfeedAmount * 100, 1) + "%", -1f, "MilkableColonists.fullnessMilkBreastfeedAmountDesc".Translate());
            fullnessMilkBreastfeedAmount = (float)listingStandard.Slider(fullnessMilkBreastfeedAmount, 0.5f, 1.5f);
            listingStandard.Gap(5f);

            listingStandard.Label("MilkableColonists.fullnessMilkSelfAmount".Translate() + ": " + Math.Round(fullnessMilkSelfAmount * 100, 1) + "%", -1f, "MilkableColonists.fullnessMilkSelfAmountDesc".Translate());
            fullnessMilkSelfAmount = (float)listingStandard.Slider(fullnessMilkSelfAmount, 0.5f, 1.5f);
            listingStandard.Gap(5f);

            listingStandard.Label("MilkableColonists.fullnessMilkMachineAmount".Translate() + ": " + Math.Round(fullnessMilkMachineAmount * 100, 1) + "%", -1f, "MilkableColonists.fullnessMilkMachineAmountDesc".Translate());
            fullnessMilkMachineAmount = (float)listingStandard.Slider(fullnessMilkMachineAmount, 0.5f, 1.5f);
            listingStandard.Gap(5f);

            listingStandard.Label("MilkableColonists.adultBreastfeedNutritionModAmount".Translate() + ": x " + Math.Round(adultBreastfeedNutritionModAmount, 1) + "", -1f, "MilkableColonists.adultBreastfeedNutritionModAmountDesc".Translate());
            adultBreastfeedNutritionModAmount = (float)listingStandard.Slider(adultBreastfeedNutritionModAmount, 1f, 100f);
            listingStandard.Gap(5f);

            listingStandard.Label("MilkableColonists.workSpeedMult".Translate() + ": x " + workSpeedMult + "", -1f, "MilkableColonists.workSpeedMultDesc".Translate());
            workSpeedMult = (int)listingStandard.Slider(workSpeedMult, 1, 10);
            listingStandard.Gap(5f);
            listingStandard.Gap(20f);


            listingStandard.Label("MilkableColonists.breastMilkModifierHydraulic".Translate() + ": x " + Math.Round(breastMilkModifierHydraulic * 100, 1) + "%", -1f, "MilkableColonists.breastMilkModifierHydraulicDesc".Translate());
            breastMilkModifierHydraulic = (float)listingStandard.Slider(breastMilkModifierHydraulic, 0.0f, 1.0f);
            listingStandard.Gap(5f);

            listingStandard.Label("MilkableColonists.breastMilkModifierBionic".Translate() + ": x " + Math.Round(breastMilkModifierBionic * 100, 1) + "%", -1f, "MilkableColonists.breastMilkModifierBionicDesc".Translate());
            breastMilkModifierBionic = (float)listingStandard.Slider(breastMilkModifierBionic, 0.0f, 1.5f);
            listingStandard.Gap(5f);

            listingStandard.Label("MilkableColonists.breastMilkModifierArchotech".Translate() + ": x " + Math.Round(breastMilkModifierArchotech * 100, 1) + "%", -1f, "MilkableColonists.breastMilkModifierArchotechDesc".Translate());
            breastMilkModifierArchotech = (float)listingStandard.Slider(breastMilkModifierArchotech, 0.0f, 2.0f);
            listingStandard.Gap(5f);
            listingStandard.Gap(20f);


            listingStandard.Label("MilkableColonists.lactPermHungerRateOffset".Translate() + ": x " + Math.Round(lactPermHungerRateOffset * 100, 1) + "%", -1f, "MilkableColonists.lactPermHungerRateOffsetDesc".Translate());
            lactPermHungerRateOffset = (float)listingStandard.Slider(lactPermHungerRateOffset, 0.1f, 0.5f);
            listingStandard.Gap(5f);

            listingStandard.Label("MilkableColonists.lactHeavyPermHungerRateOffset".Translate() + ": x " + Math.Round(lactHeavyPermHungerRateOffset * 100, 1) + "%", -1f, "MilkableColonists.lactHeavyPermHungerRateOffsetDesc".Translate());
            lactHeavyPermHungerRateOffset = (float)listingStandard.Slider(lactHeavyPermHungerRateOffset, 0.2f, 1.0f);
            listingStandard.Gap(5f);

            listingStandard.Label("MilkableColonists.fertilityFactorOverride".Translate() + ": " + Math.Round(fertilityFactorOverride * 100, 1) + "%", -1f, "MilkableColonists.fertilityFactorOverrideDesc".Translate());
            fertilityFactorOverride = (float)listingStandard.Slider(fertilityFactorOverride, 0.0f, 2.0f);
            listingStandard.Gap(5f);

            listingStandard.Gap(20f);


            listingStandard.CheckboxLabeled("MilkableColonists.ignoreReproductiveStage".Translate(), ref ignoreReproductiveStage, "MilkableColonists.ignoreReproductiveStageDesc".Translate());
            listingStandard.Gap(5f);
            //listingStandard.CheckboxLabeled("MilkableColonists.enableDebugLogging".Translate(), ref enableDebugLogging, "MilkableColonists.enableDebugLoggingDesc".Translate());
            //listingStandard.Gap(5f);
            listingStandard.CheckboxLabeled("MilkableColonists.enableLessInteruptions".Translate(), ref enableLessInteruptions, "MilkableColonists.enableLessInteruptionsDesc".Translate());
            listingStandard.Gap(5f);
            listingStandard.CheckboxLabeled("MilkableColonists.enableDebugLoggingMilkGrowth".Translate(), ref enableDebugLoggingMilkGrowth, "MilkableColonists.enableDebugLoggingMilkGrowthDesc".Translate());
            listingStandard.Gap(5f);
            listingStandard.CheckboxLabeled("MilkableColonists.enableDebugFillButton".Translate(), ref enableDebugFillButton, "MilkableColonists.enableDebugFillButtonDesc".Translate());



            listingStandard.End();
            Widgets.EndScrollView();

        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref breastGrowthDisabled, "breastGrowthDisabled", breastGrowthDisabled, true);
            Scribe_Values.Look(ref breastGrowthMaxSize, "breastGrowthMaxSize", breastGrowthMaxSize, true);
            Scribe_Values.Look(ref forceCapMaxBreastSize, "forceCapMaxBreastSize", forceCapMaxBreastSize, true);
            Scribe_Values.Look(ref ignoreReproductiveStage, "ignoreReproductiveStage", ignoreReproductiveStage, true);
            Scribe_Values.Look(ref flatChestGivesMilk, "flatChestGivesMilk", flatChestGivesMilk, true);
            Scribe_Values.Look(ref enableMilkDrip, "enableMilkDrip", enableMilkDrip, true);
            Scribe_Values.Look(ref adultBreastfeedNutritionModAmount, "adultBreastfeedNutritionModAmount", adultBreastfeedNutritionModAmount, true);
            Scribe_Values.Look(ref workSpeedMult, "workSpeedMult", workSpeedMult, true);
            
            Scribe_Values.Look(ref fullnessMilkBottleAmount, "fullnessMilkBottleAmount", fullnessMilkBottleAmount, true);
            Scribe_Values.Look(ref fullnessMilkBreastfeedAmount, "fullnessMilkBreastfeedAmount", fullnessMilkBreastfeedAmount, true);
            Scribe_Values.Look(ref fullnessMilkSelfAmount, "fullnessMilkSelfAmount", fullnessMilkSelfAmount, true);
            Scribe_Values.Look(ref fullnessMilkMachineAmount, "fullnessMilkMachineAmount", fullnessMilkMachineAmount, true);
            Scribe_Values.Look(ref fullnessHungerAmount, "fullnessHungerAmount", fullnessHungerAmount, true);

            Scribe_Values.Look(ref breastMilkModifierHydraulic, "breastMilkModifierHydraulic", breastMilkModifierHydraulic, true);
            Scribe_Values.Look(ref breastMilkModifierBionic, "breastMilkModifierBionic", breastMilkModifierBionic, true);
            Scribe_Values.Look(ref breastMilkModifierArchotech, "breastMilkModifierArchotech", breastMilkModifierArchotech, true);

            Scribe_Values.Look(ref lactPermHungerRateOffset, "lactPermHungerRateOffset", lactPermHungerRateOffset, true);
            Scribe_Values.Look(ref lactHeavyPermHungerRateOffset, "lactHeavyPermHungerRateOffset", lactHeavyPermHungerRateOffset, true);
            Scribe_Values.Look(ref fertilityFactorOverride, "fertilityFactorOverride", fertilityFactorOverride, true);

            Scribe_Values.Look(ref enableDebugLogging, "enableDebugLogging", enableDebugLogging, true);
            Scribe_Values.Look(ref enableDebugLoggingMilkGrowth, "enableDebugLoggingMilkGrowth", enableDebugLoggingMilkGrowth, true);
            Scribe_Values.Look(ref enableDebugFillButton, "enableDebugFillButton", enableDebugFillButton, true);
            Scribe_Values.Look(ref enableLessInteruptions, "enableLessInteruptions", enableLessInteruptions, true);


            



        }


    }
}
