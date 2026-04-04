using System.Collections.Generic;
using MilkCum.Core;
using MilkCum.Core.Utils;
using UnityEngine;
using Verse;

using static MilkCum.Core.Constants.Constants;

namespace MilkCum.UI;

/// <summary>种族覆盖子 Tab：白名单、黑名单。流速倍率已移至设置「RJW」区块与乳房容量一起。</summary>
public class Widget_RaceOverrides
{
	public void Draw(Rect inRect)
	{
		var list = new Listing_Standard { maxOneColumn = true, ColumnWidth = inRect.width - 20f };
		list.Begin(inRect);
		list.Label("EM.RaceOverridesSection".Translate());
		list.Gap(4f);
		string raceAlways = string.Join(", ", MilkCumSettings.raceCanAlwaysLactate ?? new List<string>());
		Rect rAlways = list.GetRect(UNIT_SIZE);
		Widgets.Label(rAlways.LeftHalf(), "EM.RaceCanAlwaysLactate".Translate());
		raceAlways = Widgets.TextField(rAlways.RightHalf(), raceAlways, 128);
		MilkCumSettings.raceCanAlwaysLactate = CommaSeparatedDefNames.Parse(raceAlways);
		list.Gap(6f);
		string raceNever = string.Join(", ", MilkCumSettings.raceCannotLactate ?? new List<string>());
		Rect rNever = list.GetRect(UNIT_SIZE);
		Widgets.Label(rNever.LeftHalf(), "EM.RaceCannotLactate".Translate());
		raceNever = Widgets.TextField(rNever.RightHalf(), raceNever, 128);
		MilkCumSettings.raceCannotLactate = CommaSeparatedDefNames.Parse(raceNever);
		list.Gap(8f);
		MilkCumSettings.raceDrugDeltaSMultiplierDefNames ??= new List<string>();
		MilkCumSettings.raceDrugDeltaSMultiplierValues ??= new List<float>();
		string raceDrugPairs = CommaSeparatedDefNames.FormatRaceDrugDeltaSText(
			MilkCumSettings.raceDrugDeltaSMultiplierDefNames,
			MilkCumSettings.raceDrugDeltaSMultiplierValues);
		Rect rDrug = list.GetRect(UNIT_SIZE);
		Widgets.Label(rDrug.LeftHalf(), "EM.RaceDrugDeltaSMultipliers".Translate());
		raceDrugPairs = Widgets.TextField(rDrug.RightHalf(), raceDrugPairs, 256);
		CommaSeparatedDefNames.ParseRaceDrugDeltaSText(
			raceDrugPairs,
			MilkCumSettings.raceDrugDeltaSMultiplierDefNames,
			MilkCumSettings.raceDrugDeltaSMultiplierValues);
		{ string tip = "EM.RaceDrugDeltaSMultipliersDesc".Translate(); TooltipHandler.TipRegion(rDrug, string.IsNullOrEmpty(tip) ? "EM.RaceDrugDeltaSMultipliersDesc" : tip); }
		list.End();
	}
}
