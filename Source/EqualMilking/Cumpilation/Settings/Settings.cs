using Verse;

namespace Cumpilation
{
    /// <summary>数据已统一到 EqualMilkingSettings；此类仅作代理，供 Cumpilation 代码读取。</summary>
    public class Settings : ModSettings
    {
        public static bool EnableCumflation { get => EqualMilking.EqualMilkingSettings.Cumpilation_EnableCumflation; set => EqualMilking.EqualMilkingSettings.Cumpilation_EnableCumflation = value; }
        public static float GlobalCumflationModifier { get => EqualMilking.EqualMilkingSettings.Cumpilation_GlobalCumflationModifier; set => EqualMilking.EqualMilkingSettings.Cumpilation_GlobalCumflationModifier = value; }
        public static bool EnableStuffing { get => EqualMilking.EqualMilkingSettings.Cumpilation_EnableStuffing; set => EqualMilking.EqualMilkingSettings.Cumpilation_EnableStuffing = value; }
        public static float GlobalStuffingModifier { get => EqualMilking.EqualMilkingSettings.Cumpilation_GlobalStuffingModifier; set => EqualMilking.EqualMilkingSettings.Cumpilation_GlobalStuffingModifier = value; }
        public static bool EnableBukkake { get => EqualMilking.EqualMilkingSettings.Cumpilation_EnableBukkake; set => EqualMilking.EqualMilkingSettings.Cumpilation_EnableBukkake = value; }
        public static float GlobaleBukkakeModifier { get => EqualMilking.EqualMilkingSettings.Cumpilation_GlobalBukkakeModifier; set => EqualMilking.EqualMilkingSettings.Cumpilation_GlobalBukkakeModifier = value; }
        public static bool EnableFluidGatheringWhileCleaning { get => EqualMilking.EqualMilkingSettings.Cumpilation_EnableFluidGatheringWhileCleaning; set => EqualMilking.EqualMilkingSettings.Cumpilation_EnableFluidGatheringWhileCleaning = value; }
        public static float MaxGatheringCheckDistance { get => EqualMilking.EqualMilkingSettings.Cumpilation_MaxGatheringCheckDistance; set => EqualMilking.EqualMilkingSettings.Cumpilation_MaxGatheringCheckDistance = value; }
        public static bool EnableProgressingConsumptionThoughts { get => EqualMilking.EqualMilkingSettings.Cumpilation_EnableProgressingConsumptionThoughts; set => EqualMilking.EqualMilkingSettings.Cumpilation_EnableProgressingConsumptionThoughts = value; }
        public static bool EnableOscillationMechanics { get => EqualMilking.EqualMilkingSettings.Cumpilation_EnableOscillationMechanics; set => EqualMilking.EqualMilkingSettings.Cumpilation_EnableOscillationMechanics = value; }
        public static bool EnableOscillationMechanicsForAnimals { get => EqualMilking.EqualMilkingSettings.Cumpilation_EnableOscillationMechanicsForAnimals; set => EqualMilking.EqualMilkingSettings.Cumpilation_EnableOscillationMechanicsForAnimals = value; }
        public static bool EnableCumpilationDebugLogging { get => EqualMilking.EqualMilkingSettings.Cumpilation_EnableDebugLogging; set => EqualMilking.EqualMilkingSettings.Cumpilation_EnableDebugLogging = value; }

        public override void ExposeData()
        {
            base.ExposeData();
            // 数据已由 EqualMilkingSettings.ExposeData 统一读写，此处仅保留空实现以兼容 ModSettings 基类
        }
    }
}
