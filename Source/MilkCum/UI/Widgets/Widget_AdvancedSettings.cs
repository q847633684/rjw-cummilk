using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using RimWorld;
using UnityEngine;
using Verse;

using static MilkCum.Core.Constants.Constants;

namespace MilkCum.UI;
public class Widget_AdvancedSettings
{
	private Vector2 _advancedScrollPosition = Vector2.zero;
	private Vector2 _sectionScrollPosition = Vector2.zero;

	/// <summary>主/子 Tab 结构下按区块绘制。mainTab 2=健康与风险，3=效率与界面，4=联动与扩展。</summary>
	public void DrawSection(Rect inRect, int mainTab, int subTab)
	{
		float topMargin = 36f;
		Rect scrollViewRect = new Rect(inRect.x, inRect.y + topMargin, inRect.width, inRect.height - topMargin);
		float contentHeight = 1200f;
		Rect scrollContent = new Rect(0f, 0f, scrollViewRect.width - 20f, contentHeight);
		Widgets.BeginScrollView(scrollViewRect, ref _sectionScrollPosition, scrollContent, true);
		try
		{
			if (mainTab == 2)
				DrawHealthSection(scrollContent, subTab);
			else if (mainTab == 3)
				DrawEfficiencyOrInterfaceSection(scrollContent, subTab);
			else if (mainTab == 4)
				DrawIntegrationSection(scrollContent, subTab);
		}
		finally
		{
			Widgets.EndScrollView();
		}
	}

	/// <summary>开发模式：选中小人的泌乳状态调试信息。仅在游戏中且有当前地图时访问 Selector，避免在 Mod 设置界面触发 Find.MapUI 的 InvalidCastException。</summary>
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
		if (subTab == 0)
			DrawMastitisBlock(list);
		else if (subTab == 1)
			DrawDbhBlock(list);
		else if (subTab == 2)
			DrawToleranceOverflowBlock(list);
		else if (subTab == 3)
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
		if (subTab == 0)
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
		if (subTab == 0 && ModLister.GetModWithIdentifier("rim.job.world") != null)
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
		else if (subTab == 1 && DubsBadHygieneIntegration.IsDubsBadHygieneActive())
			DrawDbhBlock(list);
		list.End();
	}

	/// <summary>乳房与池：容量、灌满时间、剩余天数等可调系数集中在此，带详细说明。</summary>
	private void DrawBreastPoolBlock(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_BreastPool".Translate());
		GUI.color = Color.white;
		list.Gap(6f);
		// 双池数据来源：勾选 = RJW 乳房尺寸与流速，不勾 = 人形对称双池
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
		// 剩余天数：药物基准、分娩基准
		Rect rBaseline = list.GetRect(UNIT_SIZE);
		float baseline = MilkCumSettings.baselineMilkDurationDays;
		Widgets.HorizontalSlider(rBaseline, ref baseline, new FloatRange(1f, 15f), "EM.BaselineMilkDurationDays".Translate(MilkCumSettings.baselineMilkDurationDays.ToString("F0")), 0.5f);
		MilkCumSettings.baselineMilkDurationDays = baseline;
		TooltipHandler.TipRegion(rBaseline, "EM.BaselineMilkDurationDaysDesc".Translate());
		list.Gap(4f);
		Rect rBirth = list.GetRect(UNIT_SIZE);
		float birth = MilkCumSettings.birthInducedMilkDurationDays;
		Widgets.HorizontalSlider(rBirth, ref birth, new FloatRange(1f, 30f), "EM.BirthInducedMilkDurationDays".Translate(MilkCumSettings.birthInducedMilkDurationDays.ToString("F0")), 0.5f);
		MilkCumSettings.birthInducedMilkDurationDays = birth;
		TooltipHandler.TipRegion(rBirth, "EM.BirthInducedMilkDurationDaysDesc".Translate());
		list.Gap(4f);
		GUI.color = Color.gray;
		list.Label("EM.BaselineMilkDurationReference".Translate(MilkCumSettings.baselineMilkDurationDays.ToString("F0"), MilkCumSettings.birthInducedMilkDurationDays.ToString("F0")));
		GUI.color = Color.white;
		list.Gap(6f);
		// 泌乳效率与增益（与容量/流速/剩余天数同属泌乳效果控制）
		Rect rEff = list.GetRect(UNIT_SIZE);
		Widgets.HorizontalSlider(rEff, ref MilkCumSettings.lactatingEfficiencyMultiplierPerStack, new FloatRange(0.01f, 5f), "EM.LactatingEfficiencyMultiplier".Translate(MilkCumSettings.lactatingEfficiencyMultiplierPerStack.ToString("F2")), 0.01f);
		TooltipHandler.TipRegion(rEff, "EM.LactatingEfficiencyMultiplierDesc".Translate());
		list.Gap(4f);
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
		// 乳房容量/流速/池参数已移至「乳房与池」子页
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

	public void Draw(Rect inRect)
	{
		// 预留顶部区域给窗口关闭键，避免关闭键随内容一起滚动
		float topMargin = 36f;
		Rect scrollViewRect = new Rect(inRect.x, inRect.y + topMargin, inRect.width, inRect.height - topMargin);
		float contentHeight = 2600f;
		Rect scrollContent = new Rect(0f, 0f, scrollViewRect.width - 20f, contentHeight);
		Widgets.BeginScrollView(scrollViewRect, ref _advancedScrollPosition, scrollContent, true);
		try
		{
		Rect sliderRect = new(0f, 0f, scrollContent.width, UNIT_SIZE);
		Widgets.HorizontalSlider(sliderRect, ref MilkCumSettings.lactatingEfficiencyMultiplierPerStack, new FloatRange(0.01f, 5f), "EM.LactatingEfficiencyMultiplier".Translate(MilkCumSettings.lactatingEfficiencyMultiplierPerStack.ToString("F2")), 0.01f);
		TooltipHandler.TipRegion(sliderRect, "EM.LactatingEfficiencyMultiplierDesc".Translate());
		sliderRect.y += UNIT_SIZE * 2;
		// 泌乳期意识/操纵/移动增益
		Rect rLactatingGain = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
		Widgets.CheckboxLabeled(rLactatingGain, "EM.LactatingGain".Translate(), ref MilkCumSettings.lactatingGainEnabled, false);
		{ string t = "EM.LactatingGainDesc".Translate(); TooltipHandler.TipRegion(rLactatingGain, string.IsNullOrEmpty(t) ? "EM.LactatingGainDesc" : t); }
		sliderRect.y += UNIT_SIZE;
		float pct = MilkCumSettings.lactatingGainCapModPercent;
		Widgets.HorizontalSlider(sliderRect, ref pct, new FloatRange(0f, 0.20f), "EM.LactatingGainPercent".Translate(pct.ToStringPercent()), 0.01f);
		MilkCumSettings.lactatingGainCapModPercent = pct;
		sliderRect.y += UNIT_SIZE * 2;
		// 谁可以吸奶 / 谁可使用奶制品：改为按产奶者指定，在奶表格「指定」列设置
		GUI.color = Color.gray;
		Widgets.Label(sliderRect, "EM.SectionDesc_IdentityAndMenu".Translate());
		sliderRect.y += Text.CalcHeight("EM.SectionDesc_IdentityAndMenu".Translate(), scrollContent.width - 20f) + 4f;
		GUI.color = Color.white;
		Widgets.Label(sliderRect, "EM.ProducerRestrictionsHint".Translate());
		GUI.color = Color.white;
		sliderRect.y += UNIT_SIZE * 2;
		// 7.9: 药物分工提示
		GUI.color = Color.gray;
		Widgets.Label(sliderRect, "EM.DrugRoleHint".Translate());
		GUI.color = Color.white;
		sliderRect.y += UNIT_SIZE * 2;
		string s_breastfeedTime = MilkCumSettings.breastfeedTime.ToString();
		Widgets.TextFieldNumericLabeled(sliderRect, Lang.Join(Lang.Breastfeed, Lang.Time), ref MilkCumSettings.breastfeedTime, ref s_breastfeedTime, 1f);
		sliderRect.y += UNIT_SIZE;
		string s_breastfeedCap = MilkCumSettings.breastfeedCapacityFactor.ToString();
		Widgets.TextFieldNumericLabeled(sliderRect, "EM.BreastfeedCapacityFactor".Translate(), ref MilkCumSettings.breastfeedCapacityFactor, ref s_breastfeedCap, 0.05f);
		sliderRect.y += UNIT_SIZE;
		string s_milkWorkBase = MilkCumSettings.milkingWorkTotalBase.ToString();
		Widgets.TextFieldNumericLabeled(sliderRect, "EM.MilkingWorkTotalBase".Translate(), ref MilkCumSettings.milkingWorkTotalBase, ref s_milkWorkBase, 1f);
		sliderRect.y += UNIT_SIZE;
		string s_milkCap = MilkCumSettings.milkingCapacityFactor.ToString();
		Widgets.TextFieldNumericLabeled(sliderRect, "EM.MilkingCapacityFactor".Translate(), ref MilkCumSettings.milkingCapacityFactor, ref s_milkCap, 0.05f);
		sliderRect.y += UNIT_SIZE;
		string prolactinLabel = MilkCumDefOf.EM_Prolactin?.label ?? "Prolactin";
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Mechanoid), ref MilkCumSettings.showMechOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Colonist), ref MilkCumSettings.showColonistOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Slave), ref MilkCumSettings.showSlaveOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Prisoner), ref MilkCumSettings.showPrisonerOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Animal), ref MilkCumSettings.showAnimalOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Misc), ref MilkCumSettings.showMiscOptions);
		sliderRect.y += UNIT_SIZE;
		// RJW 联动（仅当 RJW 激活时显示）
		if (ModLister.GetModWithIdentifier("rim.job.world") != null)
		{
			sliderRect.y += UNIT_SIZE;
			GUI.color = Color.gray;
			Widgets.Label(sliderRect, "EM.SectionDesc_RJW".Translate());
			sliderRect.y += Text.CalcHeight("EM.SectionDesc_RJW".Translate(), scrollContent.width - 20f) + 4f;
			GUI.color = Color.white;
			Widgets.Label(sliderRect, "EM.RJWSection".Translate());
			GUI.color = Color.white;
			sliderRect.y += UNIT_SIZE;
			Rect rRjwBreast = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwBreast, "EM.RjwBreastSize".Translate(), ref MilkCumSettings.rjwBreastSizeEnabled, false);
			{ string t = "EM.RjwBreastSizeDesc".Translate(); TooltipHandler.TipRegion(rRjwBreast, string.IsNullOrEmpty(t) ? "EM.RjwBreastSizeDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			Rect rFlowMult = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			float defaultFlowMultiplierForHumanlike = MilkCumSettings.defaultFlowMultiplierForHumanlike;
			Widgets.HorizontalSlider(rFlowMult, ref defaultFlowMultiplierForHumanlike, new FloatRange(0.25f, 2f), "EM.DefaultFlowMultiplierForHumanlike".Translate(MilkCumSettings.defaultFlowMultiplierForHumanlike.ToString("F2")), 0.05f);
			MilkCumSettings.defaultFlowMultiplierForHumanlike = defaultFlowMultiplierForHumanlike;
			TooltipHandler.TipRegion(rFlowMult, "EM.DefaultFlowMultiplierForHumanlikeDesc".Translate());
			sliderRect.y += UNIT_SIZE;
			Rect rCapCoeff = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			float capCoeff = MilkCumSettings.rjwBreastCapacityCoefficient;
			Widgets.HorizontalSlider(rCapCoeff, ref capCoeff, new FloatRange(0.25f, 2f), "EM.RjwBreastCapacityCoefficient".Translate(MilkCumSettings.rjwBreastCapacityCoefficient.ToString("F2")), 0.05f);
			MilkCumSettings.rjwBreastCapacityCoefficient = capCoeff;
			TooltipHandler.TipRegion(rCapCoeff, "EM.RjwBreastCapacityCoefficientDesc".Translate());
			sliderRect.y += UNIT_SIZE;
			Rect rRjwLust = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwLust, "EM.RjwLustFromNursing".Translate(), ref MilkCumSettings.rjwLustFromNursingEnabled, false);
			{ string t = "EM.RjwLustFromNursingDesc".Translate(); TooltipHandler.TipRegion(rRjwLust, string.IsNullOrEmpty(t) ? "EM.RjwLustFromNursingDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			Rect rRjwNeed = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwNeed, "EM.RjwSexNeedLactatingBonus".Translate(), ref MilkCumSettings.rjwSexNeedLactatingBonusEnabled, false);
			{ string t = "EM.RjwSexNeedLactatingBonusDesc".Translate(); TooltipHandler.TipRegion(rRjwNeed, string.IsNullOrEmpty(t) ? "EM.RjwSexNeedLactatingBonusDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			Rect rRjwSat = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwSat, "EM.RjwSexSatisfactionAfterNursing".Translate(), ref MilkCumSettings.rjwSexSatisfactionAfterNursingEnabled, false);
			{ string t = "EM.RjwSexSatisfactionAfterNursingDesc".Translate(); TooltipHandler.TipRegion(rRjwSat, string.IsNullOrEmpty(t) ? "EM.RjwSexSatisfactionAfterNursingDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			float fert = MilkCumSettings.rjwLactationFertilityFactor;
			Widgets.HorizontalSlider(sliderRect, ref fert, new FloatRange(0f, 1f), "EM.RjwLactationFertility".Translate(fert.ToStringPercent()), 0.05f);
			MilkCumSettings.rjwLactationFertilityFactor = fert;
			sliderRect.y += UNIT_SIZE;
			Rect rRjwInSex = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwInSex, "EM.RjwLactatingInSexDesc".Translate(), ref MilkCumSettings.rjwLactatingInSexDescriptionEnabled, false);
			{ string t = "EM.RjwLactatingInSexDescDesc".Translate(); TooltipHandler.TipRegion(rRjwInSex, string.IsNullOrEmpty(t) ? "EM.RjwLactatingInSexDescDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			Rect rSexBoost = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rSexBoost, "EM.RjwSexAddsLactationBoost".Translate(), ref MilkCumSettings.rjwSexAddsLactationBoost, false);
			{ string t = "EM.RjwSexAddsLactationBoostDesc".Translate(); TooltipHandler.TipRegion(rSexBoost, string.IsNullOrEmpty(t) ? "EM.RjwSexAddsLactationBoostDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			if (MilkCumSettings.rjwSexAddsLactationBoost)
			{
				Rect rDeltaS = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
				Widgets.Label(rDeltaS.LeftHalf(), "EM.RjwSexLactationBoostDeltaS".Translate(MilkCumSettings.rjwSexLactationBoostDeltaS.ToString("F2")));
				MilkCumSettings.rjwSexLactationBoostDeltaS = Widgets.HorizontalSlider(rDeltaS.RightHalf(), MilkCumSettings.rjwSexLactationBoostDeltaS, 0.05f, 0.5f, true);
				sliderRect.y += UNIT_SIZE;
			}
		}
		// Dubs Bad Hygiene 联动：乳腺炎/堵塞的卫生触发
		if (DubsBadHygieneIntegration.IsDubsBadHygieneActive())
		{
			sliderRect.y += UNIT_SIZE;
			GUI.color = Color.gray;
			Widgets.Label(sliderRect, "EM.SectionDesc_DBH".Translate());
			sliderRect.y += Text.CalcHeight("EM.SectionDesc_DBH".Translate(), scrollContent.width - 20f) + 4f;
			GUI.color = Color.white;
			Widgets.Label(sliderRect, "EM.DubsBadHygieneSection".Translate());
			sliderRect.y += UNIT_SIZE;
			GUI.color = Color.white;
			Rect rDbhMastitis = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rDbhMastitis, "EM.UseDubsBadHygieneForMastitis".Translate(), ref MilkCumSettings.useDubsBadHygieneForMastitis, false);
			{ string t = "EM.UseDubsBadHygieneForMastitisDesc".Translate(); TooltipHandler.TipRegion(rDbhMastitis, string.IsNullOrEmpty(t) ? "EM.UseDubsBadHygieneForMastitisDesc" : t); }
		}
		// 乳腺炎与耐受、溢出与 AI：可折叠区块
		Rect listRect = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, contentHeight - sliderRect.y);
		Listing_Standard list = new Listing_Standard();
		list.Begin(listRect);
		// 建议 22：从 Def 加载默认（Def 可被其他 mod patch）
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_LoadFromDef".Translate());
		GUI.color = Color.white;
		list.Gap(4f);
		Rect rLoadDef = list.GetRect(UNIT_SIZE);
		if (Widgets.ButtonText(rLoadDef, "EM.LoadDefaultsFromDef".Translate()))
			MilkCumSettings.ApplyDefaultsFromDef();
		TooltipHandler.TipRegion(rLoadDef, "EM.LoadDefaultsFromDefDesc".Translate());
		list.Gap(6f);
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_Mastitis".Translate());
		GUI.color = Color.white;
		list.Gap(4f);
		list.Label("EM.MastitisAndToleranceSection".Translate());
		list.Gap(4f);
		{
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
			list.Gap(6f);
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
		list.Gap(6f);
		list.Label("EM.OverflowFilthSection".Translate());
		list.Gap(4f);
		{
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
		list.Gap(6f);
		list.Label("EM.RaceOverridesSection".Translate());
		list.Gap(4f);
		{
			string raceAlways = string.Join(", ", MilkCumSettings.raceCanAlwaysLactate ?? new List<string>());
			Rect rAlways = list.GetRect(UNIT_SIZE);
			Widgets.Label(rAlways.LeftHalf(), "EM.RaceCanAlwaysLactate".Translate());
			raceAlways = Widgets.TextField(rAlways.RightHalf(), raceAlways, 128);
			MilkCumSettings.raceCanAlwaysLactate = ParseCommaSeparatedDefNames(raceAlways);
			list.Gap(6f);
			string raceNever = string.Join(", ", MilkCumSettings.raceCannotLactate ?? new List<string>());
			Rect rNever = list.GetRect(UNIT_SIZE);
			Widgets.Label(rNever.LeftHalf(), "EM.RaceCannotLactate".Translate());
			raceNever = Widgets.TextField(rNever.RightHalf(), raceNever, 128);
			MilkCumSettings.raceCannotLactate = ParseCommaSeparatedDefNames(raceNever);
			list.Gap(6f);
		}
		// DevMode：选中小人的泌乳状态（L、双池、E、卫生风险、溢出累计等）
		if (Prefs.DevMode)
		{
			list.Gap(6f);
			list.Label("EM.DevModeLactationPanel".Translate());
			list.Gap(4f);
			if (Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null)
			{
				Pawn sel = Find.Selector.SingleSelectedThing as Pawn;
				if (sel == null)
				{
					list.Label("EM.DevModeNoPawnSelected".Translate());
				}
				else
				{
					var hediff = sel.LactatingHediffWithComps();
					var comp = hediff?.comps?.OfType<HediffComp_EqualMilkingLactating>().FirstOrDefault();
					if (comp == null)
					{
						list.Label("EM.DevModeNotLactating".Translate(sel.LabelShort));
					}
					else
					{
						string debug = comp.CompDebugString();
						float h = Text.CalcHeight(debug, list.ColumnWidth);
						Rect rDebug = list.GetRect(Mathf.Min(h, 200f));
						Widgets.Label(rDebug, debug);
					}
				}
			}
			else
			{
				list.Label("EM.DevModeNoGame".Translate());
			}
		}
		list.End();
		}
		finally
		{
			Widgets.EndScrollView();
		}
	}

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