using Verse;

namespace MilkCum.Fluids.Cum
{
    /// <summary>数据已统一到 MilkCumSettings；此类仅作代理，供 Cumpilation 旧代码读取。</summary>
    public class Settings : ModSettings
    {
        public static bool EnableCumflation { get => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableCumflation; set => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableCumflation = value; }
        public static float GlobalCumflationModifier { get => MilkCum.Core.Settings.MilkCumSettings.Cum_GlobalCumflationModifier; set => MilkCum.Core.Settings.MilkCumSettings.Cum_GlobalCumflationModifier = value; }
        public static bool EnableStuffing { get => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableStuffing; set => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableStuffing = value; }
        public static float GlobalStuffingModifier { get => MilkCum.Core.Settings.MilkCumSettings.Cum_GlobalStuffingModifier; set => MilkCum.Core.Settings.MilkCumSettings.Cum_GlobalStuffingModifier = value; }
        public static bool EnableBukkake { get => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableBukkake; set => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableBukkake = value; }
        public static float GlobaleBukkakeModifier { get => MilkCum.Core.Settings.MilkCumSettings.Cum_GlobalBukkakeModifier; set => MilkCum.Core.Settings.MilkCumSettings.Cum_GlobalBukkakeModifier = value; }
        public static bool EnableFluidGatheringWhileCleaning { get => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableFluidGatheringWhileCleaning; set => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableFluidGatheringWhileCleaning = value; }
        public static float MaxGatheringCheckDistance { get => MilkCum.Core.Settings.MilkCumSettings.Cum_MaxGatheringCheckDistance; set => MilkCum.Core.Settings.MilkCumSettings.Cum_MaxGatheringCheckDistance = value; }
        public static bool EnableProgressingConsumptionThoughts { get => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableProgressingConsumptionThoughts; set => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableProgressingConsumptionThoughts = value; }
        public static bool EnableOscillationMechanics { get => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableOscillationMechanics; set => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableOscillationMechanics = value; }
        public static bool EnableOscillationMechanicsForAnimals { get => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableOscillationMechanicsForAnimals; set => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableOscillationMechanicsForAnimals = value; }
        public static bool EnableCumDebugLogging { get => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableDebugLogging; set => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableDebugLogging = value; }
        public static bool EnableVirtualSemenPool { get => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableVirtualSemenPool; set => MilkCum.Core.Settings.MilkCumSettings.Cum_EnableVirtualSemenPool = value; }
        public static float SemenPoolDaysForFullRefill { get => MilkCum.Core.Settings.MilkCumSettings.Cum_SemenPoolDaysForFullRefill; set => MilkCum.Core.Settings.MilkCumSettings.Cum_SemenPoolDaysForFullRefill = value; }
    }
}
