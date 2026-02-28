using System;
using System.Collections.Generic;
using System.Linq;
using MilkCum.Core;
using MilkCum.Milk.Data;
using MilkCum.Milk.Helpers;
using RimWorld;
using UnityEngine;
using Verse;
using static MilkCum.Milk.Helpers.Constants;

namespace MilkCum.UI;
public class Widget_MilkableTable
{
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
		IEnumerable<ThingDef> pawnDefs = EqualMilkingSettings.pawnDefs;
		if (pawnDefs == null)
			return;
		IEnumerable<ThingDef> itemDefs = (from def in DefDatabase<ThingDef>.AllDefs where def.category == ThingCategory.Item && !def.IsApparel && !def.IsBuildingArtificial && !def.IsCorpse && !def.isUnfinishedThing && !def.IsWeapon select def).Distinct().OrderBy(def => def.defName);
        Text.Font = GameFont.Small;

        WidgetRow widgetRow = new(rect.x, rect.y, UIDirection.RightThenDown, rect.width);
        Text.Font = GameFont.Tiny;
        string lactatingLabel = HediffDefOf.Lactating?.label ?? "Lactating";
        widgetRow.Label(Lang.Pawn, UNIT_SIZE * 8, null);
        widgetRow.Label(lactatingLabel, UNIT_SIZE * 3, null);
        widgetRow.Label(Lang.MilkType, UNIT_SIZE * 6, Lang.MilkTypeDesc);
        widgetRow.Label(Lang.MilkAmount, UNIT_SIZE * 3, Lang.MilkAmountDesc);
        Text.Font = GameFont.Small;
		Rect tableRect = new(rect.x, rect.y + UNIT_SIZE, rect.width, rect.height - UNIT_SIZE);
		Rect scrollRect = new(tableRect.x, tableRect.y + UNIT_SIZE, tableRect.width - UNIT_SIZE, pawnDefs.Count() * UNIT_SIZE);
		Widgets.BeginScrollView(tableRect, ref scrollPosition, scrollRect, true);
		try
		{
		using (IEnumerator<ThingDef> enumerator = pawnDefs.GetEnumerator())
        {
            float y_Offset = tableRect.y;
            while (enumerator.MoveNext())
            {
                ThingDef pawnDef = enumerator.Current;
                RaceMilkType product = namesToProducts.GetWithFallback(pawnDef.defName, new RaceMilkType());
                y_Offset += UNIT_SIZE;
                Widgets.ThingIcon(new Rect(tableRect.x, y_Offset, UNIT_SIZE, UNIT_SIZE), DefDatabase<ThingDef>.GetNamed(pawnDef.defName, true), null, null, 1f, null, null);
                Widgets.Label(new Rect(tableRect.x + UNIT_SIZE, y_Offset, UNIT_SIZE * 9, UNIT_SIZE), pawnDef.DisplayText());
                //Text.Font = GameFont.Tiny;
                Widgets.Checkbox(new Vector2(tableRect.x + UNIT_SIZE * 10, y_Offset), ref product.isMilkable, UNIT_SIZE);
                if (!product.isMilkable) { continue; } //Skip the rest of the row if the pawn is not milkable
                SetupSelectProductButton(new Rect(tableRect.x + UNIT_SIZE * 11, y_Offset, UNIT_SIZE * 7, UNIT_SIZE), pawnDef, itemDefs);
                string text = product.milkAmount.ToString();
                Widgets.TextFieldNumeric(new Rect(tableRect.x + UNIT_SIZE * 19, y_Offset, UNIT_SIZE * 3, UNIT_SIZE), ref product.milkAmount, ref text, 1, 99999);
            }
        }
        }
		finally
		{
			Widgets.EndScrollView();
		}
    }
    private void SetupSelectProductButton(Rect buttonRect, ThingDef currentOptionDef, IEnumerable<ThingDef> optionDefs)
    {
        ThingDef milkProductDef;
        if (namesToProducts.TryGetValue(currentOptionDef.defName, out RaceMilkType product))
        {
            milkProductDef = string.IsNullOrEmpty(product.milkTypeDefName) ? null : DefDatabase<ThingDef>.GetNamedSilentFail(product.milkTypeDefName);
        }
        else
        {
            RaceMilkType newProduct = EqualMilkingSettings.GetDefaultMilkProduct(currentOptionDef);
            namesToProducts.Add(currentOptionDef.defName, newProduct);
            milkProductDef = string.IsNullOrEmpty(newProduct.milkTypeDefName) ? null : DefDatabase<ThingDef>.GetNamedSilentFail(newProduct.milkTypeDefName);
        }
        if (milkProductDef == null)
            milkProductDef = DefDatabase<ThingDef>.GetNamedSilentFail("Milk") ?? EMDefOf.EM_HumanMilk; // 无奶类型时回退，避免 NRE
        if (Widgets.ButtonText(buttonRect, "        " + milkProductDef.DisplayText(), true, true, true, TextAnchor.MiddleLeft))
        {
            Window_Search searchWindow = new(namesToProducts[currentOptionDef.defName].SetMilkType) { windowRect = new Rect(buttonRect.x, buttonRect.y, buttonRect.width, 500) };
            Find.WindowStack.Add(searchWindow);
        }
        Widgets.DefIcon(new Rect(buttonRect.x, buttonRect.y, buttonRect.height, buttonRect.height), milkProductDef);
    }
    private void RestoreVanilla()
    {
        foreach (string defName in namesToProducts.Keys.ToList())
        {
            if (DefDatabase<ThingDef>.GetNamedSilentFail(defName) is ThingDef def)
            {
                namesToProducts[defName] = EqualMilkingSettings.GetDefaultMilkProduct(def);
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
                namesToProducts[defName] = EqualMilkingSettings.GetDefaultMilkProduct(def);
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
