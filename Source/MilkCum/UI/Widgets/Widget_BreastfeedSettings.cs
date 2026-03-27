using System.Collections.Generic;
using MilkCum.Core;
using UnityEngine;
using Verse;

using static MilkCum.Core.Constants.Constants;

namespace MilkCum.UI;

public class Widget_BreastfeedSettings
{
	private readonly HumanlikeBreastfeed humanlikeBreastfeed;
	private readonly AnimalBreastfeed animalBreastfeed;
	private readonly MechanoidBreastfeed mechanoidBreastfeed;
	private Vector2 _allTabsScrollPosition = Vector2.zero;
	private const float AllTabsContentHeight = 800f;

	public Widget_BreastfeedSettings(HumanlikeBreastfeed humanlikeBreastfeed, AnimalBreastfeed animalBreastfeed, MechanoidBreastfeed mechanoidBreastfeed)
	{
		this.humanlikeBreastfeed = humanlikeBreastfeed;
		this.animalBreastfeed = animalBreastfeed;
		this.mechanoidBreastfeed = mechanoidBreastfeed;
	}

	/// <summary>主/子 Tab 结构下：仅绘制总览（营养→能量、回缩吸收）。</summary>
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
		DrawNutritionEnergyBlock(ref inRect);
		inRect.y += UNIT_SIZE * 2;
	}

	/// <summary>营养→能量、额外饥饿、回缩吸收：Draw 与 DrawOverview 共用。</summary>
	private static void DrawNutritionEnergyBlock(ref Rect inRect)
	{
		string buffer = MilkCumSettings.nutritionToEnergyFactor.ToString();
		Widgets.TextFieldNumericLabeled(new Rect(inRect.x, inRect.y, inRect.width, UNIT_SIZE), "(" + Lang.Join(Lang.Breastfeed, Lang.Mechanoid) + ")" + Lang.Join(Lang.Nutrition, "=>", Lang.Energy, Lang.StatFactor), ref MilkCumSettings.nutritionToEnergyFactor, ref buffer, 1f);
		inRect.y += UNIT_SIZE;
		float basisF = MilkCumSettings.lactationExtraNutritionBasis;
		Widgets.HorizontalSlider(new Rect(inRect.x, inRect.y, inRect.width, UNIT_SIZE), ref basisF, new FloatRange(0f, 300f), "EM.LactationExtraNutritionFactor".Translate(MilkCumSettings.lactationExtraNutritionBasis.ToString()), 5f);
		MilkCumSettings.lactationExtraNutritionBasis = Mathf.RoundToInt(basisF);
		inRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(inRect.x, inRect.y, inRect.width, UNIT_SIZE), "EM.ReabsorbNutritionEnabled".Translate(), ref MilkCumSettings.reabsorbNutritionEnabled);
		inRect.y += UNIT_SIZE;
		float effF = MilkCumSettings.reabsorbNutritionEfficiency;
		Widgets.HorizontalSlider(new Rect(inRect.x, inRect.y, inRect.width, UNIT_SIZE), ref effF, new FloatRange(0f, 1f), "EM.ReabsorbNutritionEfficiencyLabel".Translate((MilkCumSettings.reabsorbNutritionEfficiency * 100f).ToString("F0") + "%"), 0.05f);
		MilkCumSettings.reabsorbNutritionEfficiency = Mathf.Clamp01(effF);
		inRect.y += UNIT_SIZE;
	}

	/// <summary>专业级「哺乳系统」一页：总览说明 + 营养→能量/回缩 + 人形/动物/机械三类设置（可滚动）。</summary>
	public void DrawBreastfeedSystemFull(Rect inRect)
	{
		inRect = inRect.ContractedBy(UNIT_SIZE / 2);
		float viewHeight = inRect.height;
		float contentWidth = inRect.width - 20f;
		const float contentHeight = 1000f;
		Rect scrollViewRect = new Rect(inRect.x, inRect.y, inRect.width, viewHeight);
		Rect scrollContent = new Rect(0f, 0f, contentWidth, contentHeight);
		Widgets.BeginScrollView(scrollViewRect, ref _allTabsScrollPosition, scrollContent, true);
		Rect contentRect = new Rect(0f, 0f, contentWidth, contentHeight);
		float y = 0f;
		string overviewDesc = "EM.BreastfeedOverviewDesc".Translate();
		if (!string.IsNullOrEmpty(overviewDesc))
		{
			GUI.color = Color.gray;
			Widgets.Label(new Rect(0f, y, contentWidth, UNIT_SIZE * 2), overviewDesc);
			GUI.color = Color.white;
			y += UNIT_SIZE * 2;
		}
		Rect nutritionRect = new Rect(0f, y, contentWidth, 300f);
		DrawNutritionEnergyBlock(ref nutritionRect);
		y = nutritionRect.y + UNIT_SIZE * 2;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(0f, y, contentWidth, UNIT_SIZE), Lang.Colonist.CapitalizeFirst());
		GUI.color = Color.white;
		y += UNIT_SIZE;
		SetupHumanlike(contentRect, ref y, humanlikeBreastfeed);
		y += UNIT_SIZE * 2;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(0f, y, contentWidth, UNIT_SIZE), Lang.Animal.CapitalizeFirst());
		GUI.color = Color.white;
		y += UNIT_SIZE;
		SetupAnimal(contentRect, ref y, animalBreastfeed);
		y += UNIT_SIZE * 2;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(0f, y, contentWidth, UNIT_SIZE), Lang.Mechanoid.CapitalizeFirst());
		GUI.color = Color.white;
		y += UNIT_SIZE;
		SetupMechanoid(contentRect, ref y, mechanoidBreastfeed);
		Widgets.EndScrollView();
	}

	/// <summary>合并绘制人形/动物/机械族三类哺乳设置于同一可滚动页（原三个子 Tab 合并为一个）。</summary>
	public void DrawAllTabs(Rect inRect)
	{
		inRect = inRect.ContractedBy(UNIT_SIZE / 2);
		float viewHeight = inRect.height;
		float contentWidth = inRect.width - 20f;
		Rect scrollViewRect = new Rect(inRect.x, inRect.y, inRect.width, viewHeight);
		Rect scrollContent = new Rect(0f, 0f, contentWidth, AllTabsContentHeight);
		Widgets.BeginScrollView(scrollViewRect, ref _allTabsScrollPosition, scrollContent, true);
		Rect contentRect = new Rect(0f, 0f, contentWidth, AllTabsContentHeight);
		float y = 0f;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(0f, y, contentWidth, UNIT_SIZE), Lang.Colonist.CapitalizeFirst());
		GUI.color = Color.white;
		y += UNIT_SIZE;
		SetupHumanlike(contentRect, ref y, humanlikeBreastfeed);
		y += UNIT_SIZE * 2;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(0f, y, contentWidth, UNIT_SIZE), Lang.Animal.CapitalizeFirst());
		GUI.color = Color.white;
		y += UNIT_SIZE;
		SetupAnimal(contentRect, ref y, animalBreastfeed);
		y += UNIT_SIZE * 2;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(0f, y, contentWidth, UNIT_SIZE), Lang.Mechanoid.CapitalizeFirst());
		GUI.color = Color.white;
		y += UNIT_SIZE;
		SetupMechanoid(contentRect, ref y, mechanoidBreastfeed);
		Widgets.EndScrollView();
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
		Widgets.CheckboxLabeled(new Rect(inRect.x, y_Offset, inRect.width, UNIT_SIZE), "EM.AnimalAdultFemaleAlwaysLactating".Translate(), ref MilkCumSettings.femaleAnimalAdultAlwaysLactating);
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