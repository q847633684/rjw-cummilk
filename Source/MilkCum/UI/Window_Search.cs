using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using MilkCum.Milk.Helpers;
using static MilkCum.Milk.Helpers.Constants;

namespace MilkCum.UI;
public class Window_Search : Window
{
	private Vector2 scrollPosition = new(0f, 0f);
	private readonly QuickSearchWidget quickSearchWidget = new();
	private static readonly List<ThingDef> AllDefs = GetItemDefs();
	private List<ThingDef> searchResults = new();
	private ThingDef selectedDef = null;
	private readonly Action<ThingDef> onClose;
	public Window_Search(Action<ThingDef> onClose)
	{
		this.doCloseX = true;
		this.closeOnClickedOutside = true;
		this.absorbInputAroundWindow = true;
		this.closeOnAccept = true;
		this.searchResults = AllDefs;
		this.onClose = onClose;

	}
	public override void PreClose()
	{
		base.PreClose();
		this.onClose.Invoke(this.selectedDef);
	}
	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Small;
		Rect quickSearchRect = new(inRect.x, inRect.y, inRect.width - 2 * Text.LineHeight, UNIT_SIZE);
		quickSearchWidget.OnGUI(quickSearchRect, UpdateFilteredDefs);
		Listing_Standard listing_Standard = new(GameFont.Small);
		Text.Anchor = TextAnchor.MiddleLeft;

		Widgets.BeginScrollView(new Rect(inRect.x, inRect.y + UNIT_SIZE * 2, inRect.width, inRect.height - UNIT_SIZE * 2), ref scrollPosition, new Rect(inRect.x, inRect.y + UNIT_SIZE, inRect.width - Text.LineHeight, searchResults.Count * Text.LineHeight));
		Rect listingRect = new(inRect.x, inRect.y + UNIT_SIZE, inRect.width - Text.LineHeight, 999999);
		listing_Standard.Begin(listingRect);
		listing_Standard.verticalSpacing = 2f;
		foreach (ThingDef def in searchResults)
		{
			Rect rect = listing_Standard.GetRect(Text.LineHeight);
			Widgets.DrawOptionBackground(rect, false);
			Widgets.ThingIcon(new Rect(rect.x, rect.y, Text.LineHeight, Text.LineHeight), def);
			Widgets.Label(new Rect(rect.x + Text.LineHeight, rect.y, rect.width - Text.LineHeight, rect.height), def.DisplayText());
			if (Widgets.ButtonInvisible(rect))
			{
				selectedDef = def;
				Close();
			}
		}
		listing_Standard.End();
		Widgets.EndScrollView();
		Text.Anchor = TextAnchor.UpperLeft;
	}
	private void UpdateFilteredDefs()
	{
		searchResults = AllDefs.Where(def => quickSearchWidget.filter.Matches(def.defName) || quickSearchWidget.filter.Matches(def.label)).ToList();
	}

	public static List<ThingDef> GetItemDefs()
	{
		return (from def in DefDatabase<ThingDef>.AllDefs
				where
		def.category == ThingCategory.Item
		&& def.stackLimit > 1
		&& !def.HasComp<CompQuality>()
		&& !def.IsArt
		&& !def.IsApparel
		&& !def.IsWeapon
		&& !def.IsBuildingArtificial
		&& !def.IsCorpse
		&& !def.isUnfinishedThing
		&& !def.Minifiable
		&& !def.IsPlant
				select def).Distinct()
				.OrderBy(def => !def.IsAnimalProduct)
				.ThenBy(def => def.ingestible == null)
				.ThenBy(def => def.defName).ToList();
	}
}