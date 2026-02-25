using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using EqualMilking.Helpers;
using static EqualMilking.Helpers.Constants;

namespace EqualMilking.UI;
public class Widget_AdvancedSettings
{
	public void Draw(Rect inRect)
	{
		Rect sliderRect = new(inRect.x, inRect.y, inRect.width, UNIT_SIZE);
		float fStacks = (float)EqualMilkingSettings.maxLactationStacks;
		Widgets.HorizontalSlider(sliderRect, ref fStacks, new FloatRange(1, 10), Lang.Join(HediffDefOf.Lactating.label, Lang.InstallImplantAlreadyMaxLevel) + ": " + ((int)fStacks).ToString(), 1f);
		EqualMilkingSettings.maxLactationStacks = (int)fStacks;
		sliderRect.y += UNIT_SIZE;
		GUI.color = Color.gray;
		GameFont prevFont = Text.Font;
		Text.Font = GameFont.Tiny;
		Widgets.Label(sliderRect, "（已弃用，仅兼容旧存档）");
		Text.Font = prevFont;
		GUI.color = Color.white;
		sliderRect.y += UNIT_SIZE;
		Widgets.HorizontalSlider(sliderRect, ref EqualMilkingSettings.lactatingEfficiencyMultiplierPerStack, new FloatRange(0.01f, 5f), Lang.Join(HediffDefOf.Lactating.label, Lang.Efficiency, Lang.StatFactor) + ": " + EqualMilkingSettings.lactatingEfficiencyMultiplierPerStack.ToString(), 0.01f);
		sliderRect.y += UNIT_SIZE * 2;
		Widgets.HorizontalSlider(sliderRect, ref EqualMilkingSettings.milkAmountMultiplierPerStack, new FloatRange(0.01f, 5f), Lang.Join(Lang.MilkAmount, Lang.StatFactor) + ": " + EqualMilkingSettings.milkAmountMultiplierPerStack.ToString(), 0.01f);
		sliderRect.y += UNIT_SIZE * 2;
		Widgets.HorizontalSlider(sliderRect, ref EqualMilkingSettings.hungerRateMultiplierPerStack, new FloatRange(0f, 5f), Lang.Join(Lang.HungerRate, Lang.StatFactor) + ": " + EqualMilkingSettings.hungerRateMultiplierPerStack.ToString(), 0.01f);
		sliderRect.y += UNIT_SIZE * 2;
		// 泌乳期意识/操纵/移动增益
		Rect rLactatingGain = new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE);
		Widgets.CheckboxLabeled(rLactatingGain, "EM.LactatingGain".Translate(), ref EqualMilkingSettings.lactatingGainEnabled, false);
		TooltipHandler.TipRegion(rLactatingGain, "EM.LactatingGainDesc".Translate());
		sliderRect.y += UNIT_SIZE;
		float pct = EqualMilkingSettings.lactatingGainCapModPercent;
		Widgets.HorizontalSlider(sliderRect, ref pct, new FloatRange(0f, 0.20f), "EM.LactatingGainPercent".Translate(pct.ToStringPercent()), 0.01f);
		EqualMilkingSettings.lactatingGainCapModPercent = pct;
		sliderRect.y += UNIT_SIZE * 2;
		// 谁可以吸奶 / 谁可使用奶制品：改为按产奶者指定，在奶表格「指定」列设置
		GUI.color = Color.gray;
		Widgets.Label(sliderRect, "EM.ProducerRestrictionsHint".Translate());
		GUI.color = Color.white;
		sliderRect.y += UNIT_SIZE * 2;
		// 7.9: 药物分工提示
		GUI.color = Color.gray;
		Widgets.Label(sliderRect, "EM.DrugRoleHint".Translate());
		GUI.color = Color.white;
		sliderRect.y += UNIT_SIZE * 2;
		string s_breastfeedTime = EqualMilkingSettings.breastfeedTime.ToString();
		Widgets.TextFieldNumericLabeled(sliderRect, Lang.Join(Lang.Breastfeed, Lang.Time), ref EqualMilkingSettings.breastfeedTime, ref s_breastfeedTime, 1f);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE), Lang.Animal.CapitalizeFirst() + ": " + Lang.Join(Lang.AnimalFemaleAdult, Lang.Always, Lang.Lactating), ref EqualMilkingSettings.femaleAnimalAdultAlwaysLactating);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE), Lang.Menu + ": " + Lang.GiveTo(EMDefOf.EM_Prolactin.label, Lang.Mechanoid), ref EqualMilkingSettings.showMechOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE), Lang.Menu + ": " + Lang.GiveTo(EMDefOf.EM_Prolactin.label, Lang.Colonist), ref EqualMilkingSettings.showColonistOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE), Lang.Menu + ": " + Lang.GiveTo(EMDefOf.EM_Prolactin.label, Lang.Slave), ref EqualMilkingSettings.showSlaveOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE), Lang.Menu + ": " + Lang.GiveTo(EMDefOf.EM_Prolactin.label, Lang.Prisoner), ref EqualMilkingSettings.showPrisonerOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE), Lang.Menu + ": " + Lang.GiveTo(EMDefOf.EM_Prolactin.label, Lang.Animal), ref EqualMilkingSettings.showAnimalOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE), Lang.Menu + ": " + Lang.GiveTo(EMDefOf.EM_Prolactin.label, Lang.Misc), ref EqualMilkingSettings.showMiscOptions);
		sliderRect.y += UNIT_SIZE;
		// RJW 联动（仅当 RJW 激活时显示）
		if (ModLister.GetModWithIdentifier("rim.job.world") != null)
		{
			sliderRect.y += UNIT_SIZE;
			GUI.color = Color.gray;
			Widgets.Label(sliderRect, "EM.RJWSection".Translate());
			GUI.color = Color.white;
			sliderRect.y += UNIT_SIZE;
			Rect rRjwBreast = new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwBreast, "EM.RjwBreastSize".Translate(), ref EqualMilkingSettings.rjwBreastSizeEnabled, false);
			TooltipHandler.TipRegion(rRjwBreast, "EM.RjwBreastSizeDesc".Translate());
			sliderRect.y += UNIT_SIZE;
			Rect rRjwLust = new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwLust, "EM.RjwLustFromNursing".Translate(), ref EqualMilkingSettings.rjwLustFromNursingEnabled, false);
			TooltipHandler.TipRegion(rRjwLust, "EM.RjwLustFromNursingDesc".Translate());
			sliderRect.y += UNIT_SIZE;
			Rect rRjwNeed = new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwNeed, "EM.RjwSexNeedLactatingBonus".Translate(), ref EqualMilkingSettings.rjwSexNeedLactatingBonusEnabled, false);
			TooltipHandler.TipRegion(rRjwNeed, "EM.RjwSexNeedLactatingBonusDesc".Translate());
			sliderRect.y += UNIT_SIZE;
			Rect rRjwSat = new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwSat, "EM.RjwSexSatisfactionAfterNursing".Translate(), ref EqualMilkingSettings.rjwSexSatisfactionAfterNursingEnabled, false);
			TooltipHandler.TipRegion(rRjwSat, "EM.RjwSexSatisfactionAfterNursingDesc".Translate());
			sliderRect.y += UNIT_SIZE;
			float fert = EqualMilkingSettings.rjwLactationFertilityFactor;
			Widgets.HorizontalSlider(sliderRect, ref fert, new FloatRange(0f, 1f), "EM.RjwLactationFertility".Translate(fert.ToStringPercent()), 0.05f);
			EqualMilkingSettings.rjwLactationFertilityFactor = fert;
			sliderRect.y += UNIT_SIZE;
			Rect rRjwInSex = new Rect(sliderRect.x, sliderRect.y, inRect.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwInSex, "EM.RjwLactatingInSexDesc".Translate(), ref EqualMilkingSettings.rjwLactatingInSexDescriptionEnabled, false);
			TooltipHandler.TipRegion(rRjwInSex, "EM.RjwLactatingInSexDescDesc".Translate());
		}
	}
}