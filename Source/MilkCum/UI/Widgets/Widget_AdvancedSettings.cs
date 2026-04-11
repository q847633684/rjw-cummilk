using System.Collections.Generic;
using System.Linq;
using System.Text;
using MilkCum.Core;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Lactation.Comps;
using MilkCum.Fluids.Lactation.Helpers;
using MilkCum.Fluids.Shared.Data;
using MilkCum.Core.Utils;
using MilkCum.Integration.DubsBadHygiene;
using MilkCum.Integration.RjwBallsOvaries;
using RimWorld;
using UnityEngine;
using Verse;

using static MilkCum.Core.Constants.Constants;

namespace MilkCum.UI;
public class Widget_AdvancedSettings
{
	/// <summary>泌乳相关设置页内二级标题：灰标题 + 可选说明，与下方控件成组。</summary>
	private static void DrawEmUISectionHeading(Listing_Standard list, string titleKey, string descKey = null, float gapBefore = 10f)
	{
		if (gapBefore > 0f)
			list.Gap(gapBefore);
		GUI.color = Color.gray;
		list.Label(titleKey.Translate());
		if (descKey != null)
		{
			string d = descKey.Translate();
			if (!string.IsNullOrEmpty(d))
				list.Label(d);
		}
		GUI.color = Color.white;
		list.Gap(4f);
	}

	/// <summary>与 RJW / 原版习惯一致：每子区段在视口高度之上额外需要的高度（上一帧由 Listing 测量）。</summary>
	private readonly Dictionary<int, float> _advancedSectionScrollExtras = new();

	private const float ScrollContentBottomPadding = 32f;
	private int _lastAdvancedScrollKey = -1;
	private Vector2 _sectionScrollPosition = Vector2.zero;

	// 健康子 Tab：乳腺炎（含卫生风险滑条与 DBH 说明）/ 溢出与杂项
	private const int SubTabHealth_Mastitis = 0;
	private const int SubTabHealth_Overflow = 1;
	// 数值平衡子 Tab（拟真 SYS 与导管/适应数值分栏，避免混在同一页）
	private const int SubTabBalance_Core = 0;
	private const int SubTabBalance_Realism = 1;
	private const int SubTabBalance_AdvancedModel = 2;
	private const int SubTabBalance_ColonistExtras = 3;
	// 外来工具：RJW / 营养系统（DBH 说明与卫生风险滑条在「健康风险 → 乳腺炎」）
	private const int SubTabIntegration_RJW = 0;
	private const int SubTabIntegration_Nutrition = 1;

	private static int MakeAdvancedScrollKey(int mainTab, int subTab) => unchecked(mainTab * 256 + subTab);

	/// <summary>按 mainTab 分发到泌乳子页、权限菜单、精液 Ballz、联动等（泌乳哺乳子 Tab 由 UI 直接绘制）。</summary>
	public void DrawSection(Rect inRect, int mainTab, int subTab)
	{
		int sectionKey = MakeAdvancedScrollKey(mainTab, subTab);
		if (sectionKey != _lastAdvancedScrollKey)
		{
			_sectionScrollPosition = Vector2.zero;
			_lastAdvancedScrollKey = sectionKey;
		}

		float topMargin = 36f;
		Rect scrollViewRect = new Rect(inRect.x, inRect.y + topMargin, inRect.width, inRect.height - topMargin);
		float viewH = Mathf.Max(1f, scrollViewRect.height);

		_advancedSectionScrollExtras.TryGetValue(sectionKey, out float scrollExtra);
		float contentH = viewH + scrollExtra;
		Rect scrollContent = new Rect(0f, 0f, scrollViewRect.width - 16f, contentH);

		float measuredH;
		Widgets.BeginScrollView(scrollViewRect, ref _sectionScrollPosition, scrollContent);
		try
		{
			measuredH = DrawAdvancedMainContent(scrollContent, mainTab, subTab);
		}
		finally
		{
			Widgets.EndScrollView();
		}

		float scrollSlack = Mathf.Max(0f, measuredH + ScrollContentBottomPadding - viewH);
		_advancedSectionScrollExtras[sectionKey] = scrollSlack;
		if (_sectionScrollPosition.y > scrollSlack)
			_sectionScrollPosition.y = scrollSlack;
	}

	private float DrawAdvancedMainContent(Rect content, int mainTab, int subTab)
	{
		if (mainTab == (int)MainTabIndex.Lactation)
		{
			// subTab 0：哺乳与营养 — 由 MilkCumSettings.UI 调用 Widget_BreastfeedSettings
			if (subTab == 1)
				return DrawBreastPoolMain(content, SubTabBalance_Core);
			if (subTab == 2)
				return DrawHealthSection(content, SubTabHealth_Mastitis);
			if (subTab == 3)
				return DrawHealthSection(content, SubTabHealth_Overflow);
			if (subTab == 4)
				return DrawBreastPoolMain(content, SubTabBalance_Realism);
			if (subTab == 5)
				return DrawBreastPoolMain(content, SubTabBalance_AdvancedModel);
			if (subTab == 6)
				return DrawBreastPoolMain(content, SubTabBalance_ColonistExtras);
			return content.height;
		}
		if (mainTab == (int)MainTabIndex.Semen && subTab == 1)
			return DrawSemenBallzSection(content);
		if (mainTab == (int)MainTabIndex.Permissions && subTab == 0)
			return DrawPermissionsMenuSection(content);
		if (mainTab == (int)MainTabIndex.Integrations)
			return DrawIntegrationSectionExtended(content, subTab);
		return content.height;
	}

	private static float DrawSemenBallzSection(Rect content)
	{
		var list = new Listing_Standard { maxOneColumn = true, ColumnWidth = content.width - 20f };
		list.Begin(content);
		list.Gap(6f);
		DrawBallzGonadsIntegrationBlock(list);
		list.End();
		return list.MaxColumnHeightSeen;
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
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MilkingActionLog".Translate(), ref MilkCumSettings.milkingActionLog);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.LogExternalFullnessBridge".Translate(), ref MilkCumSettings.logExternalFullnessBridge);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.LactationDrugIntakeLog".Translate(), ref MilkCumSettings.lactationDrugIntakeLog);
		list.Gap(4f);
		DrawOptionalModsDevBlock(list);
		list.Gap(6f);
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
			DrawDevBreastPoolTopologyBlock(list, sel);
			list.Gap(4f);
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

	/// <summary>与 RJW「Supported mods」类似：左列名称，右列已检测/未找到。</summary>
	private static void DrawOptionalModsDevBlock(Listing_Standard list)
	{
		list.Gap(4f);
		GUI.color = Color.gray;
		Rect title = list.Label("EM.OptionalModsSectionTitle".Translate());
		GUI.color = Color.white;
		TooltipHandler.TipRegion(title, "EM.OptionalModsSectionTip".Translate());
		foreach ((string labelKey, bool active) in OptionalModDevRows())
		{
			Rect rect = list.GetRect(Text.LineHeight);
			rect.SplitVertically(rect.width / 2f, out Rect leftCell, out Rect rightCell);
			Widgets.Label(leftCell, labelKey.Translate());
			GUI.contentColor = active ? new Color(0.35f, 0.85f, 0.4f) : new Color(1f, 0.82f, 0.25f);
			Widgets.Label(rightCell, active ? "EM.ModStatusDetected".Translate() : "EM.ModStatusNotFound".Translate());
			GUI.contentColor = Color.white;
		}
	}

	private static IEnumerable<(string LabelKey, bool Active)> OptionalModDevRows()
	{
		yield return ("EM.OptionalMod_RJW", ModIntegrationGates.RjwModActive);
		yield return ("EM.OptionalMod_DBH", DubsBadHygieneIntegration.IsDubsBadHygieneActive());
		yield return ("EM.OptionalMod_VEFCore", ModLister.GetModWithIdentifier("oskarpotocki.vanillafactionsexpanded.core") != null);
		yield return ("EM.OptionalMod_RJWGenes", ModLister.GetModWithIdentifier("vegapnk.rjw.genes") != null);
		yield return ("EM.OptionalMod_BallzGonads", RjwBallsOvariesIntegration.IsModActive);
	}

	private static string AbbrevPoolKeyForDev(string key)
	{
		if (string.IsNullOrEmpty(key)) return "∅";
		if (key.Length <= 32) return key;
		return key.Substring(0, 18) + "…" + key.Substring(key.Length - 12);
	}

	/// <summary>Dev：当前选中小人的全局拓扑模式 + 各池条目 Key/Site 缩写，便于与日志自检对照。</summary>
	private static void DrawDevBreastPoolTopologyBlock(Listing_Standard list, Pawn pawn)
	{
		GUI.color = Color.gray;
		list.Label("EM.DevModeBreastPoolTopology".Translate());
		GUI.color = Color.white;
		if (!ModIntegrationGates.RjwModActive)
		{
			list.Label("EM.DevModeBreastPoolTopologyDisabled".Translate());
			return;
		}

		list.Label("EM.BreastPoolLayoutVirtualLRFixed".Translate());
		var entries = pawn.GetResolvedBreastPoolEntries();
		if (entries == null || entries.Count == 0)
		{
			list.Label("EM.DevModeBreastPoolTopologyNoEntries".Translate());
			return;
		}

		var sb = new StringBuilder();
		sb.AppendLine("EM.DevModeBreastPoolTopologyEntriesHeader".Translate(entries.Count));
		for (int i = 0; i < entries.Count; i++)
		{
			FluidPoolEntry e = entries[i];
			string part = e.SourcePart?.def?.defName ?? "-";
			sb.AppendLine($"  #{i} Site={e.Site} anatomL={e.IsLeft} idx={e.PoolIndex} cap={e.Capacity:F3} {part} key=`{AbbrevPoolKeyForDev(e.Key)}`");
		}

		sb.AppendLine();
		sb.Append("EM.DevModeBreastPoolUiHint".Translate());
		string body = sb.ToString();
		float h = Text.CalcHeight(body, list.ColumnWidth);
		const float maxDevTopologyHeight = 240f;
		Rect r = list.GetRect(Mathf.Min(h, maxDevTopologyHeight));
		Widgets.Label(r, body);
		if (h > maxDevTopologyHeight)
			list.Label("EM.DevModeBreastPoolTopologyTruncated".Translate());
	}

	private float DrawHealthSection(Rect content, int subTab)
	{
		var list = new Listing_Standard();
		list.Begin(content);
		if (subTab == SubTabHealth_Mastitis)
			DrawMastitisBlock(list);
		else if (subTab == SubTabHealth_Overflow)
			DrawOverflowBlock(list);
		list.End();
		return list.MaxColumnHeightSeen;
	}

	private void DrawMastitisBlock(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_Mastitis".Translate());
		GUI.color = Color.white;
		list.Gap(6f);
		DrawEmUISectionHeading(list, "EM.UISection.MastitisSwitches", "EM.UISection.MastitisSwitchesDesc", 0f);
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
		DrawEmUISectionHeading(list, "EM.UISection.MastitisMtbAndRisk", "EM.UISection.MastitisMtbAndRiskDesc", 0f);
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

	private void DrawOverflowBlock(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.ToleranceVanillaOnlyBody".Translate());
		GUI.color = Color.white;
		list.Gap(6f);
		DrawEmUISectionHeading(list, "EM.UISection.OverflowFilthAndLetters", "EM.UISection.OverflowFilthAndLettersDesc", 0f);
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
		DrawEmUISectionHeading(list, "EM.UISection.DrugTimingReference", "EM.BreastPoolParamsInToleranceHint", 0f);
		DrawEmUISectionHeading(list, "EM.UISection.MilkingAiPreference", "EM.UISection.MilkingAiPreferenceDesc", 0f);
		Rect rAiFullness = list.GetRect(UNIT_SIZE);
		bool aiPreferHighFullnessTargets = MilkCumSettings.aiPreferHighFullnessTargets;
		Widgets.CheckboxLabeled(rAiFullness, "EM.AiPreferHighFullnessTargets".Translate(), ref aiPreferHighFullnessTargets, false);
		MilkCumSettings.aiPreferHighFullnessTargets = aiPreferHighFullnessTargets;
		TooltipHandler.TipRegion(rAiFullness, "EM.AiPreferHighFullnessTargetsDesc".Translate());
	}

	private float DrawPermissionsMenuSection(Rect content)
	{
		var list = new Listing_Standard();
		list.Begin(content);
		GUI.color = Color.gray;
		list.Label("EM.SectionDesc_IdentityAndMenu".Translate());
		GUI.color = Color.white;
		list.Gap(6f);
		DrawEmUISectionHeading(list, "EM.UISection.PermProducerNote", "EM.ProducerRestrictionsHint", 0f);
		DrawEmUISectionHeading(list, "EM.DefaultSucklerRulesSection", null, 0f);
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
		DrawEmUISectionHeading(list, "EM.UISection.PermDrugRoles", "EM.DrugRoleHint", 0f);
		DrawEmUISectionHeading(list, "EM.UISection.PermProlactinMenu", "EM.UISection.PermProlactinMenuDesc", 0f);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Mechanoid), ref MilkCumSettings.showMechOptions);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Colonist), ref MilkCumSettings.showColonistOptions);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Slave), ref MilkCumSettings.showSlaveOptions);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Prisoner), ref MilkCumSettings.showPrisonerOptions);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Animal), ref MilkCumSettings.showAnimalOptions);
		Widgets.CheckboxLabeled(list.GetRect(UNIT_SIZE), "EM.MenuProlactinShow".Translate(Lang.Misc), ref MilkCumSettings.showMiscOptions);
		list.End();
		return list.MaxColumnHeightSeen;
	}

	private float DrawBreastPoolMain(Rect content, int balanceSubTab)
	{
		var list = new Listing_Standard();
		list.Begin(content);
		switch (balanceSubTab)
		{
			case SubTabBalance_Core:
				DrawBreastPoolBalancePageHeader(list);
				DrawBreastPoolTabCore(list);
				break;
			case SubTabBalance_Realism:
				GUI.color = Color.gray;
				list.Label("EM.UISection.BalanceSubTabRealismBlurb".Translate());
				GUI.color = Color.white;
				list.Gap(6f);
				DrawBreastPoolTabRealism(list);
				break;
			case SubTabBalance_AdvancedModel:
				GUI.color = Color.gray;
				list.Label("EM.UISection.BalanceSubTabAdvancedBlurb".Translate());
				GUI.color = Color.white;
				list.Gap(6f);
				DrawBreastPoolTabAdvancedModel(list);
				break;
			case SubTabBalance_ColonistExtras:
				GUI.color = Color.gray;
				list.Label("EM.UISection.BalanceSubTabColonistBlurb".Translate());
				GUI.color = Color.white;
				list.Gap(6f);
				DrawBreastPoolTabColonistExtras(list);
				break;
		}
		list.End();
		return list.MaxColumnHeightSeen;
	}

	private static void DrawBreastPoolBalancePageHeader(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.UISection.BalanceBreastPoolPage".Translate());
		list.Label("EM.SectionDesc_BreastPool".Translate());
		list.Label("EM.UISection.BalanceVsRealismHint".Translate());
		GUI.color = Color.white;
		list.Gap(6f);
	}

	/// <summary>数值平衡 · 生理拟真：SYS 开关与配套滑条（与导管/组织适应数值分栏）。</summary>
	private static void DrawBreastPoolTabRealism(Listing_Standard list) => DrawRealismSettingsBlock(list);

	/// <summary>外来工具：RJW 与营养系统两子页；第三方桥接勾选对两页均显示在顶部。</summary>
	private float DrawIntegrationSectionExtended(Rect content, int subTab)
	{
		var list = new Listing_Standard();
		list.Begin(content);
		DrawEmUISectionHeading(list, "EM.UISection.ThirdPartyLactationBridge", "EM.UISection.ThirdPartyLactationBridgeDesc", 0f);
		Rect rBridge = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rBridge, "EM.BridgeExternalCompMilkableFullness".Translate(), ref MilkCumSettings.bridgeExternalCompMilkableFullness, false);
		TooltipHandler.TipRegion(rBridge, "EM.BridgeExternalCompMilkableFullnessDesc".Translate());
		Rect rCh = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rCh, "EM.BridgeExternalLactatingCharge".Translate(), ref MilkCumSettings.bridgeExternalLactatingCharge, false);
		TooltipHandler.TipRegion(rCh, "EM.BridgeExternalLactatingChargeDesc".Translate());
		list.Gap(6f);
		if (subTab == SubTabIntegration_RJW)
		{
			if (ModIntegrationGates.RjwModActive)
			{
				GUI.color = Color.gray;
				list.Label("EM.SectionDesc_RJW".Translate());
				GUI.color = Color.white;
				list.Gap(6f);
				DrawRjwBlock(list);
			}
			else
			{
				GUI.color = Color.gray;
				list.Label("EM.RequiresRJWMod".Translate());
				GUI.color = Color.white;
			}
		}
		else if (subTab == SubTabIntegration_Nutrition)
			DrawNutritionBlock(list);
		list.End();
		return list.MaxColumnHeightSeen;
	}

	private static void DrawRealismSettingsBlock(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.Realism.Section".Translate());
		list.Label("EM.Realism.SectionDesc".Translate());
		GUI.color = Color.white;
		list.Gap(4f);
		void Toggle(string key, ref bool v)
		{
			Rect r = list.GetRect(UNIT_SIZE);
			Widgets.CheckboxLabeled(r, key.Translate(), ref v, false);
		}
		Toggle("EM.Realism.ComplianceInflow", ref MilkCumSettings.realismComplianceInflow);
		Toggle("EM.Realism.StretchBuffer", ref MilkCumSettings.realismStretchBuffer);
		Toggle("EM.Realism.RjwStretchMilestone", ref MilkCumSettings.realismRjwStretchMilestone);
		Toggle("EM.Realism.RjwStretchPerSideSync", ref MilkCumSettings.realismRjwStretchPerSideSync);
		Toggle("EM.Realism.MetabolicGate", ref MilkCumSettings.realismMetabolicGate);
		Toggle("EM.Realism.PoolFedSelf", ref MilkCumSettings.realismPoolFedSelf);
		Toggle("EM.Realism.ReflexLeak", ref MilkCumSettings.realismReflexLeak);
		Toggle("EM.Realism.EmptyStasisCoupling", ref MilkCumSettings.realismEmptyStasisCoupling);
		Toggle("EM.Realism.StimulusSource", ref MilkCumSettings.realismStimulusSource);
		Toggle("EM.Realism.StressLetdown", ref MilkCumSettings.realismStressLetdown);
		Toggle("EM.Realism.WeaningCurve", ref MilkCumSettings.realismWeaningCurve);
		Toggle("EM.Realism.Circadian", ref MilkCumSettings.realismCircadian);
		Toggle("EM.Realism.LactationEstablishment", ref MilkCumSettings.realismLactationEstablishment);
		list.Gap(4f);
		float ce = MilkCumSettings.realismComplianceExponent;
		Widgets.HorizontalSlider(list.GetRect(UNIT_SIZE), ref ce, new FloatRange(0.5f, 3f), "EM.Realism.ComplianceExponent".Translate(ce.ToString("F2")), 0.05f);
		MilkCumSettings.realismComplianceExponent = ce;
		float se = MilkCumSettings.realismStretchExtraFraction;
		Widgets.HorizontalSlider(list.GetRect(UNIT_SIZE), ref se, new FloatRange(0.05f, 0.45f), "EM.Realism.StretchExtraFraction".Translate(se.ToString("F2")), 0.01f);
		MilkCumSettings.realismStretchExtraFraction = se;
		float pft = MilkCumSettings.realismPoolFedSelfFoodTarget;
		Widgets.HorizontalSlider(list.GetRect(UNIT_SIZE), ref pft, new FloatRange(0.1f, 0.9f), "EM.Realism.PoolFedTarget".Translate(pft.ToString("F2")), 0.02f);
		MilkCumSettings.realismPoolFedSelfFoodTarget = pft;
		float wh = MilkCumSettings.realismWeaningHalfLifeDays;
		Widgets.HorizontalSlider(list.GetRect(UNIT_SIZE), ref wh, new FloatRange(0.25f, 8f), "EM.Realism.WeaningHalfLifeDays".Translate(wh.ToString("F2")), 0.05f);
		MilkCumSettings.realismWeaningHalfLifeDays = wh;
		float ca = MilkCumSettings.realismCircadianAmplitude;
		Widgets.HorizontalSlider(list.GetRect(UNIT_SIZE), ref ca, new FloatRange(0f, 0.25f), "EM.Realism.CircadianAmplitude".Translate(ca.ToString("F2")), 0.01f);
		MilkCumSettings.realismCircadianAmplitude = ca;
		float ed = MilkCumSettings.realismEstablishmentDays;
		Widgets.HorizontalSlider(list.GetRect(UNIT_SIZE), ref ed, new FloatRange(0.5f, 14f), "EM.Realism.EstablishmentDays".Translate(ed.ToString("F1")), 0.1f);
		MilkCumSettings.realismEstablishmentDays = ed;
		float em = MilkCumSettings.realismEstablishmentMinMult;
		Widgets.HorizontalSlider(list.GetRect(UNIT_SIZE), ref em, new FloatRange(0.05f, 1f), "EM.Realism.EstablishmentMinMult".Translate(em.ToString("F2")), 0.02f);
		MilkCumSettings.realismEstablishmentMinMult = em;
		GUI.color = Color.gray;
		list.Label("EM.Realism.LactationEstablishmentDesc".Translate());
		GUI.color = Color.white;
	}

	private void DrawNutritionBlock(Listing_Standard list)
	{
		DrawEmUISectionHeading(list, "EM.UISection.NutritionBasics", "EM.SectionDesc_NutritionIntegration", 0f);
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

	/// <summary>数值平衡 · 核心：容量拓扑、RJW 胸形/阶段、挤奶、满池渗漏、L 上限。</summary>
	private void DrawBreastPoolTabCore(Listing_Standard list)
	{
		DrawEmUISectionHeading(list, "EM.UISection.PoolCapacity", "EM.UISection.PoolCapacityDesc", 0f);
		GUI.color = Color.gray;
		list.Label("EM.RjwBreastSizeSectionHint".Translate());
		if (ModIntegrationGates.RjwModActive)
			list.Label("EM.RjwBreastPoolAutoLinked".Translate());
		list.Label("EM.BreastPoolBaseCapacityRjwVolumeOnly".Translate());
		GUI.color = Color.white;
		list.Gap(4f);
		list.Label("EM.BreastPoolLayoutVirtualLRFixed".Translate());
		list.Gap(4f);
		Rect rCapCoeff = list.GetRect(UNIT_SIZE);
		float capCoeff = MilkCumSettings.rjwBreastCapacityCoefficient;
		Widgets.HorizontalSlider(rCapCoeff, ref capCoeff, new FloatRange(0.25f, 4f), "EM.RjwBreastCapacityCoefficient".Translate(MilkCumSettings.rjwBreastCapacityCoefficient.ToString("F2")), 0.05f);
		MilkCumSettings.rjwBreastCapacityCoefficient = capCoeff;
		TooltipHandler.TipRegion(rCapCoeff, "EM.RjwBreastCapacityCoefficientDescLong".Translate());
		list.Gap(6f);

		DrawEmUISectionHeading(list, "EM.UISection.RjwShapeAndStageFlow", "EM.UISection.RjwShapeAndStageFlowDesc", 0f);
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

		DrawEmUISectionHeading(list, "EM.UISection.MilkingThroughput", "EM.UISection.MilkingThroughputDesc", 0f);
		Rect rFlowMult = list.GetRect(UNIT_SIZE);
		float defaultFlowMultiplierForHumanlike = MilkCumSettings.defaultFlowMultiplierForHumanlike;
		Widgets.HorizontalSlider(rFlowMult, ref defaultFlowMultiplierForHumanlike, new FloatRange(0.25f, 2f), "EM.DefaultFlowMultiplierForHumanlike".Translate(MilkCumSettings.defaultFlowMultiplierForHumanlike.ToString("F2")), 0.05f);
		MilkCumSettings.defaultFlowMultiplierForHumanlike = defaultFlowMultiplierForHumanlike;
		TooltipHandler.TipRegion(rFlowMult, "EM.DefaultFlowMultiplierForHumanlikeDescLong".Translate());
		list.Gap(6f);
		Rect rWorkBase = list.GetRect(UNIT_SIZE);
		float milkingWorkBase = MilkCumSettings.milkingWorkTotalBase;
		Widgets.HorizontalSlider(rWorkBase, ref milkingWorkBase, new FloatRange(15f, 180f), "EM.MilkingWorkTotalBase".Translate(milkingWorkBase.ToString("F0")), 1f);
		MilkCumSettings.milkingWorkTotalBase = Mathf.Max(0.01f, milkingWorkBase);
		TooltipHandler.TipRegion(rWorkBase, "EM.MilkingWorkTotalBaseDesc".Translate());
		list.Gap(6f);

		DrawEmUISectionHeading(list, "EM.UISection.OverflowResidual", "EM.UISection.OverflowResidualDesc", 0f);
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

		DrawEmUISectionHeading(list, "EM.UISection.DrugLactationLevelCap", "EM.UISection.DrugLactationLevelCapDesc", 0f);
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
	}

	/// <summary>数值平衡 · 导管与模型：示意图、组织适应、导管进出与压力/导通（强度与节奏类滑条）。</summary>
	private void DrawBreastPoolTabAdvancedModel(Listing_Standard list)
	{
		DrawEmUISectionHeading(list, "EM.UISection.PoolSchematicPreview", "EM.UISection.PoolSchematicPreviewDesc", 0f);
		Rect rSchematic = list.GetRect(298f);
		LactationPoolSchematicGraph.DrawInRect(rSchematic);
		list.Gap(6f);

		DrawEmUISectionHeading(list, "EM.UISection.TissueAdaptation", "EM.UISection.TissueAdaptationDesc", 0f);
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
		list.Gap(6f);

		DrawEmUISectionHeading(list, "EM.UISection.DuctInflowRouting", "EM.UISection.DuctInflowRoutingDesc", 0f);
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
		Rect rSharedBudget = list.GetRect(UNIT_SIZE);
		bool sharedBudget = MilkCumSettings.inflowSharedMammaryBudget;
		Widgets.CheckboxLabeled(rSharedBudget, "EM.InflowSharedMammaryBudget".Translate(), ref sharedBudget, false);
		MilkCumSettings.inflowSharedMammaryBudget = sharedBudget;
		TooltipHandler.TipRegion(rSharedBudget, "EM.InflowSharedMammaryBudgetDesc".Translate());
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
		list.Gap(6f);

		DrawEmUISectionHeading(list, "EM.UISection.DuctDrainAndMachine", "EM.UISection.DuctDrainAndMachineDesc", 0f);
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
	}

	/// <summary>数值平衡 · 殖民者加成：泌乳属性增益与参数提示。</summary>
	private void DrawBreastPoolTabColonistExtras(Listing_Standard list)
	{
		DrawEmUISectionHeading(list, "EM.UISection.LactationColonistStatGain", "EM.UISection.LactationColonistStatGainDesc", 0f);
		Rect rLactatingGain = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rLactatingGain, "EM.LactatingGain".Translate(), ref MilkCumSettings.lactatingGainEnabled, false);
		{ string t = "EM.LactatingGainDesc".Translate(); TooltipHandler.TipRegion(rLactatingGain, string.IsNullOrEmpty(t) ? "EM.LactatingGainDesc" : t); }
		list.Gap(4f);
		float pct = MilkCumSettings.lactatingGainCapModPercent;
		Rect rPct = list.GetRect(UNIT_SIZE);
		Widgets.HorizontalSlider(rPct, ref pct, new FloatRange(0f, 0.20f), "EM.LactatingGainPercent".Translate(pct.ToStringPercent()), 0.01f);
		MilkCumSettings.lactatingGainCapModPercent = pct;
		list.Gap(8f);
		GUI.color = Color.gray;
		list.Label("EM.BreastPoolParamsHint".Translate());
		GUI.color = Color.white;
	}

	private void DrawRjwBlock(Listing_Standard list)
	{
		GUI.color = Color.gray;
		list.Label("EM.RjwIntegrationBreastPoolNavHint".Translate("EM.Tab.Lactation".Translate(), "EM.SubTab.BalanceCore".Translate()));
		list.Label("EM.RjwIntegrationFeaturesAutoOn".Translate());
		GUI.color = Color.white;
		list.Gap(6f);
		DrawEmUISectionHeading(list, "EM.UISection.RjwFertilityAndSexBoost", "EM.UISection.RjwFertilityAndSexBoostDesc", 0f);
		float fert = MilkCumSettings.rjwLactationFertilityFactor;
		Rect rFert = list.GetRect(UNIT_SIZE);
		Widgets.HorizontalSlider(rFert, ref fert, new FloatRange(0f, 1f), "EM.RjwLactationFertility".Translate(fert.ToStringPercent()), 0.05f);
		MilkCumSettings.rjwLactationFertilityFactor = fert;
		list.Gap(6f);
		Rect rSat = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rSat, "EM.RjwSexSatisfactionAfterNursing".Translate(), ref MilkCumSettings.rjwSexSatisfactionAfterNursingEnabled, false);
		TooltipHandler.TipRegion(rSat, "EM.RjwSexSatisfactionAfterNursingDesc".Translate());
		list.Gap(4f);
		Rect rInSex = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rInSex, "EM.RjwLactatingInSexDesc".Translate(), ref MilkCumSettings.rjwLactatingInSexDescriptionEnabled, false);
		TooltipHandler.TipRegion(rInSex, "EM.RjwLactatingInSexDescDesc".Translate());
		list.Gap(4f);
		Rect rSexBoost = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rSexBoost, "EM.RjwSexAddsLactationBoost".Translate(), ref MilkCumSettings.rjwSexAddsLactationBoost, false);
		TooltipHandler.TipRegion(rSexBoost, "EM.RjwSexAddsLactationBoostDesc".Translate());
		list.Gap(4f);
		if (MilkCumSettings.rjwSexAddsLactationBoost)
		{
			Rect rDeltaS = list.GetRect(UNIT_SIZE);
			Widgets.Label(rDeltaS.LeftHalf(), "EM.RjwSexLactationBoostDeltaS".Translate(MilkCumSettings.rjwSexLactationBoostDeltaS.ToString("F2")));
			MilkCumSettings.rjwSexLactationBoostDeltaS = Widgets.HorizontalSlider(rDeltaS.RightHalf(), MilkCumSettings.rjwSexLactationBoostDeltaS, 0f, 0.5f, true);
			TooltipHandler.TipRegion(rDeltaS, "EM.RjwSexLactationBoostDeltaSDesc".Translate());
		}
		list.Gap(6f);
		DrawEmUISectionHeading(list, "EM.RjwPermanentBreastGainSection", "EM.RjwPermanentBreastGainDesc", 0f);
		bool rjwBreastGui = GUI.enabled;
		GUI.enabled = ModIntegrationGates.RjwModActive;
		Rect rPerm = list.GetRect(UNIT_SIZE);
		bool permanentBreastGain = MilkCumSettings.rjwPermanentBreastGainFromLactationEnabled;
		Widgets.CheckboxLabeled(rPerm, "EM.RjwPermanentBreastGain".Translate(), ref permanentBreastGain, false);
		MilkCumSettings.rjwPermanentBreastGainFromLactationEnabled = permanentBreastGain;
		TooltipHandler.TipRegion(rPerm, "EM.RjwPermanentBreastGainDesc".Translate());
		if (MilkCumSettings.rjwPermanentBreastGainFromLactationEnabled)
		{
			list.Gap(4f);
			Rect rDaysM = list.GetRect(UNIT_SIZE);
			float daysPerMilestone = MilkCumSettings.rjwPermanentBreastGainDaysPerMilestone;
			Widgets.HorizontalSlider(rDaysM, ref daysPerMilestone, new FloatRange(1f, 60f), "EM.RjwPermanentBreastGainDays".Translate(daysPerMilestone.ToString("F1")), 0.5f);
			MilkCumSettings.rjwPermanentBreastGainDaysPerMilestone = Mathf.Max(0.1f, daysPerMilestone);
			TooltipHandler.TipRegion(rDaysM, "EM.RjwPermanentBreastGainDaysDesc".Translate());
			list.Gap(4f);
			Rect rSevDelta = list.GetRect(UNIT_SIZE);
			float sevDelta = MilkCumSettings.rjwPermanentBreastGainSeverityDelta;
			Widgets.HorizontalSlider(rSevDelta, ref sevDelta, new FloatRange(0.01f, 0.15f), "EM.RjwPermanentBreastGainDelta".Translate(sevDelta.ToString("F3")), 0.005f);
			MilkCumSettings.rjwPermanentBreastGainSeverityDelta = Mathf.Clamp(sevDelta, 0.001f, 1f);
			TooltipHandler.TipRegion(rSevDelta, "EM.RjwPermanentBreastGainDeltaDesc".Translate());
		}
		GUI.enabled = rjwBreastGui;
	}

	private static void DrawBallzGonadsIntegrationBlock(Listing_Standard list)
	{
		list.Gap(8f);
		if (!RjwBallsOvariesIntegration.IsModActive)
		{
			GUI.color = Color.gray;
			list.Label("EM.BallzIntegrationNotDetected".Translate());
			GUI.color = Color.white;
			return;
		}

		DrawEmUISectionHeading(list, "EM.UISection.BallzGonads", "EM.UISection.BallzGonadsDesc", 0f);
		Rect rEn = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rEn, "EM.Ballz.Enable".Translate(), ref MilkCumSettings.Integration_Ballz_Enable, false);
		TooltipHandler.TipRegion(rEn, "EM.Ballz.EnableDesc".Translate());
		if (!MilkCumSettings.Integration_Ballz_Enable)
			return;
		Rect rCap = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rCap, "EM.Ballz.GonadSemenCapacity".Translate(), ref MilkCumSettings.Integration_Ballz_GonadSemenCapacity, false);
		TooltipHandler.TipRegion(rCap, "EM.Ballz.GonadSemenCapacityDesc".Translate());
		Rect rT = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rT, "EM.Ballz.TestosteroneRefill".Translate(), ref MilkCumSettings.Integration_Ballz_TestosteroneRefill, false);
		TooltipHandler.TipRegion(rT, "EM.Ballz.TestosteroneRefillDesc".Translate());
		if (MilkCumSettings.Integration_Ballz_TestosteroneRefill)
		{
			Rect rTp = list.GetRect(UNIT_SIZE);
			Widgets.CheckboxLabeled(rTp, "EM.Ballz.TestosteroneRequiresPenisPool".Translate(), ref MilkCumSettings.Integration_Ballz_TestosteroneRequiresPenisPool, false);
			TooltipHandler.TipRegion(rTp, "EM.Ballz.TestosteroneRequiresPenisPoolDesc".Translate());
		}
		Rect rN = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rN, "EM.Ballz.NeuteredNoSemen".Translate(), ref MilkCumSettings.Integration_Ballz_NeuteredNoSemen, false);
		TooltipHandler.TipRegion(rN, "EM.Ballz.NeuteredNoSemenDesc".Translate());
		Rect rE = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rE, "EM.Ballz.ElastrationPenalty".Translate(), ref MilkCumSettings.Integration_Ballz_ElastrationPenalty, false);
		TooltipHandler.TipRegion(rE, "EM.Ballz.ElastrationPenaltyDesc".Translate());
		Rect rG = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rG, "EM.Ballz.TesticleGear".Translate(), ref MilkCumSettings.Integration_Ballz_TesticleGear, false);
		TooltipHandler.TipRegion(rG, "EM.Ballz.TesticleGearDesc".Translate());
		Rect rL = list.GetRect(UNIT_SIZE);
		Widgets.CheckboxLabeled(rL, "EM.Ballz.EstrogenLactation".Translate(), ref MilkCumSettings.Integration_Ballz_EstrogenLactation, false);
		TooltipHandler.TipRegion(rL, "EM.Ballz.EstrogenLactationDesc".Translate());
	}

	// Draw(Rect) removed; use DrawSection(mainTab, subTab) instead.
}
