using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;
using static MilkCum.Core.Constants.Constants;

namespace MilkCum.UI;
public class Widget_MilkTagsTable
{
    /// <summary>濂舵爣绛句腑濮嬬粓淇濈暀鐨勭墿鍝佺绫伙紙浜哄ザ銆佸姩鐗╁ザ銆佺簿娑诧級锛屼笉闅?namesToProducts 绉婚櫎锛涗笖涓庝骇涓婚檺鍒躲€岃皝鍙互鍚冦€嶈仈鍔ㄣ€</summary>
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
                // 浜哄ザ銆佺簿娑查粯璁ゅ紑鍚€屾樉绀哄姩鐗╁悕銆嶏紝浜т富闄愬埗銆岃皝鍙互鍚冦€嶆墠鐢熸晥
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
