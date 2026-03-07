using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using static MilkCum.Core.Constants.Constants;

namespace MilkCum.UI;
public class Widget_MilkTagsTable
{
    /// <summary>奶标签中始终保留的物品种类（人奶、动物奶、精液），不随 namesToProducts 移除；且与产主限制「谁可以吃」联动。</summary>
    private static readonly HashSet<string> FixedProductTagKeys = new() { "EM_HumanMilk", "Milk", "Cumpilation_Cum" };

    private readonly Dictionary<string, RaceMilkType> namesToProducts;
    private readonly Dictionary<string, MilkTag> productsToTags;
    private Vector2 scrollPosition = Vector2.zero;
    public Widget_MilkTagsTable(Dictionary<string, RaceMilkType> namesToProducts, Dictionary<string, MilkTag> productsToTags)
    {
        this.namesToProducts = namesToProducts;
        this.productsToTags = productsToTags;
    }
    public void Draw(Rect inRect)
    {
        List<MilkTag> tags = namesToProducts.Values.Where(product => product != null && product.milkTypeDefName != null)
                                                .Select(product => new MilkTag(product.milkTypeDefName)).Distinct().ToList();
        foreach (MilkTag tag in tags)
        {
            if (!productsToTags.ContainsKey(tag.defName))
            {
                // 人奶、精液默认开启「显示动物名」，产主限制「谁可以吃」才生效
                bool defaultTagPawn = tag.defName == "EM_HumanMilk" || tag.defName == "Cumpilation_Cum";
                productsToTags.Add(tag.defName, defaultTagPawn ? new MilkTag(tag.defName, true, false) : tag);
            }
        }
        HashSet<string> removedTags = new(productsToTags.Keys);
        removedTags.ExceptWith(tags.Select(tag => tag.defName));
        removedTags.ExceptWith(FixedProductTagKeys);
        foreach (string key in removedTags)
        {
            productsToTags.Remove(key);
        }
        WidgetRow widgetRow = new(inRect.x, inRect.y, UIDirection.RightThenDown, inRect.width);
        widgetRow.Label(Lang.Item, UNIT_SIZE * 10, null);
        widgetRow.Label(Lang.Race, UNIT_SIZE * 3, null);
        widgetRow.Label(Lang.ShowAnimalNames, UNIT_SIZE * 3, null);
        Rect headerRect = new(inRect.x, inRect.y, inRect.width, UNIT_SIZE);
        TooltipHandler.TipRegion(headerRect, "EM.MilkTagsTableHeaderDesc".Translate());
        inRect.y += UNIT_SIZE;
        Widgets.BeginScrollView(inRect, ref scrollPosition, new Rect(0, 0, inRect.width, productsToTags.Count * UNIT_SIZE));
        float y_Offset = 0f;
        List<string> removedDefs = new();
        foreach (string key in productsToTags.Keys)
        {
            MilkTag value = productsToTags[key];
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(value.defName);
            if (thingDef == null) { removedDefs.Add(key); continue; }
            Widgets.ThingIcon(new Rect(0, y_Offset, UNIT_SIZE, UNIT_SIZE), thingDef, null, null, 1f, null, null);
            Widgets.Label(new Rect(UNIT_SIZE, y_Offset, UNIT_SIZE * 9, UNIT_SIZE), thingDef.DisplayText());
            Widgets.Checkbox(new Vector2(UNIT_SIZE * 10, y_Offset), ref value.TagRace, UNIT_SIZE);
            Widgets.Checkbox(new Vector2(UNIT_SIZE * 14, y_Offset), ref value.TagPawn, UNIT_SIZE);
            y_Offset += UNIT_SIZE;
        }
        foreach (string key in removedDefs)
        {
            productsToTags.Remove(key);
        }
        Widgets.EndScrollView();
    }
}
