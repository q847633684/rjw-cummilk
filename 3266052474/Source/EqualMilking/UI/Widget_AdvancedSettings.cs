using Verse;
using RimWorld;
using UnityEngine;
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
		sliderRect.y += UNIT_SIZE * 2;
		Widgets.HorizontalSlider(sliderRect, ref EqualMilkingSettings.lactatingEfficiencyMultiplierPerStack, new FloatRange(0.01f, 5f), Lang.Join(HediffDefOf.Lactating.label, Lang.Efficiency, Lang.StatFactor) + ": " + EqualMilkingSettings.lactatingEfficiencyMultiplierPerStack.ToString(), 0.01f);
		sliderRect.y += UNIT_SIZE * 2;
		Widgets.HorizontalSlider(sliderRect, ref EqualMilkingSettings.milkAmountMultiplierPerStack, new FloatRange(0.01f, 5f), Lang.Join(Lang.MilkAmount, Lang.StatFactor) + ": " + EqualMilkingSettings.milkAmountMultiplierPerStack.ToString(), 0.01f);
		sliderRect.y += UNIT_SIZE * 2;
		Widgets.HorizontalSlider(sliderRect, ref EqualMilkingSettings.hungerRateMultiplierPerStack, new FloatRange(0f, 5f), Lang.Join(Lang.HungerRate, Lang.StatFactor) + ": " + EqualMilkingSettings.hungerRateMultiplierPerStack.ToString(), 0.01f);
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
	}
}