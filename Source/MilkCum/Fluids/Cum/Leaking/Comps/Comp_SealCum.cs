using System.Collections.Generic;
using System.Linq;
using Verse;
using rjw;

namespace MilkCum.Fluids.Cum.Leaking
{
	/// <summary>阴道相关「塞住 / 是否允许自动泄精」状态；交互在指派限制窗口，不再占用小人底部 Gizmo。</summary>
	public class Comp_SealCum : ThingComp
	{
		private bool cumSealed;
		private bool canDeflate = true;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref cumSealed, "cumSealed", false);
			Scribe_Values.Look(ref canDeflate, "canDeflate", true);
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			// 基类迭代保留；塞住/泄精开关在 Window_ProducerRestrictions 中操作。
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
				yield return gizmo;
		}

		public bool CanDeflate()
		{
			return canDeflate;
		}

		public void SetSealed(bool value) => cumSealed = value;

		public void SetCanDeflate(bool value) => canDeflate = value;

		public bool PlayerControlled
		{
			get
			{
				Pawn pawn = (Pawn)parent;
				if (pawn.IsColonist)
				{
					if (pawn.HostFaction != null)
						return pawn.IsSlave;
					return true;
				}
				return false;
			}
		}

		public bool IsSealed()
		{
			return canSeal() && cumSealed;
		}

		public bool canSeal()
		{
			Pawn pawn = (Pawn)parent;

			if (pawn.Dead)
				return false;

			var GenitalBPR = Genital_Helper.get_genitalsBPR(pawn);
			IEnumerable<ISexPartHediff> vaginas = Genital_Helper.get_PartsHediffList(pawn, GenitalBPR).Where(x => Genital_Helper.is_vagina(x)).Cast<ISexPartHediff>();
			if (!vaginas.Any())
				return false;
			// 与指派窗口一致：有人类生殖器阴道即允许「塞住」状态参与泄漏逻辑。
			return true;
		}
	}
}
