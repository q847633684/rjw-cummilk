using System.Collections.Generic;
using MilkCum.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>`MilkCumSettings` 的设置窗口状态与 UI 渲染分块。</summary>
internal partial class MilkCumSettings
{
	private int mainTabIndex = 0;
	private int subTabIndex = 0;
	private static readonly float unitSize = 32f;

	private static Widget_MilkableTable milkableTable;
	private static Widget_MilkTagsTable milkTagsTable;
	private static Widget_AdvancedSettings advancedSettings;
	private static Widget_BreastfeedSettings breastfeedSettings;
	private static Widget_GeneSetting geneSetting;
	private static Widget_DefaultSetting defaultSettingWidget;
	private static Widget_CumpilationSettings cumpilationSettings;
	private static Widget_RaceOverrides raceOverridesWidget;

	private static void EnsureScribeDeepTypes()
	{
		if (_risk == null || _risk.GetType() != typeof(MilkRiskSettings)) _risk = new MilkRiskSettings();
		if (humanlikeBreastfeed == null || humanlikeBreastfeed.GetType() != typeof(HumanlikeBreastfeed)) humanlikeBreastfeed = new HumanlikeBreastfeed();
		if (animalBreastfeed == null || animalBreastfeed.GetType() != typeof(AnimalBreastfeed)) animalBreastfeed = new AnimalBreastfeed();
		if (mechanoidBreastfeed == null || mechanoidBreastfeed.GetType() != typeof(MechanoidBreastfeed)) mechanoidBreastfeed = new MechanoidBreastfeed();
		if (colonistSetting == null || colonistSetting.GetType() != typeof(MilkSettings)) colonistSetting = new MilkSettings();
		if (slaveSetting == null || slaveSetting.GetType() != typeof(MilkSettings)) slaveSetting = new MilkSettings();
		if (prisonerSetting == null || prisonerSetting.GetType() != typeof(MilkSettings)) prisonerSetting = new MilkSettings();
		if (animalSetting == null || animalSetting.GetType() != typeof(MilkSettings)) animalSetting = new MilkSettings();
		if (mechSetting == null || mechSetting.GetType() != typeof(MilkSettings)) mechSetting = new MilkSettings();
		if (entitySetting == null || entitySetting.GetType() != typeof(MilkSettings)) entitySetting = new MilkSettings();
		genes ??= new List<Gene_MilkTypeData>();
	}

	public void DoWindowContents(Rect inRect)
	{
		EnsureScribeDeepTypes();
		inRect.yMin += unitSize;

		pawnDefs ??= GetMilkablePawns();
		defaultMilkProducts ??= GetDefaultMilkProducts();
		humanlikeBreastfeed ??= new HumanlikeBreastfeed();
		animalBreastfeed ??= new AnimalBreastfeed();
		mechanoidBreastfeed ??= new MechanoidBreastfeed();
		colonistSetting ??= new MilkSettings();
		slaveSetting ??= new MilkSettings();
		prisonerSetting ??= new MilkSettings();
		animalSetting ??= new MilkSettings();
		mechSetting ??= new MilkSettings();
		entitySetting ??= new MilkSettings();

		List<TabRecord> mainTabs = new()
		{
			new("EM.Tab.CoreSystems".Translate(), () => { mainTabIndex = (int)MainTabIndex.CoreSystems; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.CoreSystems),
			new("EM.Tab.HealthRisk".Translate(), () => { mainTabIndex = (int)MainTabIndex.HealthRisk; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.HealthRisk),
			new("EM.Tab.Permissions".Translate(), () => { mainTabIndex = (int)MainTabIndex.Permissions; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Permissions),
			new("EM.Tab.Balance".Translate(), () => { mainTabIndex = (int)MainTabIndex.Balance; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Balance),
			new("EM.Tab.Integrations".Translate(), () => { mainTabIndex = (int)MainTabIndex.Integrations; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Integrations),
			new("EM.Tab.DataRaces".Translate(), () => { mainTabIndex = (int)MainTabIndex.DataRaces; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.DataRaces)
		};
		if (Prefs.DevMode)
			mainTabs.Add(new TabRecord("EM.Tab.DevTools".Translate(), () => { mainTabIndex = (int)MainTabIndex.DevTools; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.DevTools));
		if (mainTabIndex >= mainTabs.Count) mainTabIndex = 0;
		TabDrawer.DrawTabs(inRect, mainTabs);
		inRect.yMin += unitSize;

		List<TabRecord> subTabs = GetSubTabs();
		if (subTabIndex >= subTabs.Count) subTabIndex = 0;
		if (subTabs.Count > 0)
		{
			TabDrawer.DrawTabs(inRect, subTabs);
			inRect.yMin += unitSize;
		}

		Widgets.DrawMenuSection(inRect);
		Rect contentRect = inRect.ContractedBy(unitSize / 2);

		bool useAdvancedSettings = mainTabIndex == (int)MainTabIndex.HealthRisk || mainTabIndex == (int)MainTabIndex.Permissions
			|| mainTabIndex == (int)MainTabIndex.Balance || mainTabIndex == (int)MainTabIndex.Integrations || mainTabIndex == (int)MainTabIndex.DevTools;
		if (useAdvancedSettings) advancedSettings ??= new Widget_AdvancedSettings();

		switch (mainTabIndex)
		{
			case (int)MainTabIndex.CoreSystems:
				breastfeedSettings ??= new Widget_BreastfeedSettings(humanlikeBreastfeed, animalBreastfeed, mechanoidBreastfeed);
				cumpilationSettings ??= new Widget_CumpilationSettings();
				if (subTabIndex == 0) breastfeedSettings.DrawBreastfeedSystemFull(contentRect);
				else if (subTabIndex == 1) cumpilationSettings.Draw(contentRect);
				else
				{
					GUI.color = Color.gray;
					Widgets.Label(contentRect, "EM.SubTab.FluidsGirlJuicePlaceholder".Translate());
					GUI.color = Color.white;
				}
				break;
			case (int)MainTabIndex.HealthRisk:
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.HealthRisk, subTabIndex);
				break;
			case (int)MainTabIndex.Permissions:
				defaultSettingWidget ??= new Widget_DefaultSetting(colonistSetting, slaveSetting, prisonerSetting, animalSetting, mechSetting, entitySetting);
				if (subTabIndex == 0) advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Permissions, 0);
				else defaultSettingWidget.Draw(contentRect);
				break;
			case (int)MainTabIndex.Balance:
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Balance, 0);
				break;
			case (int)MainTabIndex.Integrations:
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Integrations, subTabIndex);
				break;
			case (int)MainTabIndex.DataRaces:
				milkableTable ??= new Widget_MilkableTable(namesToProducts);
				milkTagsTable ??= new Widget_MilkTagsTable(namesToProducts, productsToTags);
				raceOverridesWidget ??= new Widget_RaceOverrides();
				geneSetting ??= new Widget_GeneSetting(genes);
				if (subTabIndex == 0) milkableTable.Draw(contentRect);
				else if (subTabIndex == 1) milkTagsTable.Draw(contentRect);
				else if (subTabIndex == 2) raceOverridesWidget.Draw(contentRect);
				else geneSetting.Draw(contentRect);
				break;
			case (int)MainTabIndex.DevTools:
				advancedSettings.DrawDevModeSection(contentRect);
				break;
		}
	}

	private List<TabRecord> GetSubTabs()
	{
		switch (mainTabIndex)
		{
			case (int)MainTabIndex.CoreSystems:
				return new List<TabRecord>
				{
					new("EM.SubTab.BreastfeedSystem".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.CumSystem".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.FluidBehavior".Translate(), () => subTabIndex = 2, subTabIndex == 2)
				};
			case (int)MainTabIndex.HealthRisk:
				return new List<TabRecord>
				{
					new("EM.SubTab.MastitisSystem".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.HygieneSystem".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.ToleranceSystem".Translate(), () => subTabIndex = 2, subTabIndex == 2),
					new("EM.SubTab.OverflowPollution".Translate(), () => subTabIndex = 3, subTabIndex == 3)
				};
			case (int)MainTabIndex.Permissions:
				return new List<TabRecord>
				{
					new("EM.SubTab.MenuVisibility".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.DefaultBehavior".Translate(), () => subTabIndex = 1, subTabIndex == 1)
				};
			case (int)MainTabIndex.Balance:
				return new List<TabRecord> { new("EM.SubTab.BalanceScaling".Translate(), () => subTabIndex = 0, subTabIndex == 0) };
			case (int)MainTabIndex.Integrations:
				return new List<TabRecord>
				{
					new("EM.SubTab.RJWIntegration".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.DBHIntegration".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.NutritionSystem".Translate(), () => subTabIndex = 2, subTabIndex == 2)
				};
			case (int)MainTabIndex.DataRaces:
				return new List<TabRecord>
				{
					new("EM.SubTab.Milk".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.MilkTags".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.RaceOverrides".Translate(), () => subTabIndex = 2, subTabIndex == 2),
					new("EM.SubTab.GenesAndAdvanced".Translate(), () => subTabIndex = 3, subTabIndex == 3)
				};
			case (int)MainTabIndex.DevTools:
			default:
				return new List<TabRecord>();
		}
	}
}
