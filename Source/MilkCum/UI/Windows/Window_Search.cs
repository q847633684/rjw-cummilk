using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using static MilkCum.Core.Constants.Constants;

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
		optionalTitle = "EM.WindowSearchTitle".Translate();
		doCloseX = true;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = true;
		closeOnAccept = true;
		searchResults = AllDefs;
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
		// 占位提示（多语言）：搜索框无原生 placeholder 时用灰字提示
		GUI.color = Color.gray;
		Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, Text.LineHeight), "EM.SearchItemsPlaceholder".Translate());
		GUI.color = Color.white;
		Rect quickSearchRect = new(inRect.x, inRect.y + Text.LineHeight, inRect.width - 2 * Text.LineHeight, UNIT_SIZE);
		quickSearchWidget.OnGUI(quickSearchRect, UpdateFilteredDefs);
		Listing_Standard listing_Standard = new(GameFont.Small);
		Text.Anchor = TextAnchor.MiddleLeft;

		float listTop = inRect.y + Text.LineHeight + UNIT_SIZE;
		float listHeight = inRect.height - Text.LineHeight - UNIT_SIZE;
		Rect outRect = new(inRect.x, listTop, inRect.width, listHeight);
		Rect contentRect = new(inRect.x, listTop, inRect.width - Text.LineHeight, Mathf.Max(1f, searchResults.Count * Text.LineHeight));
		Widgets.BeginScrollView(outRect, ref scrollPosition, contentRect);
		Rect listingRect = new(inRect.x, listTop, inRect.width - Text.LineHeight, 999999);
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