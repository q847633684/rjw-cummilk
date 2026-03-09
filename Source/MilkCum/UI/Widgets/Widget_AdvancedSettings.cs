using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Core.Settings;
using RimWorld;
using UnityEngine;
using Verse;

using static MilkCum.Core.Constants.Constants;

namespace MilkCum.UI;
public class Widget_AdvancedSettings
{
	/// <summary>Fixed height for settings section scroll content. If users report truncation on very high DPI or many options, consider content-based height or ListView (if RimWorld API supports it).</summary>
	private const float SectionScrollContentHeight = 2400f;

	private Vector2 _sectionScrollPosition = Vector2.zero;

	// 健康与风险子 Tab 索引
	private const int SubTabHealth_Mastitis = 0;
	private const int SubTabHealth_DBH = 1;
	private const int SubTabHealth_ToleranceOverflow = 2;
	private const int SubTabHealth_LoadFromDef = 3;
	// 效率与界面子 Tab 索引
	private const int SubTabEfficiency_IdentityAndMenu = 0;
	private const int SubTabEfficiency_BreastPool = 1;
	// 联动与扩展子 Tab 索引
	private const int SubTabIntegration_RJW = 0;
	private const int SubTabIntegration_DBH = 1;

	/// <summary>Main/sub tab layout; mainTab is MainTabIndex (Health=2, Efficiency=3, Integration=4).</summary>
	public void DrawSection(Rect inRect, int mainTab, int subTab)
	{
		float topMargin = 36f;
		Rect scrollViewRect = new Rect(inRect.x, inRect.y + topMargin, inRect.width, inRect.height - topMargin);
		Rect scrollContent = new Rect(0f, 0f, scrollViewRect.width - 20f, SectionScrollContentHeight);
		Widgets.BeginScrollView(scrollViewRect, ref _sectionScrollPosition, scrollContent, true);
		try
		{
			if (mainTab == (int)MainTabIndex.HealthAndRisk)
				DrawHealthSection(scrollContent, subTab);
			else if (mainTab == (int)MainTabIndex.EfficiencyAndInterface)
				DrawEfficiencyOrInterfaceSection(scrollContent, subTab);
			else if (mainTab == (int)MainTabIndex.IntegrationAndAdvanced)
				DrawIntegrationSection(scrollContent, subTab);
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
		else if (subTab == SubTabHealth_ToleranceOverflow)
			DrawToleranceOverflowBlock(list);
		else if (subTab == SubTabHealth_LoadFromDef)
		{
			GUI.color = Color.gray;
			list.Label("EM.SectionDesc_LoadFromDef".Translate());
			GUI.color = Color.white;
			list.Gap(4f);
			Rect rLoadDef = list.GetRect(UNIT_SIZE);
			if (Widgets.ButtonText(rLoadDef, "EM.LoadDefaultsFromDef".Translate()))
				MilkCumSettings.ApplyDefaultsFromDef();
			TooltipHandler.TipRegion(rLoadDef, "EM.LoadDefaultsFromDefDesc".Translate());
		}
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

	private void DrawToleranceOverflowBlock(Listing_Standard list)
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
		list.Gap(6f);
		list.Label("EM.OverflowFilthSection".Translate());
		list.Gap(4f);
		string overflowFilth = MilkCumSettings.overflowFilthDefName ?? "";
		Rect rFilth = list.GetRect(UNIT_SIZE);
		Widgets.Label(rFilth.LeftHalf(), "EM.OverflowFilthDefName".Translate());
		overflowFilth = Widgets.TextField(rFilth.RightHalf(), overflowFilth, 64);
		MilkCumSettings.overflowFilthDefName = overflowFilth?.Trim() ?? "Filth_Vomit";
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

	private void DrawIntegrationSection(Rect content, int subTab)
	{
		var list = new Listing_Standard();
		list.Begin(content);
		if (subTab == SubTabIntegration_RJW && ModLister.GetModWithIdentifier("rim.job.world") != null)
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
		else if (subTab == SubTabIntegration_RJW)
		{
			GUI.color = Color.gray;
			list.Label("EM.RequiresRJWMod".Translate());
			GUI.color = Color.white;
		}
		else if (subTab == SubTabIntegration_DBH && DubsBadHygieneIntegration.IsDubsBadHygieneActive())
			DrawDbhBlock(list);
		list.End();
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
		Rect rCapCoeff = list.GetRect(UNIT_SIZE);
		float capCoeff = MilkCumSettings.rjwBreastCapacityCoefficient;
		Widgets.HorizontalSlider(rCapCoeff, ref capCoeff, new FloatRange(0.25f, 4f), "EM.RjwBreastCapacityCoefficient".Translate(MilkCumSettings.rjwBreastCapacityCoefficient.ToString("F2")), 0.05f);
		MilkCumSettings.rjwBreastCapacityCoefficient = capCoeff;
		TooltipHandler.TipRegion(rCapCoeff, "EM.RjwBreastCapacityCoefficientDescLong".Translate());
		list.Gap(6f);
		// 流速倍率
		Rect rFlowMult = list.GetRect(UNIT_SIZE);
		float defaultFlowMultiplierForHumanlike = MilkCumSettings.defaultFlowMultiplierForHumanlike;
		Widgets.HorizontalSlider(rFlowMult, ref defaultFlowMultiplierForHumanlike, new FloatRange(0.25f, 2f), "EM.DefaultFlowMultiplierForHumanlike".Translate(MilkCumSettings.defaultFlowMultiplierForHumanlike.ToString("F2")), 0.05f);
		MilkCumSettings.defaultFlowMultiplierForHumanlike = defaultFlowMultiplierForHumanlike;
		TooltipHandler.TipRegion(rFlowMult, "EM.DefaultFlowMultiplierForHumanlikeDescLong".Translate());
		list.Gap(6f);
		// Baseline/birth milk duration days
		Rect rBaseline = list.GetRect(UNIT_SIZE);
		float baseline = MilkCumSettings.baselineMilkDurationDays;
		Widgets.HorizontalSlider(rBaseline, ref baseline, new FloatRange(1f, 15f), "EM.BaselineMilkDurationDays".Translate(MilkCumSettings.baselineMilkDurationDays.ToString("F0")), 0.5f);
		MilkCumSettings.baselineMilkDurationDays = baseline;
		TooltipHandler.TipRegion(rBaseline, "EM.BaselineMilkDurationDaysDesc".Translate());
		list.Gap(6f);
		Rect rBirth = list.GetRect(UNIT_SIZE);
		float birthInduced = MilkCumSettings.birthInducedMilkDurationDays;
		Widgets.HorizontalSlider(rBirth, ref birthInduced, new FloatRange(1f, 360f), "EM.BirthInducedMilkDurationDays".Translate(MilkCumSettings.birthInducedMilkDurationDays.ToString("F0")), 1f);
		MilkCumSettings.birthInducedMilkDurationDays = birthInduced;
		TooltipHandler.TipRegion(rBirth, "EM.BirthInducedMilkDurationDaysDesc".Translate());
		list.Gap(6f);
		GUI.color = Color.gray;
		list.Label("EM.BaselineMilkDurationReference".Translate(MilkCumSettings.baselineMilkDurationDays.ToString("F0"), MilkCumSettings.birthInducedMilkDurationDays.ToString("F0")));
		GUI.color = Color.white;
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
