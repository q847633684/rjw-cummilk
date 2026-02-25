using Verse;

namespace Cumpilation.Leaking
{
    /// <summary>数据已统一到 EqualMilkingSettings；此类仅作代理，供 Cumpilation 代码读取。</summary>
    public class Settings : ModSettings
    {
        public static bool EnableFilthGeneration { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnableFilthGeneration; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnableFilthGeneration = value; }
        public static bool EnableAutoDeflateBucket { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnableAutoDeflateBucket; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnableAutoDeflateBucket = value; }
        public static bool EnableAutoDeflateClean { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnableAutoDeflateClean; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnableAutoDeflateClean = value; }
        public static bool EnableAutoDeflateDirty { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnableAutoDeflateDirty; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnableAutoDeflateDirty = value; }
        public static bool EnablePrivacy { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnablePrivacy; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_EnablePrivacy = value; }
        public static float AutoDeflateMinSeverity { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_AutoDeflateMinSeverity; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_AutoDeflateMinSeverity = value; }
        public static float AutoDeflateMaxDistance { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_AutoDeflateMaxDistance; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_AutoDeflateMaxDistance = value; }
        public static float LeakMult { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_LeakMult; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_LeakMult = value; }
        public static float LeakRate { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_LeakRate; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_LeakRate = value; }
        public static float DeflateMult { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_DeflateMult; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_DeflateMult = value; }
        public static float DeflateRate { get => EqualMilking.EqualMilkingSettings.CumpilationLeak_DeflateRate; set => EqualMilking.EqualMilkingSettings.CumpilationLeak_DeflateRate = value; }

        public override void ExposeData()
        {
            base.ExposeData();
            // 数据已由 EqualMilkingSettings.ExposeData 统一读写
        }
    }
}
