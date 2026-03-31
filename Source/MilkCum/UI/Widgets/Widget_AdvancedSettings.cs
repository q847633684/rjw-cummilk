using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Core.Utils;
using RimWorld;
using UnityEngine;
using Verse;

using static MilkCum.Core.Constants.Constants;

namespace MilkCum.UI;
public class Widget_AdvancedSettings
{
	private static string RjwBreastPoolCapacityModeLabel(RjwBreastPoolCapacityMode mode) => mode switch
	{
		RjwBreastPoolCapacityMode.RjwBreastWeight => "EM.RjwCapacityMode_Weight".Translate(),
		RjwBreastPoolCapacityMode.RjwBreastVolume => "EM.RjwCapacityMode_Volume".Translate(),
		_ => "EM.RjwCapacityMode_Severity".Translate(),
	};

	private static string RjwBreastPoolTopologyModeLabel(RjwBreastPoolTopologyMode mode) => mode switch
	{
		RjwBreastPoolTopologyMode.RjwChestUnified => "EM.RjwTopology_ChestUnified".Translate(),
		RjwBreastPoolTopologyMode.PerAnatomicalLeaf => "EM.RjwTopology_PerLeaf".Translate(),
		_ => "EM.RjwTopology_VirtualLR".Translate(),
	};

	/// <summary>Fixed height for settings section scroll content. If users report truncation on very high DPI or many options, consider content-based height or ListView (if RimWorld API supports it).</summary>
	private const float SectionScrollContentHeight = 3200f;

	private Vector2 _sectionScrollPosition = Vector2.zero;

	// 健康子 Tab 索引（专业级：4 个 = 乳腺炎 / 卫生 / 耐受 / 溢出）
	private const int SubTabHealth_Mastitis = 0;
	private const int SubTabHealth_DBH = 1;
	private const int SubTabHealth_Tolerance = 2;
	private const int SubTabHealth_Overflow = 3;
	// 效率与界面
	private const int SubTabEfficiency_IdentityAndMenu = 0;
	private const int SubTabEfficiency_BreastPool = 1;
	// 模组联动：RJW / DBH / 营养系统
	private const int SubTabIntegration_RJW = 0;
	private const int SubTabIntegration_DBH = 1;
	private const int SubTabIntegration_Nutrition = 2;

	/// <summary>专业级 7 主 Tab：按 mainTab 分发到健康/权限/数值/联动等区块。</summary>
	public void DrawSection(Rect inRect, int mainTab, int subTab)
	{
		float topMargin = 36f;
		Rect scrollViewRect = new Rect(inRect.x, inRect.y + topMargin, inRect.width, inRect.height - topMargin);
		Rect scrollContent = new Rect(0f, 0f, scrollViewRect.width - 20f, SectionScrollContentHeight);
		Widgets.BeginScrollView(scrollViewRect, ref _sectionScrollPosition, scrollContent, true);
		try
		{
			if (mainTab == (int)MainTabIndex.HealthRisk)
				DrawHealthSection(scrollContent, subTab);
			else if (mainTab == (int)MainTabIndex.Permissions && subTab == 0)
				DrawEfficiencyOrInterfaceSection(scrollContent, SubTabEfficiency_IdentityAndMenu);
			else if (mainTab == (int)MainTabIndex.Balance)
				DrawEfficiencyOrInterfaceSection(scrollContent, SubTabEfficiency_BreastPool);
			else if (mainTab == (int)MainTabIndex.Integrations)
				DrawIntegrationSectionExtended(scrollContent, subTab);
		}
		finally
		{
			Widgets.EndScrollView();
		}
	}

	/// <summary>DevMode: selected pawn lactation debug. Only when playing and map active; avoids InvalidCastException in Mod settings.</summary>
	public void DrawDevModeSection(Rect inRect)
	{
		if (!Prefs.DevMode) return;
		var list = new Listing_Standard { maxOneColumn = true, ColumnWidth = inRect.width - 20f };
		list.Begin(inRect);
		list.Gap(6f);
		list.Label("EM.DevModeLactationPanel".Translate());
		list.Gap(4f);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.LactationLog".Translate(), ref MilkCumSettings.lactationLog);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.LactationPoolTickLog".Translate(), ref MilkCumSettings.lactationPoolTickLog);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.LactationDrugIntakeLog".Translate(), ref MilkCumSettings.lactationDrugIntakeLog);
		list.Gap(4f);
		if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null)
		{
			list.Label("EM.DevModeNoGame".Translate());
			list.End();
			return;
		}
		Pawn sel = Find.Selector.SingleSelectedThing as Pawn;
		if (sel == null)
			list.Label("EM.DevModeNoPawnSelected".Translate());
		else
		{
			var hediff = sel.LactatingHediffWithComps();
			var comp = hediff?.comps?.OfType<HediffComp_EqualMilkingLactating>().FirstOrDefault();
			if (comp == null)
				list.Label("EM.DevModeNotLactating".Translate(sel.LabelShort));
			else
			{
				string debug = comp.CompDebugString();
				var milkComp = sel.CompEquallyMilkable();
				if (milkComp != null)
				{
					int now = Find.TickManager?.TicksGame ?? -1;
					int age = milkComp.IsCachedFlowValid() && now >= 0 ? now - milkComp.CachedFlowTick : -1;
					debug += $"\n\n[MilkCum.Cache]\n" +
					         $"  TotalFlowPerDay = {milkComp.CachedFlowPerDayForDisplay:F3} (age {age} ticks)\n" +
					         $"  Pressure = {milkComp.CachedPressureForDisplay:F3}, Letdown = {milkComp.CachedLetdownForDisplay:F3}, Conditions = {milkComp.CachedConditionsForDisplay:F3}";
					debug += "\n\n" + milkComp.BuildDuctDebugString();
				}
				float h = Text.CalcHeight(debug, list.ColumnWidth);
				Rect rDebug = list.GetRect(Mathf.Min(h, 200f));
				Widgets.Label(rDebug, debug);
			}
		}
		list.End();
	}

	private void DrawHealthSection(Rect content, int subTab)
	{
		var list = new Listing_Standard();
		list.Begin(content);
		if (subTab == SubTabHealth_Mastitis)
			DrawMastitisBlock(list);
		else if (subTab == SubTabHealth_DBH)
			DrawDbhBlock(list);
		else if (subTab == SubTabHealth_Tolerance)
			DrawToleranceBlock(list);
		else if (subTab == SubTabHealth_Overflow)
			DrawOverflowBlock(list);
		list.End();
	}

	private void DrawMastitisBlock(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_Mastitis".Translate());
		GUI.color = Color.white;
		list.Gap(4f);
		list.Label("EM.MastitisAndToleranceSection".Translate());
		list.Gap(4f);
		Rect rAllowMastitis = list.GetRect(UNIT_SIZE);
		bool allowMastitis = MilkCumSettings.allowMastitis;
		Widgets.CheckboxLabeled(rAllowMastitis, "EM.AllowMastitis".Translate(), ref allowMastitis, false);
		MilkCumSettings.allowMastitis = allowMastitis;
		TooltipHandler.TipRegion(rAllowMastitis, "EM.AllowMastitisDesc".Translate());
		list.Gap(6f);
		Rect rInflamEnable = list.GetRect(UNIT_SIZE);
		bool enableInflam = MilkCumSettings.enableInflammationModel;
		Widgets.CheckboxLabeled(rInflamEnable, "EM.EnableInflammationModel".Translate(), ref enableInflam, false);
		MilkCumSettings.enableInflammationModel = enableInflam;
		TooltipHandler.TipRegion(rInflamEnable, "EM.EnableInflammationModelDesc".Translate());
		list.Gap(4f);
		GUI.color = Color.gray;
		list.Label("EM.InflammationModelSection".Translate());
		GUI.color = Color.white;
		bool inflamGui = GUI.enabled;
		GUI.enabled = MilkCumSettings.enableInflammationModel;
		list.Gap(4f);
		Rect rIa = list.GetRect(UNIT_SIZE);
		float ia = MilkCumSettings.inflammationAlpha;
		Widgets.HorizontalSlider(rIa, ref ia, new FloatRange(0f, 8f), "EM.InflammationAlpha".Translate(MilkCumSettings.inflammationAlpha.ToString("F2")), 0.05f);
		MilkCumSettings.inflammationAlpha = ia;
		TooltipHandler.TipRegion(rIa, "EM.InflammationAlphaDesc".Translate());
		list.Gap(4f);
		Rect rIb = list.GetRect(UNIT_SIZE);
		float ib = MilkCumSettings.inflammationBeta;
		Widgets.HorizontalSlider(rIb, ref ib, new FloatRange(0f, 1f), "EM.InflammationBeta".Translate(MilkCumSettings.inflammationBeta.ToString("F2")), 0.02f);
		MilkCumSettings.inflammationBeta = ib;
		TooltipHandler.TipRegion(rIb, "EM.InflammationBetaDesc".Translate());
		list.Gap(4f);
		Rect rIg = list.GetRect(UNIT_SIZE);
		float ig = MilkCumSettings.inflammationGamma;
		Widgets.HorizontalSlider(rIg, ref ig, new FloatRange(0f, 1f), "EM.InflammationGamma".Translate(MilkCumSettings.inflammationGamma.ToString("F2")), 0.02f);
		MilkCumSettings.inflammationGamma = ig;
		TooltipHandler.TipRegion(rIg, "EM.InflammationGammaDesc".Translate());
		list.Gap(4f);
		Rect rIr = list.GetRect(UNIT_SIZE);
		float ir = MilkCumSettings.inflammationRho;
		Widgets.HorizontalSlider(rIr, ref ir, new FloatRange(0.001f, 0.5f), "EM.InflammationRho".Translate(MilkCumSettings.inflammationRho.ToString("F3")), 0.005f);
		MilkCumSettings.inflammationRho = ir;
		TooltipHandler.TipRegion(rIr, "EM.InflammationRhoDesc".Translate());
		list.Gap(4f);
		Rect rIc = list.GetRect(UNIT_SIZE);
		float ic = MilkCumSettings.inflammationCrit;
		Widgets.HorizontalSlider(rIc, ref ic, new FloatRange(0.2f, 3f), "EM.InflammationCrit".Translate(MilkCumSettings.inflammationCrit.ToString("F2")), 0.05f);
		MilkCumSettings.inflammationCrit = ic;
		TooltipHandler.TipRegion(rIc, "EM.InflammationCritDesc".Translate());
		list.Gap(4f);
		Rect rIst = list.GetRect(UNIT_SIZE);
		float ist = MilkCumSettings.inflammationStasisFullnessThreshold;
		Widgets.HorizontalSlider(rIst, ref ist, new FloatRange(0.5f, 0.99f), "EM.InflammationStasisFullnessThreshold".Translate(MilkCumSettings.inflammationStasisFullnessThreshold.ToString("F2")), 0.01f);
		MilkCumSettings.inflammationStasisFullnessThreshold = ist;
		TooltipHandler.TipRegion(rIst, "EM.InflammationStasisFullnessThresholdDesc".Translate());
		list.Gap(4f);
		Rect rIse = list.GetRect(UNIT_SIZE);
		float ise = MilkCumSettings.inflammationStasisExponent;
		Widgets.HorizontalSlider(rIse, ref ise, new FloatRange(1f, 4f), "EM.InflammationStasisExponent".Translate(MilkCumSettings.inflammationStasisExponent.ToString("F1")), 0.1f);
		MilkCumSettings.inflammationStasisExponent = ise;
		TooltipHandler.TipRegion(rIse, "EM.InflammationStasisExponentDesc".Translate());
		list.Gap(4f);
		Rect rIhb = list.GetRect(UNIT_SIZE);
		float ihb = MilkCumSettings.inflammationHygieneBaselineFactor;
		Widgets.HorizontalSlider(rIhb, ref ihb, new FloatRange(0f, 1f), "EM.InflammationHygieneBaselineFactor".Translate(MilkCumSettings.inflammationHygieneBaselineFactor.ToString("F2")), 0.02f);
		MilkCumSettings.inflammationHygieneBaselineFactor = ihb;
		TooltipHandler.TipRegion(rIhb, "EM.InflammationHygieneBaselineFactorDesc".Translate());
		list.Gap(4f);
		Rect rIds = list.GetRect(UNIT_SIZE);
		float ids = MilkCumSettings.inflammationDrainReliefScale;
		Widgets.HorizontalSlider(rIds, ref ids, new FloatRange(0f, 2f), "EM.InflammationDrainReliefScale".Translate(MilkCumSettings.inflammationDrainReliefScale.ToString("F2")), 0.02f);
		MilkCumSettings.inflammationDrainReliefScale = ids;
		TooltipHandler.TipRegion(rIds, "EM.InflammationDrainReliefScaleDesc".Translate());
		list.Gap(4f);
		Rect rIdm = list.GetRect(UNIT_SIZE);
		float idm = MilkCumSettings.inflammationDrainReliefMaxPerEvent;
		Widgets.HorizontalSlider(rIdm, ref idm, new FloatRange(0f, 0.5f), "EM.InflammationDrainReliefMaxPerEvent".Translate(MilkCumSettings.inflammationDrainReliefMaxPerEvent.ToString("F2")), 0.01f);
		MilkCumSettings.inflammationDrainReliefMaxPerEvent = idm;
		TooltipHandler.TipRegion(rIdm, "EM.InflammationDrainReliefMaxPerEventDesc".Translate());
		GUI.enabled = inflamGui;
		list.Gap(6f);
		Rect rMtb = list.GetRect(UNIT_SIZE);
		float mastitisBaseMtbDays = MilkCumSettings.mastitisBaseMtbDays;
		Widgets.HorizontalSlider(rMtb, ref mastitisBaseMtbDays, new FloatRange(0.2f, 10f), "EM.MastitisBaseMtbDays".Translate(MilkCumSettings.mastitisBaseMtbDays.ToString("F1")), 0.1f);
		MilkCumSettings.mastitisBaseMtbDays = mastitisBaseMtbDays;
		list.Gap(6f);
		Rect rOverFull = list.GetRect(UNIT_SIZE);
		float overFullnessRiskMultiplier = MilkCumSettings.overFullnessRiskMultiplier;
		Widgets.HorizontalSlider(rOverFull, ref overFullnessRiskMultiplier, new FloatRange(0.5f, 5f), "EM.OverFullnessRiskMultiplier".Translate(MilkCumSettings.overFullnessRiskMultiplier.ToString("F1")), 0.1f);
		MilkCumSettings.overFullnessRiskMultiplier = overFullnessRiskMultiplier;
		list.Gap(6f);
		Rect rHygiene = list.GetRect(UNIT_SIZE);
		float hygieneRiskMultiplier = MilkCumSettings.hygieneRiskMultiplier;
		Widgets.HorizontalSlider(rHygiene, ref hygieneRiskMultiplier, new FloatRange(0.5f, 3f), "EM.HygieneRiskMultiplier".Translate(MilkCumSettings.hygieneRiskMultiplier.ToString("F1")), 0.1f);
		MilkCumSettings.hygieneRiskMultiplier = hygieneRiskMultiplier;
		list.Gap(6f);
		Rect rInfection = list.GetRect(UNIT_SIZE);
		float mastitisInfectionRiskFactor = MilkCumSettings.mastitisInfectionRiskFactor;
		Widgets.HorizontalSlider(rInfection, ref mastitisInfectionRiskFactor, new FloatRange(1f, 3f), "EM.MastitisInfectionRiskFactor".Translate(MilkCumSettings.mastitisInfectionRiskFactor.ToString("F1")), 0.05f);
		MilkCumSettings.mastitisInfectionRiskFactor = mastitisInfectionRiskFactor;
		TooltipHandler.TipRegion(rInfection, "EM.MastitisInfectionRiskFactorDesc".Translate());
		list.Gap(6f);
		Rect rMtbHuman = list.GetRect(UNIT_SIZE);
		float mastitisMtbDaysMultiplierHumanlike = MilkCumSettings.mastitisMtbDaysMultiplierHumanlike;
		Widgets.HorizontalSlider(rMtbHuman, ref mastitisMtbDaysMultiplierHumanlike, new FloatRange(0.1f, 3f), "EM.MastitisMtbDaysMultiplierHumanlike".Translate(MilkCumSettings.mastitisMtbDaysMultiplierHumanlike.ToString("F2")), 0.05f);
		MilkCumSettings.mastitisMtbDaysMultiplierHumanlike = mastitisMtbDaysMultiplierHumanlike;
		TooltipHandler.TipRegion(rMtbHuman, "EM.MastitisMtbDaysMultiplierHumanlikeDesc".Translate());
		list.Gap(6f);
		Rect rMtbAnimal = list.GetRect(UNIT_SIZE);
		float mastitisMtbDaysMultiplierAnimal = MilkCumSettings.mastitisMtbDaysMultiplierAnimal;
		Widgets.HorizontalSlider(rMtbAnimal, ref mastitisMtbDaysMultiplierAnimal, new FloatRange(0.1f, 3f), "EM.MastitisMtbDaysMultiplierAnimal".Translate(MilkCumSettings.mastitisMtbDaysMultiplierAnimal.ToString("F2")), 0.05f);
		MilkCumSettings.mastitisMtbDaysMultiplierAnimal = mastitisMtbDaysMultiplierAnimal;
		TooltipHandler.TipRegion(rMtbAnimal, "EM.MastitisMtbDaysMultiplierAnimalDesc".Translate());
	}

	private void DrawDbhBlock(Listing_Standard list)
	{
		if (!DubsBadHygieneIntegration.IsDubsBadHygieneActive()) return;
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_DBH".Translate());
		GUI.color = Color.white;
		list.Gap(4f);
		list.Label("EM.DubsBadHygieneSection".Translate());
		list.Gap(4f);
		Rect rDbhMastitis = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rDbhMastitis, "EM.UseDubsBadHygieneForMastitis".Translate(), ref MilkCumSettings.useDubsBadHygieneForMastitis, false);
		string t = "EM.UseDubsBadHygieneForMastitisDesc".Translate();
		TooltipHandler.TipRegion(rDbhMastitis, string.IsNullOrEmpty(t) ? "EM.UseDubsBadHygieneForMastitisDesc" : t);
	}

	private void DrawToleranceBlock(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_ToleranceOverflow".Translate());
		GUI.color = Color.white;
		list.Gap(4f);
		Rect rToleranceAffect = list.GetRect(UNIT_SIZE);
		bool allowToleranceAffectMilk = MilkCumSettings.allowToleranceAffectMilk;
		Widgets.CheckboxLabeled(rToleranceAffect, "EM.AllowToleranceAffectMilk".Translate(), ref allowToleranceAffectMilk, false);
		MilkCumSettings.allowToleranceAffectMilk = allowToleranceAffectMilk;
		TooltipHandler.TipRegion(rToleranceAffect, "EM.AllowToleranceAffectMilkDesc".Translate());
		list.Gap(6f);
		Rect rExp = list.GetRect(UNIT_SIZE);
		float toleranceFlowImpactExponent = MilkCumSettings.toleranceFlowImpactExponent;
		Widgets.HorizontalSlider(rExp, ref toleranceFlowImpactExponent, new FloatRange(0.1f, 3f), "EM.ToleranceFlowImpactExponent".Translate(MilkCumSettings.toleranceFlowImpactExponent.ToString("F1")), 0.1f);
		MilkCumSettings.toleranceFlowImpactExponent = toleranceFlowImpactExponent;
	}

	private void DrawOverflowBlock(Listing_Standard list)
	{
		list.Label("EM.OverflowFilthSection".Translate());
		list.Gap(4f);
		string overflowFilth = MilkCumSettings.overflowFilthDefName ?? "";
		Rect rFilth = list.GetRect(UNIT_SIZE);
		Widgets.Label(rFilth.LeftHalf(), "EM.OverflowFilthDefName".Translate());
		overflowFilth = Widgets.TextField(rFilth.RightHalf(), overflowFilth, 64);
		MilkCumSettings.overflowFilthDefName = overflowFilth?.Trim() ?? "Filth_Vomit";
		list.Gap(4f);
		Rect rFullPoolLetter = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rFullPoolLetter, "EM.EnableFullPoolLetter".Translate(), ref MilkCumSettings.enableFullPoolLetter, false);
		TooltipHandler.TipRegion(rFullPoolLetter, "EM.EnableFullPoolLetterDesc".Translate());
		list.Gap(4f);
		Rect rCooldown = list.GetRect(UNIT_SIZE);
		float cooldownDays = MilkCumSettings.fullPoolLetterCooldownDays;
		Widgets.HorizontalSlider(rCooldown, ref cooldownDays, new FloatRange(0.5f, 7f), "EM.FullPoolLetterCooldownDays".Translate(cooldownDays.ToString("F1")), 0.5f);
		MilkCumSettings.fullPoolLetterCooldownDays = cooldownDays;
		TooltipHandler.TipRegion(rCooldown, "EM.FullPoolLetterCooldownDaysDesc".Translate());
		list.Gap(6f);
		GUI.color = Color.gray;
		list.Label("EM.BreastPoolParamsInToleranceHint".Translate());
		GUI.color = Color.white;
		list.Gap(6f);
		Rect rAiFullness = list.GetRect(UNIT_SIZE);
		bool aiPreferHighFullnessTargets = MilkCumSettings.aiPreferHighFullnessTargets;
		Widgets.CheckboxLabeled(rAiFullness, "EM.AiPreferHighFullnessTargets".Translate(), ref aiPreferHighFullnessTargets, false);
		MilkCumSettings.aiPreferHighFullnessTargets = aiPreferHighFullnessTargets;
		TooltipHandler.TipRegion(rAiFullness, "EM.AiPreferHighFullnessTargetsDesc".Translate());
	}

	private void DrawEfficiencyOrInterfaceSection(Rect content, int subTab)
	{
		var list = new Listing_Standard();
		list.Begin(content);
		if (subTab == SubTabEfficiency_IdentityAndMenu)
		{
			GUI.color = Color.gray;
			list.Label("EM.SectionDesc_IdentityAndMenu".Translate());
			GUI.color = Color.white;
			list.Gap(4f);
			GUI.color = Color.gray;
			list.Label("EM.ProducerRestrictionsHint".Translate());
			GUI.color = Color.white;
			list.Gap(6f);
			GUI.color = Color.gray;
			list.Label("EM.DefaultSucklerRulesSection".Translate());
			GUI.color = Color.white;
			list.Gap(4f);
			Rect rChild = list.GetRect(UNIT_SIZE);
			Widgets.CheckboxLabeled(rChild, "EM.DefaultSucklerIncludeChildren".Translate(), ref MilkCumSettings.defaultSucklerIncludeChildren, false);
			TooltipHandler.TipRegion(rChild, "EM.DefaultSucklerIncludeChildrenDesc".Translate());
			Rect rLover = list.GetRect(UNIT_SIZE);
			Widgets.CheckboxLabeled(rLover, "EM.DefaultSucklerIncludeLover".Translate(), ref MilkCumSettings.defaultSucklerIncludeLover, false);
			TooltipHandler.TipRegion(rLover, "EM.DefaultSucklerIncludeLoverDesc".Translate());
			Rect rSpouse = list.GetRect(UNIT_SIZE);
			Widgets.CheckboxLabeled(rSpouse, "EM.DefaultSucklerIncludeSpouse".Translate(), ref MilkCumSettings.defaultSucklerIncludeSpouse, false);
			TooltipHandler.TipRegion(rSpouse, "EM.DefaultSucklerIncludeSpouseDesc".Translate());
			Rect rExclParent = list.GetRect(UNIT_SIZE);
			Widgets.CheckboxLabeled(rExclParent, "EM.DefaultSucklerExcludeParents".Translate(), ref MilkCumSettings.defaultSucklerExcludeParents, false);
			TooltipHandler.TipRegion(rExclParent, "EM.DefaultSucklerExcludeParentsDesc".Translate());
			list.Gap(6f);
			list.Label("EM.DrugRoleHint".Translate());
			list.Gap(6f);
			Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Mechanoid), ref MilkCumSettings.showMechOptions);
			Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Colonist), ref MilkCumSettings.showColonistOptions);
			Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Slave), ref MilkCumSettings.showSlaveOptions);
			Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Prisoner), ref MilkCumSettings.showPrisonerOptions);
			Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Animal), ref MilkCumSettings.showAnimalOptions);
			Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Misc), ref MilkCumSettings.showMiscOptions);
		}
		else
			DrawBreastPoolBlock(list);
		list.End();
	}

	/// <summary>专业级：RJW / DBH / 营养系统 三子 Tab。</summary>
	private void DrawIntegrationSectionExtended(Rect content, int subTab)
	{
		var list = new Listing_Standard();
		list.Begin(content);
		if (subTab == SubTabIntegration_RJW)
		{
			if (ModLister.GetModWithIdentifier("rim.job.world") != null)
			{
				GUI.color = Color.gray;
				list.Label("EM.SectionDesc_RJW".Translate());
				GUI.color = Color.white;
				list.Gap(4f);
				GUI.color = Color.gray;
				list.Label("EM.RJWSection".Translate());
				GUI.color = Color.white;
				list.Gap(4f);
				DrawRjwBlock(list);
			}
			else
			{
				GUI.color = Color.gray;
				list.Label("EM.RequiresRJWMod".Translate());
				GUI.color = Color.white;
			}
		}
		else if (subTab == SubTabIntegration_DBH)
			DrawDbhBlock(list);
		else if (subTab == SubTabIntegration_Nutrition)
			DrawNutritionBlock(list);
		list.End();
	}

	private void DrawNutritionBlock(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_NutritionIntegration".Translate());
		GUI.color = Color.white;
		list.Gap(4f);
		string buffer = MilkCumSettings.nutritionToEnergyFactor.ToString();
		Widgets.TextFieldNumericLabeled(list.GetRect(UNIT_SIZE), "(" + Lang.Join(Lang.Breastfeed, Lang.Mechanoid) + ")" + Lang.Join(Lang.Nutrition, "=>", Lang.Energy, Lang.StatFactor), ref MilkCumSettings.nutritionToEnergyFactor, ref buffer, 1f);
		list.Gap(4f);
		float basisF = MilkCumSettings.lactationExtraNutritionBasis;
		Widgets.HorizontalSlider(list.GetRect(UNIT_SIZE), ref basisF, new FloatRange(0f, 300f), "EM.LactationExtraNutritionFactor".Translate(MilkCumSettings.lactationExtraNutritionBasis.ToString()), 5f);
		MilkCumSettings.lactationExtraNutritionBasis = Mathf.RoundToInt(basisF);
		list.Gap(4f);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.ReabsorbNutritionEnabled".Translate(), ref MilkCumSettings.reabsorbNutritionEnabled);
		list.Gap(4f);
		float effF = MilkCumSettings.reabsorbNutritionEfficiency;
		Widgets.HorizontalSlider(list.GetRect(UNIT_SIZE), ref effF, new FloatRange(0f, 1f), "EM.ReabsorbNutritionEfficiencyLabel".Translate((MilkCumSettings.reabsorbNutritionEfficiency * 100f).ToString("F0") + "%"), 0.05f);
		MilkCumSettings.reabsorbNutritionEfficiency = Mathf.Clamp01(effF);
	}

	/// <summary>Breast and pool: capacity, fill time, remaining days; tunable params with descriptions.</summary>
	private void DrawBreastPoolBlock(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_BreastPool".Translate());
		GUI.color = Color.white;
		list.Gap(6f);
		// 鍙屾睜鏁版嵁鏉ユ簮锛氬嬀閫?= RJW 涔虫埧灏哄涓庢祦閫燂紝涓嶅嬀 = 浜哄舰瀵圭О鍙屾睜
		GUI.color = Color.gray;
		list.Label("EM.RjwBreastSizeSectionHint".Translate());
		GUI.color = Color.white;
		list.Gap(2f);
		Rect rRjwBreast = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rRjwBreast, "EM.RjwBreastSize".Translate(), ref MilkCumSettings.rjwBreastSizeEnabled, false);
		{ string t = "EM.RjwBreastSizeDesc".Translate(); TooltipHandler.TipRegion(rRjwBreast, string.IsNullOrEmpty(t) ? "EM.RjwBreastSizeDesc" : t); }
		list.Gap(4f);
		Rect rCapMode = list.GetRect(UNIT_SIZE);
		if (Widgets.ButtonText(rCapMode, "EM.RjwBreastPoolCapacityMode".Translate(RjwBreastPoolCapacityModeLabel(MilkCumSettings.rjwBreastPoolCapacityMode))))
		{
			var opts = new List<FloatMenuOption>
			{
				new FloatMenuOption("EM.RjwCapacityMode_Severity".Translate(), () => MilkCumSettings.rjwBreastPoolCapacityMode = RjwBreastPoolCapacityMode.Severity),
				new FloatMenuOption("EM.RjwCapacityMode_Weight".Translate(), () => MilkCumSettings.rjwBreastPoolCapacityMode = RjwBreastPoolCapacityMode.RjwBreastWeight),
				new FloatMenuOption("EM.RjwCapacityMode_Volume".Translate(), () => MilkCumSettings.rjwBreastPoolCapacityMode = RjwBreastPoolCapacityMode.RjwBreastVolume),
			};
			Find.WindowStack.Add(new FloatMenu(opts));
		}
		TooltipHandler.TipRegion(rCapMode, "EM.RjwBreastPoolCapacityModeTip".Translate());
		list.Gap(4f);
		Rect rTopo = list.GetRect(UNIT_SIZE);
		if (Widgets.ButtonText(rTopo, "EM.RjwBreastPoolTopologyMode".Translate(RjwBreastPoolTopologyModeLabel(MilkCumSettings.rjwBreastPoolTopologyMode))))
		{
			var topoOpts = new List<FloatMenuOption>
			{
				new FloatMenuOption("EM.RjwTopology_ChestUnified".Translate(), () => MilkCumSettings.rjwBreastPoolTopologyMode = RjwBreastPoolTopologyMode.RjwChestUnified),
				new FloatMenuOption("EM.RjwTopology_VirtualLR".Translate(), () => MilkCumSettings.rjwBreastPoolTopologyMode = RjwBreastPoolTopologyMode.VirtualLeftRight),
				new FloatMenuOption("EM.RjwTopology_PerLeaf".Translate(), () => MilkCumSettings.rjwBreastPoolTopologyMode = RjwBreastPoolTopologyMode.PerAnatomicalLeaf),
			};
			Find.WindowStack.Add(new FloatMenu(topoOpts));
		}
		TooltipHandler.TipRegion(rTopo, "EM.RjwBreastPoolTopologyModeTip".Translate());
		list.Gap(4f);
		Rect rCapCoeff = list.GetRect(UNIT_SIZE);
		float capCoeff = MilkCumSettings.rjwBreastCapacityCoefficient;
		Widgets.HorizontalSlider(rCapCoeff, ref capCoeff, new FloatRange(0.25f, 4f), "EM.RjwBreastCapacityCoefficient".Translate(MilkCumSettings.rjwBreastCapacityCoefficient.ToString("F2")), 0.05f);
		MilkCumSettings.rjwBreastCapacityCoefficient = capCoeff;
		TooltipHandler.TipRegion(rCapCoeff, "EM.RjwBreastCapacityCoefficientDescLong".Translate());
		list.Gap(4f);
		Rect rNipplePct = list.GetRect(UNIT_SIZE);
		float nipplePct = MilkCumSettings.rjwNippleStageFlowBonusPercent;
		Widgets.HorizontalSlider(rNipplePct, ref nipplePct, new FloatRange(-15f, 15f), "EM.RjwNippleStageFlowBonusPct".Translate(nipplePct.ToString("F1")), 0.5f);
		MilkCumSettings.rjwNippleStageFlowBonusPercent = nipplePct;
		TooltipHandler.TipRegion(rNipplePct, "EM.RjwNippleStageFlowBonusPctDesc".Translate());
		list.Gap(4f);
		Rect rLactSev = list.GetRect(UNIT_SIZE);
		float lactSev = MilkCumSettings.rjwLactatingSeverityBonus;
		Widgets.HorizontalSlider(rLactSev, ref lactSev, new FloatRange(0f, 1f), "EM.RjwLactatingSeverityBonus".Translate(lactSev.ToString("F2")), 0.01f);
		MilkCumSettings.rjwLactatingSeverityBonus = lactSev;
		TooltipHandler.TipRegion(rLactSev, "EM.RjwLactatingSeverityBonusDesc".Translate());
		list.Gap(4f);
		Rect rStretchSev = list.GetRect(UNIT_SIZE);
		float stretchSev = MilkCumSettings.rjwLactatingStretchSeverityBonus;
		Widgets.HorizontalSlider(rStretchSev, ref stretchSev, new FloatRange(0f, 1f), "EM.RjwLactatingStretchSeverityBonus".Translate(stretchSev.ToString("F2")), 0.01f);
		MilkCumSettings.rjwLactatingStretchSeverityBonus = stretchSev;
		TooltipHandler.TipRegion(rStretchSev, "EM.RjwLactatingStretchSeverityBonusDesc".Translate());
		list.Gap(6f);
		// 流速倍率
		Rect rFlowMult = list.GetRect(UNIT_SIZE);
		float defaultFlowMultiplierForHumanlike = MilkCumSettings.defaultFlowMultiplierForHumanlike;
		Widgets.HorizontalSlider(rFlowMult, ref defaultFlowMultiplierForHumanlike, new FloatRange(0.25f, 2f), "EM.DefaultFlowMultiplierForHumanlike".Translate(MilkCumSettings.defaultFlowMultiplierForHumanlike.ToString("F2")), 0.05f);
		MilkCumSettings.defaultFlowMultiplierForHumanlike = defaultFlowMultiplierForHumanlike;
		TooltipHandler.TipRegion(rFlowMult, "EM.DefaultFlowMultiplierForHumanlikeDescLong".Translate());
		list.Gap(6f);
		GUI.color = Color.gray;
		list.Label("EM.OverflowResidualSection".Translate());
		GUI.color = Color.white;
		list.Gap(2f);
		Rect rResFlow = list.GetRect(UNIT_SIZE);
		float overflowResidual = MilkCumSettings.overflowResidualFlowFactor;
		Widgets.HorizontalSlider(rResFlow, ref overflowResidual, new FloatRange(0f, 0.25f), "EM.OverflowResidualFlowFactor".Translate((overflowResidual * 100f).ToString("F1") + "%"), 0.005f);
		MilkCumSettings.overflowResidualFlowFactor = Mathf.Clamp01(overflowResidual);
		TooltipHandler.TipRegion(rResFlow, "EM.OverflowResidualFlowFactorDesc".Translate());
		list.Gap(4f);
		Rect rResDyn = list.GetRect(UNIT_SIZE);
		bool resDyn = MilkCumSettings.overflowResidualDynamicScaling;
		Widgets.CheckboxLabeled(rResDyn, "EM.OverflowResidualDynamicScaling".Translate(), ref resDyn, false);
		MilkCumSettings.overflowResidualDynamicScaling = resDyn;
		TooltipHandler.TipRegion(rResDyn, "EM.OverflowResidualDynamicScalingDesc".Translate());
		if (MilkCumSettings.overflowResidualDynamicScaling)
		{
			list.Gap(4f);
			Rect rResL = list.GetRect(UNIT_SIZE);
			float refL = MilkCumSettings.overflowResidualLactationRefL;
			Widgets.HorizontalSlider(rResL, ref refL, new FloatRange(0.1f, 5f), "EM.OverflowResidualLactationRefL".Translate(refL.ToString("F2")), 0.05f);
			MilkCumSettings.overflowResidualLactationRefL = refL;
			TooltipHandler.TipRegion(rResL, "EM.OverflowResidualLactationRefLDesc".Translate());
			list.Gap(4f);
			Rect rResI = list.GetRect(UNIT_SIZE);
			float boostI = MilkCumSettings.overflowResidualInflammationBoost;
			Widgets.HorizontalSlider(rResI, ref boostI, new FloatRange(0f, 2f), "EM.OverflowResidualInflammationBoost".Translate(boostI.ToString("F2")), 0.05f);
			MilkCumSettings.overflowResidualInflammationBoost = boostI;
			TooltipHandler.TipRegion(rResI, "EM.OverflowResidualInflammationBoostDesc".Translate());
		}
		list.Gap(6f);
		// 泌乳水平上限：超过后吃药只延长时间、不提高流速
		Rect rLactationCap = list.GetRect(UNIT_SIZE);
		float lactationLevelCap = MilkCumSettings.lactationLevelCap;
		string capLabel = lactationLevelCap <= 0f ? "0 (off)" : lactationLevelCap.ToString("F1");
		Widgets.HorizontalSlider(rLactationCap, ref lactationLevelCap, new FloatRange(0f, 10f), "EM.LactationLevelCap".Translate(capLabel), 0.5f);
		MilkCumSettings.lactationLevelCap = lactationLevelCap;
		TooltipHandler.TipRegion(rLactationCap, "EM.LactationLevelCapDesc".Translate());
		list.Gap(4f);
		Rect rCapDurationMult = list.GetRect(UNIT_SIZE);
		float capDurationMult = MilkCumSettings.lactationLevelCapDurationMultiplier;
		Widgets.HorizontalSlider(rCapDurationMult, ref capDurationMult, new FloatRange(0.5f, 3f), "EM.LactationLevelCapDurationMultiplier".Translate(capDurationMult.ToString("F1")), 0.1f);
		MilkCumSettings.lactationLevelCapDurationMultiplier = capDurationMult;
		TooltipHandler.TipRegion(rCapDurationMult, "EM.LactationLevelCapDurationMultiplierDesc".Translate());
		list.Gap(6f);
		GUI.color = Color.gray;
		list.Label("EM.NPoolAdvancedSection".Translate());
		GUI.color = Color.white;
		Rect rSlowTheta = list.GetRect(UNIT_SIZE);
		float slowTheta = MilkCumSettings.adaptationSlowTheta;
		Widgets.HorizontalSlider(rSlowTheta, ref slowTheta, new FloatRange(0f, 0.01f), "EM.AdaptationSlowTheta".Translate(slowTheta.ToString("F4")), 0.0002f);
		MilkCumSettings.adaptationSlowTheta = slowTheta;
		TooltipHandler.TipRegion(rSlowTheta, "EM.AdaptationSlowThetaDesc".Translate());
		Rect rSlowOmega = list.GetRect(UNIT_SIZE);
		float slowOmega = MilkCumSettings.adaptationSlowOmega;
		Widgets.HorizontalSlider(rSlowOmega, ref slowOmega, new FloatRange(0f, 0.01f), "EM.AdaptationSlowOmega".Translate(slowOmega.ToString("F4")), 0.0002f);
		MilkCumSettings.adaptationSlowOmega = slowOmega;
		TooltipHandler.TipRegion(rSlowOmega, "EM.AdaptationSlowOmegaDesc".Translate());
		Rect rSubsteps = list.GetRect(UNIT_SIZE);
		float substeps = MilkCumSettings.inflowEventSubsteps;
		Widgets.HorizontalSlider(rSubsteps, ref substeps, new FloatRange(1f, 12f), "EM.InflowEventSubsteps".Translate(Mathf.RoundToInt(substeps).ToString()), 1f);
		MilkCumSettings.inflowEventSubsteps = Mathf.Clamp(Mathf.RoundToInt(substeps), 1, 12);
		TooltipHandler.TipRegion(rSubsteps, "EM.InflowEventSubstepsDesc".Translate());
		Rect rBurst = list.GetRect(UNIT_SIZE);
		float burst = MilkCumSettings.inflowEventBurstDurationTicks;
		Widgets.HorizontalSlider(rBurst, ref burst, new FloatRange(0f, 1200f), "EM.InflowEventBurstDurationTicks".Translate(Mathf.RoundToInt(burst).ToString()), 30f);
		MilkCumSettings.inflowEventBurstDurationTicks = Mathf.Clamp(Mathf.RoundToInt(burst), 0, 1200);
		TooltipHandler.TipRegion(rBurst, "EM.InflowEventBurstDurationTicksDesc".Translate());
		Rect rHopPenalty = list.GetRect(UNIT_SIZE);
		float hopPenalty = MilkCumSettings.ductHopPenaltyPerEdge;
		Widgets.HorizontalSlider(rHopPenalty, ref hopPenalty, new FloatRange(0f, 0.8f), "EM.DuctHopPenaltyPerEdge".Translate(hopPenalty.ToString("F2")), 0.02f);
		MilkCumSettings.ductHopPenaltyPerEdge = hopPenalty;
		TooltipHandler.TipRegion(rHopPenalty, "EM.DuctHopPenaltyPerEdgeDesc".Translate());
		Rect rInflowRes = list.GetRect(UNIT_SIZE);
		float inflowRes = MilkCumSettings.ductInflowInflammationResistance;
		Widgets.HorizontalSlider(rInflowRes, ref inflowRes, new FloatRange(0f, 4f), "EM.DuctInflowInflammationResistance".Translate(inflowRes.ToString("F2")), 0.05f);
		MilkCumSettings.ductInflowInflammationResistance = inflowRes;
		TooltipHandler.TipRegion(rInflowRes, "EM.DuctInflowInflammationResistanceDesc".Translate());
		Rect rDrainResManual = list.GetRect(UNIT_SIZE);
		float drainResManual = MilkCumSettings.ductDrainInflammationResistanceManual;
		Widgets.HorizontalSlider(rDrainResManual, ref drainResManual, new FloatRange(0f, 4f), "EM.DuctDrainInflammationResistanceManual".Translate(drainResManual.ToString("F2")), 0.05f);
		MilkCumSettings.ductDrainInflammationResistanceManual = drainResManual;
		TooltipHandler.TipRegion(rDrainResManual, "EM.DuctDrainInflammationResistanceManualDesc".Translate());
		Rect rDrainResMachine = list.GetRect(UNIT_SIZE);
		float drainResMachine = MilkCumSettings.ductDrainInflammationResistanceMachine;
		Widgets.HorizontalSlider(rDrainResMachine, ref drainResMachine, new FloatRange(0f, 4f), "EM.DuctDrainInflammationResistanceMachine".Translate(drainResMachine.ToString("F2")), 0.05f);
		MilkCumSettings.ductDrainInflammationResistanceMachine = drainResMachine;
		TooltipHandler.TipRegion(rDrainResMachine, "EM.DuctDrainInflammationResistanceMachineDesc".Translate());
		Rect rMachineSuction = list.GetRect(UNIT_SIZE);
		float machineSuction = MilkCumSettings.ductMachineSuctionBonus;
		Widgets.HorizontalSlider(rMachineSuction, ref machineSuction, new FloatRange(0.5f, 2f), "EM.DuctMachineSuctionBonus".Translate(machineSuction.ToString("F2")), 0.02f);
		MilkCumSettings.ductMachineSuctionBonus = machineSuction;
		TooltipHandler.TipRegion(rMachineSuction, "EM.DuctMachineSuctionBonusDesc".Translate());
		Rect rPressureBase = list.GetRect(UNIT_SIZE);
		float pressureBase = MilkCumSettings.ductDrainPressureBase;
		Widgets.HorizontalSlider(rPressureBase, ref pressureBase, new FloatRange(0f, 2f), "EM.DuctDrainPressureBase".Translate(pressureBase.ToString("F2")), 0.02f);
		MilkCumSettings.ductDrainPressureBase = pressureBase;
		TooltipHandler.TipRegion(rPressureBase, "EM.DuctDrainPressureBaseDesc".Translate());
		Rect rPressureScale = list.GetRect(UNIT_SIZE);
		float pressureScale = MilkCumSettings.ductDrainPressureScale;
		Widgets.HorizontalSlider(rPressureScale, ref pressureScale, new FloatRange(0f, 2f), "EM.DuctDrainPressureScale".Translate(pressureScale.ToString("F2")), 0.02f);
		MilkCumSettings.ductDrainPressureScale = pressureScale;
		TooltipHandler.TipRegion(rPressureScale, "EM.DuctDrainPressureScaleDesc".Translate());
		Rect rCondMin = list.GetRect(UNIT_SIZE);
		float condMin = MilkCumSettings.ductConductanceMin;
		Widgets.HorizontalSlider(rCondMin, ref condMin, new FloatRange(0.01f, 1f), "EM.DuctConductanceMin".Translate(condMin.ToString("F2")), 0.01f);
		MilkCumSettings.ductConductanceMin = condMin;
		TooltipHandler.TipRegion(rCondMin, "EM.DuctConductanceMinDesc".Translate());
		Rect rCondMax = list.GetRect(UNIT_SIZE);
		float condMax = MilkCumSettings.ductConductanceMax;
		Widgets.HorizontalSlider(rCondMax, ref condMax, new FloatRange(MilkCumSettings.ductConductanceMin, 3f), "EM.DuctConductanceMax".Translate(condMax.ToString("F2")), 0.02f);
		MilkCumSettings.ductConductanceMax = condMax;
		TooltipHandler.TipRegion(rCondMax, "EM.DuctConductanceMaxDesc".Translate());
		list.Gap(6f);
		// Lactating gain
		Rect rLactatingGain = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rLactatingGain, "EM.LactatingGain".Translate(), ref MilkCumSettings.lactatingGainEnabled, false);
		{ string t = "EM.LactatingGainDesc".Translate(); TooltipHandler.TipRegion(rLactatingGain, string.IsNullOrEmpty(t) ? "EM.LactatingGainDesc" : t); }
		list.Gap(4f);
		float pct = MilkCumSettings.lactatingGainCapModPercent;
		Rect rPct = list.GetRect(UNIT_SIZE);
		Widgets.HorizontalSlider(rPct, ref pct, new FloatRange(0f, 0.20f), "EM.LactatingGainPercent".Translate(pct.ToStringPercent()), 0.01f);
		MilkCumSettings.lactatingGainCapModPercent = pct;
		list.Gap(6f);
		GUI.color = Color.gray;
		list.Label("EM.BreastPoolParamsHint".Translate());
		GUI.color = Color.white;
	}

	private void DrawRjwBlock(Listing_Standard list)
	{
		// Breast/pool params moved to Breast & pool sub-tab
		Rect rRjwLust = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rRjwLust, "EM.RjwLustFromNursing".Translate(), ref MilkCumSettings.rjwLustFromNursingEnabled, false);
		{ string t = "EM.RjwLustFromNursingDesc".Translate(); TooltipHandler.TipRegion(rRjwLust, string.IsNullOrEmpty(t) ? "EM.RjwLustFromNursingDesc" : t); }
		list.Gap(6f);
		Rect rRjwNeed = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rRjwNeed, "EM.RjwSexNeedLactatingBonus".Translate(), ref MilkCumSettings.rjwSexNeedLactatingBonusEnabled, false);
		{ string t = "EM.RjwSexNeedLactatingBonusDesc".Translate(); TooltipHandler.TipRegion(rRjwNeed, string.IsNullOrEmpty(t) ? "EM.RjwSexNeedLactatingBonusDesc" : t); }
		list.Gap(6f);
		Rect rRjwSat = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rRjwSat, "EM.RjwSexSatisfactionAfterNursing".Translate(), ref MilkCumSettings.rjwSexSatisfactionAfterNursingEnabled, false);
		{ string t = "EM.RjwSexSatisfactionAfterNursingDesc".Translate(); TooltipHandler.TipRegion(rRjwSat, string.IsNullOrEmpty(t) ? "EM.RjwSexSatisfactionAfterNursingDesc" : t); }
		list.Gap(6f);
		float fert = MilkCumSettings.rjwLactationFertilityFactor;
		Rect rFert = list.GetRect(UNIT_SIZE);
		Widgets.HorizontalSlider(rFert, ref fert, new FloatRange(0f, 1f), "EM.RjwLactationFertility".Translate(fert.ToStringPercent()), 0.05f);
		MilkCumSettings.rjwLactationFertilityFactor = fert;
		list.Gap(6f);
		Rect rRjwInSex = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rRjwInSex, "EM.RjwLactatingInSexDesc".Translate(), ref MilkCumSettings.rjwLactatingInSexDescriptionEnabled, false);
		{ string t = "EM.RjwLactatingInSexDescDesc".Translate(); TooltipHandler.TipRegion(rRjwInSex, string.IsNullOrEmpty(t) ? "EM.RjwLactatingInSexDescDesc" : t); }
		list.Gap(6f);
		Rect rSexBoost = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rSexBoost, "EM.RjwSexAddsLactationBoost".Translate(), ref MilkCumSettings.rjwSexAddsLactationBoost, false);
		{ string t = "EM.RjwSexAddsLactationBoostDesc".Translate(); TooltipHandler.TipRegion(rSexBoost, string.IsNullOrEmpty(t) ? "EM.RjwSexAddsLactationBoostDesc" : t); }
		list.Gap(6f);
		if (MilkCumSettings.rjwSexAddsLactationBoost)
		{
			Rect rDeltaS = list.GetRect(UNIT_SIZE);
			Widgets.Label(rDeltaS.LeftHalf(), "EM.RjwSexLactationBoostDeltaS".Translate(MilkCumSettings.rjwSexLactationBoostDeltaS.ToString("F2")));
			MilkCumSettings.rjwSexLactationBoostDeltaS = Widgets.HorizontalSlider(rDeltaS.RightHalf(), MilkCumSettings.rjwSexLactationBoostDeltaS, 0.05f, 0.5f, true);
		}
	}

	// Draw(Rect) removed; use DrawSection(mainTab, subTab) instead.
	private static List<string> ParseCommaSeparatedDefNames(string text)
	{
		var list = new List<string>();
		if (string.IsNullOrWhiteSpace(text)) return list;
		foreach (string s in text.Split(','))
		{
			string t = s.Trim();
			if (!string.IsNullOrEmpty(t)) list.Add(t);
		}
		return list;
	}
}
