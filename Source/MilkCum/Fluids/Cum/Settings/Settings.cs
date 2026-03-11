using Verse;

namespace Cumpilation
{
    /// <summary>数据已统一到 MilkCumSettings；此类仅作代理，供 Cumpilation 代码读取。</summary>
    public class Settings : ModSettings
    {
        public static bool EnableCumflation { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableCumflation; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableCumflation = value; }
        public static float GlobalCumflationModifier { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_GlobalCumflationModifier; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_GlobalCumflationModifier = value; }
        public static bool EnableStuffing { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableStuffing; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableStuffing = value; }
        public static float GlobalStuffingModifier { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_GlobalStuffingModifier; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_GlobalStuffingModifier = value; }
        public static bool EnableBukkake { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableBukkake; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableBukkake = value; }
        public static float GlobaleBukkakeModifier { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_GlobalBukkakeModifier; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_GlobalBukkakeModifier = value; }
        public static bool EnableFluidGatheringWhileCleaning { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableFluidGatheringWhileCleaning; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableFluidGatheringWhileCleaning = value; }
        public static float MaxGatheringCheckDistance { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_MaxGatheringCheckDistance; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_MaxGatheringCheckDistance = value; }
        public static bool EnableProgressingConsumptionThoughts { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableProgressingConsumptionThoughts; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableProgressingConsumptionThoughts = value; }
        public static bool EnableOscillationMechanics { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableOscillationMechanics; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableOscillationMechanics = value; }
        public static bool EnableOscillationMechanicsForAnimals { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableOscillationMechanicsForAnimals; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableOscillationMechanicsForAnimals = value; }
        public static bool EnableCumpilationDebugLogging { get => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableDebugLogging; set => MilkCum.Core.Settings.MilkCumSettings.Cumpilation_EnableDebugLogging = value; }
    }
}
