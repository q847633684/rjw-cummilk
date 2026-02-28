using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using MilkCum.Milk.Comps;
using RimWorld;
using UnityEngine;
using Verse;

using static MilkCum.Milk.Helpers.Constants;

namespace MilkCum.UI;
public class Widget_AdvancedSettings
{
	private Vector2 _advancedScrollPosition = Vector2.zero;

	public void Draw(Rect inRect)
	{
		// 使用滚动区域，避免内容超出设置框（长标签、小窗口时可滚动查看）
		float contentHeight = 2600f;
		Rect scrollContent = new Rect(0f, 0f, inRect.width - 20f, contentHeight);
		Widgets.BeginScrollView(inRect, ref _advancedScrollPosition, scrollContent, true);
		Rect sliderRect = new(0f, 0f, scrollContent.width, UNIT_SIZE);
		float fStacks = (float)EqualMilkingSettings.maxLactationStacks;
		string lactatingLabel = HediffDefOf.Lactating?.label ?? "Lactating";
		Widgets.HorizontalSlider(sliderRect, ref fStacks, new FloatRange(1, 10), Lang.Join(lactatingLabel, Lang.InstallImplantAlreadyMaxLevel) + ": " + ((int)fStacks).ToString(), 1f);
		EqualMilkingSettings.maxLactationStacks = (int)fStacks;
		sliderRect.y += UNIT_SIZE;
		GUI.color = Color.gray;
		GameFont prevFont = Text.Font;
		Text.Font = GameFont.Tiny;
		Widgets.Label(sliderRect, "（已弃用，仅兼容旧存档）");
		Text.Font = prevFont;
		GUI.color = Color.white;
		sliderRect.y += UNIT_SIZE;
		Widgets.HorizontalSlider(sliderRect, ref EqualMilkingSettings.lactatingEfficiencyMultiplierPerStack, new FloatRange(0.01f, 5f), Lang.Join(lactatingLabel, Lang.Efficiency, Lang.StatFactor) + ": " + EqualMilkingSettings.lactatingEfficiencyMultiplierPerStack.ToString(), 0.01f);
		sliderRect.y += UNIT_SIZE * 2;
		Widgets.HorizontalSlider(sliderRect, ref EqualMilkingSettings.milkAmountMultiplierPerStack, new FloatRange(0.01f, 5f), Lang.Join(Lang.MilkAmount, Lang.StatFactor) + ": " + EqualMilkingSettings.milkAmountMultiplierPerStack.ToString(), 0.01f);
		sliderRect.y += UNIT_SIZE * 2;
		Widgets.HorizontalSlider(sliderRect, ref EqualMilkingSettings.hungerRateMultiplierPerStack, new FloatRange(0f, 5f), Lang.Join(Lang.HungerRate, Lang.StatFactor) + ": " + EqualMilkingSettings.hungerRateMultiplierPerStack.ToString(), 0.01f);
		sliderRect.y += UNIT_SIZE * 2;
		// 泌乳期意识/操纵/移动增益
		Rect rLactatingGain = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
		Widgets.CheckboxLabeled(rLactatingGain, "EM.LactatingGain".Translate(), ref EqualMilkingSettings.lactatingGainEnabled, false);
		{ string t = "EM.LactatingGainDesc".Translate(); TooltipHandler.TipRegion(rLactatingGain, string.IsNullOrEmpty(t) ? "EM.LactatingGainDesc" : t); }
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
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.AnimalAdultFemaleAlwaysLactating".Translate(), ref EqualMilkingSettings.femaleAnimalAdultAlwaysLactating);
		sliderRect.y += UNIT_SIZE;
		string prolactinLabel = EMDefOf.EM_Prolactin?.label ?? "Prolactin";
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Mechanoid), ref EqualMilkingSettings.showMechOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Colonist), ref EqualMilkingSettings.showColonistOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Slave), ref EqualMilkingSettings.showSlaveOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Prisoner), ref EqualMilkingSettings.showPrisonerOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Animal), ref EqualMilkingSettings.showAnimalOptions);
		sliderRect.y += UNIT_SIZE;
		Widgets.CheckboxLabeled(new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Misc), ref EqualMilkingSettings.showMiscOptions);
		sliderRect.y += UNIT_SIZE;
		// RJW 联动（仅当 RJW 激活时显示）
		if (ModLister.GetModWithIdentifier("rim.job.world") != null)
		{
			sliderRect.y += UNIT_SIZE;
			GUI.color = Color.gray;
			Widgets.Label(sliderRect, "EM.RJWSection".Translate());
			GUI.color = Color.white;
			sliderRect.y += UNIT_SIZE;
			Rect rRjwBreast = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwBreast, "EM.RjwBreastSize".Translate(), ref EqualMilkingSettings.rjwBreastSizeEnabled, false);
			{ string t = "EM.RjwBreastSizeDesc".Translate(); TooltipHandler.TipRegion(rRjwBreast, string.IsNullOrEmpty(t) ? "EM.RjwBreastSizeDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			Rect rRjwLust = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwLust, "EM.RjwLustFromNursing".Translate(), ref EqualMilkingSettings.rjwLustFromNursingEnabled, false);
			{ string t = "EM.RjwLustFromNursingDesc".Translate(); TooltipHandler.TipRegion(rRjwLust, string.IsNullOrEmpty(t) ? "EM.RjwLustFromNursingDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			Rect rRjwNeed = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwNeed, "EM.RjwSexNeedLactatingBonus".Translate(), ref EqualMilkingSettings.rjwSexNeedLactatingBonusEnabled, false);
			{ string t = "EM.RjwSexNeedLactatingBonusDesc".Translate(); TooltipHandler.TipRegion(rRjwNeed, string.IsNullOrEmpty(t) ? "EM.RjwSexNeedLactatingBonusDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			Rect rRjwSat = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwSat, "EM.RjwSexSatisfactionAfterNursing".Translate(), ref EqualMilkingSettings.rjwSexSatisfactionAfterNursingEnabled, false);
			{ string t = "EM.RjwSexSatisfactionAfterNursingDesc".Translate(); TooltipHandler.TipRegion(rRjwSat, string.IsNullOrEmpty(t) ? "EM.RjwSexSatisfactionAfterNursingDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			float fert = EqualMilkingSettings.rjwLactationFertilityFactor;
			Widgets.HorizontalSlider(sliderRect, ref fert, new FloatRange(0f, 1f), "EM.RjwLactationFertility".Translate(fert.ToStringPercent()), 0.05f);
			EqualMilkingSettings.rjwLactationFertilityFactor = fert;
			sliderRect.y += UNIT_SIZE;
			Rect rRjwInSex = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rRjwInSex, "EM.RjwLactatingInSexDesc".Translate(), ref EqualMilkingSettings.rjwLactatingInSexDescriptionEnabled, false);
			{ string t = "EM.RjwLactatingInSexDescDesc".Translate(); TooltipHandler.TipRegion(rRjwInSex, string.IsNullOrEmpty(t) ? "EM.RjwLactatingInSexDescDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			Rect rSexBoost = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rSexBoost, "EM.RjwSexAddsLactationBoost".Translate(), ref EqualMilkingSettings.rjwSexAddsLactationBoost, false);
			{ string t = "EM.RjwSexAddsLactationBoostDesc".Translate(); TooltipHandler.TipRegion(rSexBoost, string.IsNullOrEmpty(t) ? "EM.RjwSexAddsLactationBoostDesc" : t); }
			sliderRect.y += UNIT_SIZE;
			if (EqualMilkingSettings.rjwSexAddsLactationBoost)
			{
				Rect rDeltaS = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
				Widgets.Label(rDeltaS.LeftHalf(), "EM.RjwSexLactationBoostDeltaS".Translate(EqualMilkingSettings.rjwSexLactationBoostDeltaS.ToString("F2")));
				EqualMilkingSettings.rjwSexLactationBoostDeltaS = Widgets.HorizontalSlider(rDeltaS.RightHalf(), EqualMilkingSettings.rjwSexLactationBoostDeltaS, 0.05f, 0.5f, true);
				sliderRect.y += UNIT_SIZE;
			}
		}
		// Dubs Bad Hygiene 联动：乳腺炎/堵塞的卫生触发
		if (DubsBadHygieneIntegration.IsDubsBadHygieneActive())
		{
			sliderRect.y += UNIT_SIZE;
			GUI.color = Color.gray;
			Widgets.Label(sliderRect, "EM.DubsBadHygieneSection".Translate());
			GUI.color = Color.white;
			sliderRect.y += UNIT_SIZE;
			Rect rDbhMastitis = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, UNIT_SIZE);
			Widgets.CheckboxLabeled(rDbhMastitis, "EM.UseDubsBadHygieneForMastitis".Translate(), ref EqualMilkingSettings.useDubsBadHygieneForMastitis, false);
			{ string t = "EM.UseDubsBadHygieneForMastitisDesc".Translate(); TooltipHandler.TipRegion(rDbhMastitis, string.IsNullOrEmpty(t) ? "EM.UseDubsBadHygieneForMastitisDesc" : t); }
		}
		// 乳腺炎与耐受、溢出与 AI：可折叠区块
		Rect listRect = new Rect(sliderRect.x, sliderRect.y, scrollContent.width, contentHeight - sliderRect.y);
		Listing_Standard list = new Listing_Standard();
		list.Begin(listRect);
		// 建议 22：从 Def 加载默认（Def 可被其他 mod patch）
		Rect rLoadDef = list.GetRect(UNIT_SIZE);
		if (Widgets.ButtonText(rLoadDef, "EM.LoadDefaultsFromDef".Translate()))
			EqualMilkingSettings.ApplyDefaultsFromDef();
		TooltipHandler.TipRegion(rLoadDef, "EM.LoadDefaultsFromDefDesc".Translate());
		list.Gap(6f);
		list.Label("EM.MastitisAndToleranceSection".Translate());
		list.Gap(4f);
		{
			Rect rAllowMastitis = list.GetRect(UNIT_SIZE);
			bool allowMastitis = EqualMilkingSettings.allowMastitis;
			Widgets.CheckboxLabeled(rAllowMastitis, "EM.AllowMastitis".Translate(), ref allowMastitis, false);
			EqualMilkingSettings.allowMastitis = allowMastitis;
			TooltipHandler.TipRegion(rAllowMastitis, "EM.AllowMastitisDesc".Translate());
			list.Gap(6f);
			Rect rMtb = list.GetRect(UNIT_SIZE);
			float mastitisBaseMtbDays = EqualMilkingSettings.mastitisBaseMtbDays;
			Widgets.HorizontalSlider(rMtb, ref mastitisBaseMtbDays, new FloatRange(0.2f, 10f), "EM.MastitisBaseMtbDays".Translate(EqualMilkingSettings.mastitisBaseMtbDays.ToString("F1")), 0.1f);
			EqualMilkingSettings.mastitisBaseMtbDays = mastitisBaseMtbDays;
			list.Gap(6f);
			Rect rOverFull = list.GetRect(UNIT_SIZE);
			float overFullnessRiskMultiplier = EqualMilkingSettings.overFullnessRiskMultiplier;
			Widgets.HorizontalSlider(rOverFull, ref overFullnessRiskMultiplier, new FloatRange(0.5f, 5f), "EM.OverFullnessRiskMultiplier".Translate(EqualMilkingSettings.overFullnessRiskMultiplier.ToString("F1")), 0.1f);
			EqualMilkingSettings.overFullnessRiskMultiplier = overFullnessRiskMultiplier;
			list.Gap(6f);
			Rect rHygiene = list.GetRect(UNIT_SIZE);
			float hygieneRiskMultiplier = EqualMilkingSettings.hygieneRiskMultiplier;
			Widgets.HorizontalSlider(rHygiene, ref hygieneRiskMultiplier, new FloatRange(0.5f, 3f), "EM.HygieneRiskMultiplier".Translate(EqualMilkingSettings.hygieneRiskMultiplier.ToString("F1")), 0.1f);
			EqualMilkingSettings.hygieneRiskMultiplier = hygieneRiskMultiplier;
			list.Gap(6f);
			Rect rMtbHuman = list.GetRect(UNIT_SIZE);
			float mastitisMtbDaysMultiplierHumanlike = EqualMilkingSettings.mastitisMtbDaysMultiplierHumanlike;
			Widgets.HorizontalSlider(rMtbHuman, ref mastitisMtbDaysMultiplierHumanlike, new FloatRange(0.1f, 3f), "EM.MastitisMtbDaysMultiplierHumanlike".Translate(EqualMilkingSettings.mastitisMtbDaysMultiplierHumanlike.ToString("F2")), 0.05f);
			EqualMilkingSettings.mastitisMtbDaysMultiplierHumanlike = mastitisMtbDaysMultiplierHumanlike;
			TooltipHandler.TipRegion(rMtbHuman, "EM.MastitisMtbDaysMultiplierHumanlikeDesc".Translate());
			list.Gap(6f);
			Rect rMtbAnimal = list.GetRect(UNIT_SIZE);
			float mastitisMtbDaysMultiplierAnimal = EqualMilkingSettings.mastitisMtbDaysMultiplierAnimal;
			Widgets.HorizontalSlider(rMtbAnimal, ref mastitisMtbDaysMultiplierAnimal, new FloatRange(0.1f, 3f), "EM.MastitisMtbDaysMultiplierAnimal".Translate(EqualMilkingSettings.mastitisMtbDaysMultiplierAnimal.ToString("F2")), 0.05f);
			EqualMilkingSettings.mastitisMtbDaysMultiplierAnimal = mastitisMtbDaysMultiplierAnimal;
			TooltipHandler.TipRegion(rMtbAnimal, "EM.MastitisMtbDaysMultiplierAnimalDesc".Translate());
			list.Gap(6f);
			Rect rToleranceAffect = list.GetRect(UNIT_SIZE);
			bool allowToleranceAffectMilk = EqualMilkingSettings.allowToleranceAffectMilk;
			Widgets.CheckboxLabeled(rToleranceAffect, "EM.AllowToleranceAffectMilk".Translate(), ref allowToleranceAffectMilk, false);
			EqualMilkingSettings.allowToleranceAffectMilk = allowToleranceAffectMilk;
			TooltipHandler.TipRegion(rToleranceAffect, "EM.AllowToleranceAffectMilkDesc".Translate());
			list.Gap(6f);
			Rect rExp = list.GetRect(UNIT_SIZE);
			float toleranceFlowImpactExponent = EqualMilkingSettings.toleranceFlowImpactExponent;
			Widgets.HorizontalSlider(rExp, ref toleranceFlowImpactExponent, new FloatRange(0.1f, 3f), "EM.ToleranceFlowImpactExponent".Translate(EqualMilkingSettings.toleranceFlowImpactExponent.ToString("F1")), 0.1f);
			EqualMilkingSettings.toleranceFlowImpactExponent = toleranceFlowImpactExponent;
		}
		list.Gap(6f);
		list.Label("EM.OverflowFilthSection".Translate());
		list.Gap(4f);
		{
			string overflowFilth = EqualMilkingSettings.overflowFilthDefName ?? "";
			Rect rFilth = list.GetRect(UNIT_SIZE);
			Widgets.Label(rFilth.LeftHalf(), "EM.OverflowFilthDefName".Translate());
			overflowFilth = Widgets.TextField(rFilth.RightHalf(), overflowFilth, 64);
			EqualMilkingSettings.overflowFilthDefName = overflowFilth?.Trim() ?? "Filth_Vomit";
			list.Gap(6f);
			GUI.color = Color.gray;
			list.Label("EM.BaselineMilkDurationReference".Translate(EqualMilkingSettings.baselineMilkDurationDays.ToString("F0"), EqualMilkingSettings.birthInducedMilkDurationDays.ToString("F0")));
			GUI.color = Color.white;
			list.Gap(6f);
			Rect rAiFullness = list.GetRect(UNIT_SIZE);
			bool aiPreferHighFullnessTargets = EqualMilkingSettings.aiPreferHighFullnessTargets;
			Widgets.CheckboxLabeled(rAiFullness, "EM.AiPreferHighFullnessTargets".Translate(), ref aiPreferHighFullnessTargets, false);
			EqualMilkingSettings.aiPreferHighFullnessTargets = aiPreferHighFullnessTargets;
			TooltipHandler.TipRegion(rAiFullness, "EM.AiPreferHighFullnessTargetsDesc".Translate());
		}
		list.Gap(6f);
		list.Label("EM.RaceOverridesSection".Translate());
		list.Gap(4f);
		{
			string raceAlways = string.Join(", ", EqualMilkingSettings.raceCanAlwaysLactate ?? new List<string>());
			Rect rAlways = list.GetRect(UNIT_SIZE);
			Widgets.Label(rAlways.LeftHalf(), "EM.RaceCanAlwaysLactate".Translate());
			raceAlways = Widgets.TextField(rAlways.RightHalf(), raceAlways, 128);
			EqualMilkingSettings.raceCanAlwaysLactate = ParseCommaSeparatedDefNames(raceAlways);
			list.Gap(6f);
			string raceNever = string.Join(", ", EqualMilkingSettings.raceCannotLactate ?? new List<string>());
			Rect rNever = list.GetRect(UNIT_SIZE);
			Widgets.Label(rNever.LeftHalf(), "EM.RaceCannotLactate".Translate());
			raceNever = Widgets.TextField(rNever.RightHalf(), raceNever, 128);
			EqualMilkingSettings.raceCannotLactate = ParseCommaSeparatedDefNames(raceNever);
			list.Gap(6f);
			Rect rHumanlike = list.GetRect(UNIT_SIZE);
			float defaultFlowMultiplierForHumanlike = EqualMilkingSettings.defaultFlowMultiplierForHumanlike;
			Widgets.HorizontalSlider(rHumanlike, ref defaultFlowMultiplierForHumanlike, new FloatRange(0.25f, 2f), "EM.DefaultFlowMultiplierForHumanlike".Translate(EqualMilkingSettings.defaultFlowMultiplierForHumanlike.ToString("F2")), 0.05f);
			EqualMilkingSettings.defaultFlowMultiplierForHumanlike = defaultFlowMultiplierForHumanlike;
			TooltipHandler.TipRegion(rHumanlike, "EM.DefaultFlowMultiplierForHumanlikeDesc".Translate());
		}
		// DevMode：选中小人的泌乳状态（L、双池、E、卫生风险、溢出累计等）
		if (Prefs.DevMode)
		{
			list.Gap(6f);
			list.Label("EM.DevModeLactationPanel".Translate());
			list.Gap(4f);
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
		list.End();
		Widgets.EndScrollView();
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