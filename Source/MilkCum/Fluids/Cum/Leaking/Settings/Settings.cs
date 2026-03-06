using Verse;

namespace Cumpilation.Leaking
{
    /// <summary>数据已统一到 MilkCumSettings；此类仅作代理，供 Cumpilation 代码读取。</summary>
    public class Settings : ModSettings
    {
        public static bool EnableFilthGeneration { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnableFilthGeneration; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnableFilthGeneration = value; }
        public static bool EnableAutoDeflateBucket { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnableAutoDeflateBucket; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnableAutoDeflateBucket = value; }
        public static bool EnableAutoDeflateClean { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnableAutoDeflateClean; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnableAutoDeflateClean = value; }
        public static bool EnableAutoDeflateDirty { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnableAutoDeflateDirty; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnableAutoDeflateDirty = value; }
        public static bool EnablePrivacy { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnablePrivacy; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_EnablePrivacy = value; }
        public static float AutoDeflateMinSeverity { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_AutoDeflateMinSeverity; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_AutoDeflateMinSeverity = value; }
        public static float AutoDeflateMaxDistance { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_AutoDeflateMaxDistance; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_AutoDeflateMaxDistance = value; }
        public static float LeakMult { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_LeakMult; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_LeakMult = value; }
        public static float LeakRate { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_LeakRate; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_LeakRate = value; }
        public static float DeflateMult { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_DeflateMult; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_DeflateMult = value; }
        public static float DeflateRate { get => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_DeflateRate; set => MilkCum.Core.Settings.MilkCumSettings.CumpilationLeak_DeflateRate = value; }

        public override void ExposeData()
        {
            base.ExposeData();
            // 数据已由 MilkCumSettings.ExposeData 统一读写
        }
    }
}
