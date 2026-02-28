using System.Collections.Generic;
using MilkCum.Core;
using MilkCum.Milk.Data;
using MilkCum.Milk.Helpers;
using UnityEngine;
using Verse;

using static MilkCum.Milk.Helpers.Constants;

namespace MilkCum.UI;

public class Widget_BreastfeedSettings
{
	private readonly HumanlikeBreastfeed humanlikeBreastfeed;
	private readonly AnimalBreastfeed animalBreastfeed;
	private readonly MechanoidBreastfeed mechanoidBreastfeed;
	private int tabIndex = 0;
	public Widget_BreastfeedSettings(HumanlikeBreastfeed humanlikeBreastfeed, AnimalBreastfeed animalBreastfeed, MechanoidBreastfeed mechanoidBreastfeed)
	{
		this.humanlikeBreastfeed = humanlikeBreastfeed;
		this.animalBreastfeed = animalBreastfeed;
		this.mechanoidBreastfeed = mechanoidBreastfeed;
	}
	public void Draw(Rect inRect)
	{
		inRect = inRect.ContractedBy(UNIT_SIZE / 2);
		string buffer = EqualMilkingSettings.nutritionToEnergyFactor.ToString();
		Widgets.TextFieldNumericLabeled(new Rect(inRect.x, inRect.y, inRect.width, UNIT_SIZE), "(" + Lang.Join(Lang.Breastfeed, Lang.Mechanoid) + ")" + Lang.Join(Lang.Nutrition, "=>", Lang.Energy, Lang.StatFactor), ref EqualMilkingSettings.nutritionToEnergyFactor, ref buffer, 1f);
		inRect.y += UNIT_SIZE;
		float basisF = EqualMilkingSettings.lactationExtraNutritionBasis;
		Widgets.HorizontalSlider(new Rect(inRect.x, inRect.y, inRect.width, UNIT_SIZE), ref basisF, new FloatRange(0f, 300f), "EM.LactationExtraNutritionFactor".Translate(EqualMilkingSettings.lactationExtraNutritionBasis.ToString()), 5f);
		EqualMilkingSettings.lactationExtraNutritionBasis = Mathf.RoundToInt(basisF);
		inRect.y += UNIT_SIZE * 2;
		inRect.height -= UNIT_SIZE * 4;
		List<TabRecord> tabs = new()
		{
			new(Lang.Colonist.CapitalizeFirst(), () => tabIndex = 0, tabIndex == 0),
			new(Lang.Animal.CapitalizeFirst(), () => tabIndex = 1, tabIndex == 1),
			new(Lang.Mechanoid.CapitalizeFirst(), () => tabIndex = 2, tabIndex == 2)
		};
		TabDrawer.DrawTabs(inRect, tabs);
		Widgets.DrawMenuSection(inRect);
		inRect = inRect.ContractedBy(UNIT_SIZE / 2);
		inRect.width -= UNIT_SIZE;
		DrawTabContent(inRect, tabIndex);
	}

	/// <summary>主/子 Tab 结构下：仅绘制总览（营养→能量、哺乳时间）。</summary>
	public void DrawOverview(Rect inRect)
	{
		inRect = inRect.ContractedBy(UNIT_SIZE / 2);
		string overviewDesc = "EM.BreastfeedOverviewDesc".Translate();
		if (!string.IsNullOrEmpty(overviewDesc))
		{
			GUI.color = Color.gray;
			Widgets.Label(inRect, overviewDesc);
			GUI.color = Color.white;
			inRect.y += UNIT_SIZE * 2;
		}
		string bufferEnergy = EqualMilkingSettings.nutritionToEnergyFactor.ToString();
		Widgets.TextFieldNumericLabeled(new Rect(inRect.x, inRect.y, inRect.width, UNIT_SIZE), "(" + Lang.Join(Lang.Breastfeed, Lang.Mechanoid) + ")" + Lang.Join(Lang.Nutrition, "=>", Lang.Energy, Lang.StatFactor), ref EqualMilkingSettings.nutritionToEnergyFactor, ref bufferEnergy, 1f);
		inRect.y += UNIT_SIZE * 2;
		float basisOverview = EqualMilkingSettings.lactationExtraNutritionBasis;
		Widgets.HorizontalSlider(new Rect(inRect.x, inRect.y, inRect.width, UNIT_SIZE), ref basisOverview, new FloatRange(0f, 300f), "EM.LactationExtraNutritionFactor".Translate(EqualMilkingSettings.lactationExtraNutritionBasis.ToString()), 5f);
		EqualMilkingSettings.lactationExtraNutritionBasis = Mathf.RoundToInt(basisOverview);
		inRect.y += UNIT_SIZE * 2;
		string bufferTime = EqualMilkingSettings.breastfeedTime.ToString();
		Widgets.TextFieldNumericLabeled(new Rect(inRect.x, inRect.y, inRect.width, UNIT_SIZE), Lang.Join(Lang.Breastfeed, Lang.Time), ref EqualMilkingSettings.breastfeedTime, ref bufferTime, 1f);
	}

	/// <summary>主/子 Tab 结构下：仅绘制指定子 Tab 内容（0=人形，1=动物，2=机械族），不画内层 Tab 栏。</summary>
	public void DrawTab(Rect inRect, int index)
	{
		inRect = inRect.ContractedBy(UNIT_SIZE / 2);
		float y = inRect.y;
		DrawTabContent(inRect, index);
	}

	private void DrawTabContent(Rect inRect, int tabIndex)
	{
		float y_Offset = inRect.y;
		if (tabIndex == 0)
			SetupHumanlike(inRect, ref y_Offset, humanlikeBreastfeed);
		else if (tabIndex == 1)
			SetupAnimal(inRect, ref y_Offset, animalBreastfeed);
		else if (tabIndex == 2)
			SetupMechanoid(inRect, ref y_Offset, mechanoidBreastfeed);
	}
	private void SetupBreastfeed(Rect inRect, ref float y_Offset, Breastfeed breastfeed)
	{
		Widgets.CheckboxLabeled(new Rect(inRect.x, y_Offset, inRect.width, UNIT_SIZE), Lang.Join(Lang.Allow, Lang.Breastfeed), ref breastfeed.AllowBreastfeeding);
		y_Offset += UNIT_SIZE;
		if (!breastfeed.AllowBreastfeeding) { return; }
		Widgets.CheckboxLabeled(new Rect(inRect.x + UNIT_SIZE, y_Offset, inRect.width, UNIT_SIZE), Lang.Join(Lang.Breastfeed, Lang.Colonist), ref breastfeed.BreastfeedHumanlike);
		y_Offset += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(inRect.x + UNIT_SIZE, y_Offset, inRect.width, UNIT_SIZE), Lang.Join(Lang.Breastfeed, Lang.Animal), ref breastfeed.BreastfeedAnimal);
		y_Offset += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(inRect.x + UNIT_SIZE, y_Offset, inRect.width, UNIT_SIZE), Lang.Join(Lang.Breastfeed, Lang.Mechanoid), ref breastfeed.BreastfeedMechanoid);
		y_Offset += UNIT_SIZE;
	}
	private void SetupHumanlike(Rect inRect, ref float y_Offset, HumanlikeBreastfeed breastfeed)
	{
		SetupBreastfeed(inRect, ref y_Offset, breastfeed);
		Widgets.CheckboxLabeled(new Rect(inRect.x + UNIT_SIZE, y_Offset, inRect.width, UNIT_SIZE), Lang.Join(Lang.Overseer, Lang.Breastfeed, Lang.Mechanoid), ref breastfeed.OverseerBreastfeed);
		y_Offset += UNIT_SIZE;
	}
	private void SetupAnimal(Rect inRect, ref float y_Offset, AnimalBreastfeed breastfeed)
	{
		SetupBreastfeed(inRect, ref y_Offset, breastfeed);
		string buffer = breastfeed.BabyAge.ToString();
		Widgets.TextFieldNumericLabeled(new Rect(inRect.x, y_Offset, inRect.width, UNIT_SIZE), Lang.Join(Lang.Baby, Lang.Age).CapitalizeFirst(), ref breastfeed.BabyAge, ref buffer, 0f);
		y_Offset += UNIT_SIZE;
	}
	private void SetupMechanoid(Rect inRect, ref float y_Offset, MechanoidBreastfeed breastfeed)
	{
		SetupBreastfeed(inRect, ref y_Offset, breastfeed);
		string buffer = breastfeed.BabyAge.ToString();
		Widgets.TextFieldNumericLabeled(new Rect(inRect.x, y_Offset, inRect.width, UNIT_SIZE), Lang.Join(Lang.Baby, Lang.Age).CapitalizeFirst(), ref breastfeed.BabyAge, ref buffer, 0f);
		y_Offset += UNIT_SIZE;
	}
}