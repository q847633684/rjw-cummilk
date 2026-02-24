using System.Collections.Generic;
using Verse;
using UnityEngine;
using EqualMilking.Helpers;
using EqualMilking.Data;
using static EqualMilking.Helpers.Constants;

namespace EqualMilking.UI;

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
		inRect.y += UNIT_SIZE * 2;
		inRect.height -= UNIT_SIZE * 2;
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
		float y_Offset = inRect.y;
		if (tabIndex == 0)
		{
			SetupHumanlike(inRect, ref y_Offset, humanlikeBreastfeed);
		}
		else if (tabIndex == 1)
		{
			SetupAnimal(inRect, ref y_Offset, animalBreastfeed);
		}
		else if (tabIndex == 2)
		{
			SetupMechanoid(inRect, ref y_Offset, mechanoidBreastfeed);
		}
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