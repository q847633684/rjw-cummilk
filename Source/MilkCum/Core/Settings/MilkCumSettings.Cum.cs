using Verse;

namespace MilkCum.Core.Settings;

/// <summary>`MilkCumSettings` 的 Cum/Leak 配置分块与序列化。</summary>
internal partial class MilkCumSettings
{
	// Cum/Leak：序列化键见 ExposeCumData（MC2.EM.*）。
	public static bool Cum_EnableCumflation = true;
	public static float Cum_GlobalCumflationModifier = 1.0f;
	public static bool Cum_EnableStuffing = true;
	public static float Cum_GlobalStuffingModifier = 1.0f;
	public static bool Cum_EnableBukkake = true;
	public static float Cum_GlobalBukkakeModifier = 1.0f;
	public static bool Cum_EnableFluidGatheringWhileCleaning = true;
	public static float Cum_MaxGatheringCheckDistance = 15.0f;
	public static bool Cum_EnableProgressingConsumptionThoughts = true;
	public static bool Cum_EnableOscillationMechanics = true;
	public static bool Cum_EnableOscillationMechanicsForAnimals = false;
	public static bool Cum_EnableDebugLogging = false;
	/// <summary>启用左/右虚拟睾丸精液池：全身射精器官汇入两侧，射精量受池存量限制并在游戏日内回充。</summary>
	public static bool Cum_EnableVirtualSemenPool = true;
	/// <summary>自空槽回满至容量所需的「游戏内日」时间（流速与 RJW 部位倍率相乘）。</summary>
	public static float Cum_SemenPoolDaysForFullRefill = 1f;

	public static bool CumLeak_EnableFilthGeneration = true;
	public static bool CumLeak_EnableAutoDeflateBucket = false;
	public static bool CumLeak_EnableAutoDeflateClean = false;
	public static bool CumLeak_EnableAutoDeflateDirty = false;
	public static bool CumLeak_EnablePrivacy = true;
	public static float CumLeak_AutoDeflateMinSeverity = 0.4f;
	public static float CumLeak_AutoDeflateMaxDistance = 100f;
	public static float CumLeak_LeakMult = 5.0f;
	public static float CumLeak_LeakRate = 1.0f;
	public static float CumLeak_DeflateMult = 5.0f;
	public static float CumLeak_DeflateRate = 1.0f;

	private static void ExposeCumData()
	{
		Scribe_Values.Look(ref Cum_EnableCumflation, "MC2.EM.Cum.EnableCumflation", true);
		Scribe_Values.Look(ref Cum_GlobalCumflationModifier, "MC2.EM.Cum.GlobalCumflationModifier", 1.0f);
		Scribe_Values.Look(ref Cum_EnableStuffing, "MC2.EM.Cum.EnableStuffing", true);
		Scribe_Values.Look(ref Cum_GlobalStuffingModifier, "MC2.EM.Cum.GlobalStuffingModifier", 1.0f);
		Scribe_Values.Look(ref Cum_EnableBukkake, "MC2.EM.Cum.EnableBukkake", true);
		Scribe_Values.Look(ref Cum_GlobalBukkakeModifier, "MC2.EM.Cum.GlobalBukkakeModifier", 1.0f);
		Scribe_Values.Look(ref Cum_EnableFluidGatheringWhileCleaning, "MC2.EM.Cum.EnableFluidGatheringWhileCleaning", true);
		Scribe_Values.Look(ref Cum_MaxGatheringCheckDistance, "MC2.EM.Cum.MaxGatheringCheckDistance", 15.0f);
		Scribe_Values.Look(ref Cum_EnableProgressingConsumptionThoughts, "MC2.EM.Cum.EnableProgressingConsumptionThoughts", true);
		Scribe_Values.Look(ref Cum_EnableOscillationMechanics, "MC2.EM.Cum.EnableOscillationMechanics", true);
		Scribe_Values.Look(ref Cum_EnableOscillationMechanicsForAnimals, "MC2.EM.Cum.EnableOscillationMechanicsForAnimals", false);
		Scribe_Values.Look(ref Cum_EnableDebugLogging, "MC2.EM.Cum.EnableDebugLogging", false);
		Scribe_Values.Look(ref Cum_EnableVirtualSemenPool, "MC2.EM.Cum.EnableVirtualSemenPool", true);
		Scribe_Values.Look(ref Cum_SemenPoolDaysForFullRefill, "MC2.EM.Cum.SemenPoolDaysForFullRefill", 1f);
		Scribe_Values.Look(ref CumLeak_EnableFilthGeneration, "MC2.EM.Cum.Leak.EnableFilthGeneration", true);
		Scribe_Values.Look(ref CumLeak_EnableAutoDeflateBucket, "MC2.EM.Cum.Leak.EnableAutoDeflateBucket", false);
		Scribe_Values.Look(ref CumLeak_EnableAutoDeflateClean, "MC2.EM.Cum.Leak.EnableAutoDeflateClean", false);
		Scribe_Values.Look(ref CumLeak_EnableAutoDeflateDirty, "MC2.EM.Cum.Leak.EnableAutoDeflateDirty", false);
		Scribe_Values.Look(ref CumLeak_EnablePrivacy, "MC2.EM.Cum.Leak.EnablePrivacy", true);
		Scribe_Values.Look(ref CumLeak_AutoDeflateMinSeverity, "MC2.EM.Cum.Leak.AutoDeflateMinSeverity", 0.4f);
		Scribe_Values.Look(ref CumLeak_AutoDeflateMaxDistance, "MC2.EM.Cum.Leak.AutoDeflateMaxDistance", 100f);
		Scribe_Values.Look(ref CumLeak_LeakMult, "MC2.EM.Cum.Leak.LeakMult", 5.0f);
		Scribe_Values.Look(ref CumLeak_LeakRate, "MC2.EM.Cum.Leak.LeakRate", 1.0f);
		Scribe_Values.Look(ref CumLeak_DeflateMult, "MC2.EM.Cum.Leak.DeflateMult", 5.0f);
		Scribe_Values.Look(ref CumLeak_DeflateRate, "MC2.EM.Cum.Leak.DeflateRate", 1.0f);
	}
}
