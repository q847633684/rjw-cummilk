using Verse;

namespace MilkCum.Fluids.Cum.Leaking
{
    /// <summary>数据已统一到 MilkCumSettings；此类仅作代理，供 Cumpilation 旧代码读取。</summary>
    public class Settings : ModSettings
    {
        public static bool EnableFilthGeneration { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnableFilthGeneration; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnableFilthGeneration = value; }
        public static bool EnableAutoDeflateBucket { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnableAutoDeflateBucket; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnableAutoDeflateBucket = value; }
        public static bool EnableAutoDeflateClean { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnableAutoDeflateClean; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnableAutoDeflateClean = value; }
        public static bool EnableAutoDeflateDirty { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnableAutoDeflateDirty; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnableAutoDeflateDirty = value; }
        public static bool EnablePrivacy { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnablePrivacy; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_EnablePrivacy = value; }
        public static float AutoDeflateMinSeverity { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_AutoDeflateMinSeverity; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_AutoDeflateMinSeverity = value; }
        public static float AutoDeflateMaxDistance { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_AutoDeflateMaxDistance; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_AutoDeflateMaxDistance = value; }
        public static float LeakMult { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_LeakMult; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_LeakMult = value; }
        public static float LeakRate { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_LeakRate; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_LeakRate = value; }
        public static float DeflateMult { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_DeflateMult; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_DeflateMult = value; }
        public static float DeflateRate { get => MilkCum.Core.Settings.MilkCumSettings.CumLeak_DeflateRate; set => MilkCum.Core.Settings.MilkCumSettings.CumLeak_DeflateRate = value; }

        public override void ExposeData()
        {
            base.ExposeData();
            // 数据已由 MilkCumSettings.ExposeData 统一读写
        }
    }
}
