using Verse;

namespace Cumpilation
{
    /// <summary>数据已统一到 EqualMilkingSettings；此类仅作代理，供 Cumpilation 代码读取。</summary>
    public class Settings : ModSettings
    {
        public static bool EnableCumflation { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableCumflation; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableCumflation = value; }
        public static float GlobalCumflationModifier { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_GlobalCumflationModifier; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_GlobalCumflationModifier = value; }
        public static bool EnableStuffing { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableStuffing; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableStuffing = value; }
        public static float GlobalStuffingModifier { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_GlobalStuffingModifier; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_GlobalStuffingModifier = value; }
        public static bool EnableBukkake { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableBukkake; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableBukkake = value; }
        public static float GlobaleBukkakeModifier { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_GlobalBukkakeModifier; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_GlobalBukkakeModifier = value; }
        public static bool EnableFluidGatheringWhileCleaning { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableFluidGatheringWhileCleaning; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableFluidGatheringWhileCleaning = value; }
        public static float MaxGatheringCheckDistance { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_MaxGatheringCheckDistance; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_MaxGatheringCheckDistance = value; }
        public static bool EnableProgressingConsumptionThoughts { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableProgressingConsumptionThoughts; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableProgressingConsumptionThoughts = value; }
        public static bool EnableOscillationMechanics { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableOscillationMechanics; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableOscillationMechanics = value; }
        public static bool EnableOscillationMechanicsForAnimals { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableOscillationMechanicsForAnimals; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableOscillationMechanicsForAnimals = value; }
        public static bool EnableCumpilationDebugLogging { get => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableDebugLogging; set => MilkCum.Core.EqualMilkingSettings.Cumpilation_EnableDebugLogging = value; }

        public override void ExposeData()
        {
            base.ExposeData();
            // 数据已由 EqualMilkingSettings.ExposeData 统一读写，此处仅保留空实现以兼容 ModSettings 基类
        }
    }
}
