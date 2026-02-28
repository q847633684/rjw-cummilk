using System.Collections.Generic;
using MilkCum.Core;
using MilkCum.Milk.Helpers;
using UnityEngine;
using Verse;

using static MilkCum.Milk.Helpers.Constants;

namespace MilkCum.UI;

/// <summary>种族覆盖子 Tab：白名单、黑名单、人形流速倍率。</summary>
public class Widget_RaceOverrides
{
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

	public void Draw(Rect inRect)
	{
		var list = new Listing_Standard { maxOneColumn = true, ColumnWidth = inRect.width - 20f };
		list.Begin(inRect);
		list.Label("EM.RaceOverridesSection".Translate());
		list.Gap(4f);
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
		list.End();
	}
}
