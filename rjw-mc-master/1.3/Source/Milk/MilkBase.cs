using System;
using HugsLib;
using HugsLib.Settings;
using Verse;

namespace Milk
{
	public class MilkBase : ModBase
	{
		public override string ModIdentifier
		{
			get
			{
				return "MilkableColonists";
			}
		}

		public static SettingHandle<bool> breastGrowthDisabled;
		public static SettingHandle<bool> ignoreReproductiveStage;
		public static SettingHandle<bool> flatChestGivesMilk;

		public static HediffDef Orassan_Lactating_Permanent = null;

		public override void DefsLoaded()
		{
			breastGrowthDisabled = Settings.GetHandle("breastGrowthDisabled", Translator.Translate("MilkableColonists.breastGrowthDisabled"), Translator.Translate("MilkableColonists.breastGrowthDisabledDesc"), false);
			flatChestGivesMilk = Settings.GetHandle("flatChestGivesMilk", Translator.Translate("MilkableColonists.flatChestGivesMilk"), Translator.Translate("MilkableColonists.flatChestGivesMilkDesc"), false);
			ignoreReproductiveStage = Settings.GetHandle("ignoreReproductiveStage", Translator.Translate("MilkableColonists.ignoreReproductiveStage"), Translator.Translate("MilkableColonists.ignoreReproductiveStageDesc"), false);

			Orassan_Lactating_Permanent = DefDatabase<HediffDef>.GetNamedSilentFail("Orassan_Lactating_Permanent");
		}
	}
}
