using rjw;
using Verse;

namespace MilkCum.Fluids.Cum.Leaking;

internal static class LeakingUtility
{
	/// <summary>获取目标 pawn 阴道部位平均松弛度（无阴道时返回 0）。</summary>
	public static float GetAverageVaginalLooseness(Pawn pawn)
	{
		if (pawn == null)
			return 0f;

		float sum = 0f;
		int count = 0;
		foreach (Hediff part in Genital_Helper.get_AllPartsHediffList(pawn))
		{
			if (!Genital_Helper.is_vagina(part))
				continue;
			sum += part.Severity;
			count++;
		}
		return count > 0 ? (sum / count) : 0f;
	}
}
