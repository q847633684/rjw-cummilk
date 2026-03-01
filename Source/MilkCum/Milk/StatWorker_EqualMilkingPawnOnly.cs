using RimWorld;
using Verse;

namespace MilkCum.Milk;

/// <summary>
/// 仅对 Pawn 请求显示；避免在查看 ThingDef（如药物 EM_Prolactin）信息卡时触发原版 StatWorker 的未处理分支并报 "Unhandled case"。
/// </summary>
public class StatWorker_EqualMilkingPawnOnly : StatWorker
{
	public override bool ShouldShowFor(StatRequest req)
	{
		return req.Pawn != null;
	}
}
