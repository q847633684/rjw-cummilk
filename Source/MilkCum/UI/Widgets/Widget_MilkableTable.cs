using System;
using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using RimWorld;
using UnityEngine;
using Verse;
using static MilkCum.Core.Constants.Constants;

namespace MilkCum.UI;
public class Widget_MilkableTable
{
    private const string ProductButtonPadding = "        ";
    private readonly Dictionary<string, RaceMilkType> namesToProducts;
    private Vector2 scrollPosition = Vector2.zero;
    public Widget_MilkableTable(Dictionary<string, RaceMilkType> namesToProducts)
    {
        this.namesToProducts = namesToProducts;
    }
    public void Draw(Rect inRect)
    {
        WidgetRow widgetRow = new(inRect.x, inRect.y, UIDirection.RightThenDown, inRect.width);
        if (widgetRow.ButtonText(Lang.Join(Lang.Allow, Lang.All), null, true, true, true, null)) { AllowMilkAll(); }
        if (widgetRow.ButtonText(Lang.ResetAll, null, true, true, true, null)) { RestoreVanilla(); }
        if (widgetRow.ButtonText(Lang.Join(Lang.Forbid, Lang.All), null, true, true, true, null)) { ForbidMilkAll(); }
        Rect tableRect = new(inRect.x, inRect.y + UNIT_SIZE, inRect.width, inRect.height - UNIT_SIZE);
        SetupTable(tableRect, ref this.scrollPosition);
    }
	private void SetupTable(Rect rect, ref Vector2 scrollPosition)
	{
		IEnumerable<ThingDef> pawnDefs = MilkCumSettings.pawnDefs;
		if (pawnDefs == null)
			return;
		// 将可泌乳条目物化，避免同一个 IEnumerable 在 UI 绘制时被 Count/枚举重复执行。
		List<ThingDef> pawnDefList = pawnDefs as List<ThingDef> ?? pawnDefs.ToList();
		int pawnCount = pawnDefList.Count;
		IEnumerable<ThingDef> itemDefs = (from def in DefDatabase<ThingDef>.AllDefs where def.category == ThingCategory.Item && !def.IsApparel && !def.IsBuildingArtificial && !def.IsCorpse && !def.isUnfinishedThing && !def.IsWeapon select def).Distinct().OrderBy(def => def.defName);
        Text.Font = GameFont.Small;

        WidgetRow widgetRow = new(rect.x, rect.y, UIDirection.RightThenDown, rect.width);
        Text.Font = GameFont.Tiny;
        string lactatingLabel = HediffDefOf.Lactating?.label ?? "EM.Lactating".Translate();
        widgetRow.Label(Lang.Pawn, UNIT_SIZE * 8, null);
        widgetRow.Label(lactatingLabel, UNIT_SIZE * 3, null);
        widgetRow.Label(Lang.MilkType, UNIT_SIZE * 6, Lang.MilkTypeDesc);
        Text.Font = GameFont.Small;
		Rect tableRect = new(rect.x, rect.y + UNIT_SIZE, rect.width, rect.height - UNIT_SIZE);
		float contentHeight = pawnCount * UNIT_SIZE;
		float outHeight = Mathf.Min(tableRect.height, contentHeight);
		outHeight = Mathf.Max(UNIT_SIZE, outHeight);
		Rect outRect = new(tableRect.x, tableRect.y, tableRect.width, outHeight);
		float innerW = tableRect.width - 16f;
		Rect scrollInner = new(0f, 0f, innerW, contentHeight);
		Widgets.BeginScrollView(outRect, ref scrollPosition, scrollInner);
		try
		{
			using (IEnumerator<ThingDef> enumerator = pawnDefList.GetEnumerator())
			{
				float yRow = 0f;
				while (enumerator.MoveNext())
				{
					ThingDef pawnDef = enumerator.Current;
					RaceMilkType product = namesToProducts.GetWithFallback(pawnDef.defName, new RaceMilkType());
					Widgets.ThingIcon(new Rect(0f, yRow, UNIT_SIZE, UNIT_SIZE), DefDatabase<ThingDef>.GetNamed(pawnDef.defName, true), null, null, 1f, null, null);
					Widgets.Label(new Rect(UNIT_SIZE, yRow, UNIT_SIZE * 9, UNIT_SIZE), pawnDef.DisplayText());
					Widgets.Checkbox(new Vector2(UNIT_SIZE * 10, yRow), ref product.isMilkable, UNIT_SIZE);
					Rect btnLocal = new(UNIT_SIZE * 11, yRow, UNIT_SIZE * 7, UNIT_SIZE);
					SetupSelectProductButton(btnLocal, pawnDef, itemDefs, outRect, scrollPosition);
					yRow += UNIT_SIZE;
				}
			}
		}
		finally
		{
			Widgets.EndScrollView();
		}
    }
    private void SetupSelectProductButton(Rect buttonRectLocal, ThingDef currentOptionDef, IEnumerable<ThingDef> optionDefs, Rect scrollViewport, Vector2 scrollPosition)
    {
        ThingDef milkProductDef;
        if (namesToProducts.TryGetValue(currentOptionDef.defName, out RaceMilkType product))
        {
            milkProductDef = string.IsNullOrEmpty(product.milkTypeDefName) ? null : DefDatabase<ThingDef>.GetNamedSilentFail(product.milkTypeDefName);
        }
        else
        {
            RaceMilkType newProduct = MilkCumSettings.GetDefaultMilkProduct(currentOptionDef);
            namesToProducts.Add(currentOptionDef.defName, newProduct);
            milkProductDef = string.IsNullOrEmpty(newProduct.milkTypeDefName) ? null : DefDatabase<ThingDef>.GetNamedSilentFail(newProduct.milkTypeDefName);
        }
        if (milkProductDef == null)
            milkProductDef = DefDatabase<ThingDef>.GetNamedSilentFail("Milk") ?? MilkCumDefOf.EM_HumanMilk; // 无奶类型时回退，避免 NRE
        if (Widgets.ButtonText(buttonRectLocal, ProductButtonPadding + milkProductDef.DisplayText(), true, true, true, TextAnchor.MiddleLeft))
        {
            float anchorX = scrollViewport.x + buttonRectLocal.x;
            float anchorY = scrollViewport.y + buttonRectLocal.y - scrollPosition.y;
            Window_Search searchWindow = new(namesToProducts[currentOptionDef.defName].SetMilkType) { windowRect = new Rect(anchorX, anchorY, buttonRectLocal.width, 500f) };
            Find.WindowStack.Add(searchWindow);
        }
        Widgets.DefIcon(new Rect(buttonRectLocal.x, buttonRectLocal.y, buttonRectLocal.height, buttonRectLocal.height), milkProductDef);
    }
    private void RestoreVanilla()
    {
        foreach (string defName in namesToProducts.Keys.ToList())
        {
            if (DefDatabase<ThingDef>.GetNamedSilentFail(defName) is ThingDef def)
            {
                namesToProducts[defName] = MilkCumSettings.GetDefaultMilkProduct(def);
            }
            else
            {
                namesToProducts.Remove(defName);
            }
        }
    }
    private void AllowMilkAll()
    {
        foreach (string defName in namesToProducts.Keys.ToList())
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                namesToProducts.Remove(defName);
                continue;
            }
            if (!namesToProducts[defName].isMilkable)
            {
                namesToProducts[defName] = MilkCumSettings.GetDefaultMilkProduct(def);
                namesToProducts[defName].isMilkable = true;
            }

        }
    }
    private void ForbidMilkAll()
    {
        List<string> defNames = new(namesToProducts.Keys);
        foreach (string defName in defNames)
        {
            namesToProducts[defName].isMilkable = false;
        }
    }

}
