using System;
using Verse;

namespace Milk
{
	public class CompProperties_MilkableHuman : CompProperties
	{
		public float milkIntervalDays = 1f;

		public float milkAmount = 6f;

        public float milkAmountBase = 2f;

        public ThingDef milkDef;

		public bool milkFemaleOnly = true;

        public CompProperties_MilkableHuman()
		{
			this.compClass = typeof(CompMilkableHuman);
		}
	}
}
