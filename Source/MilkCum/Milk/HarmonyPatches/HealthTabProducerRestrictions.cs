using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using MilkCum.UI;

namespace MilkCum.Milk.HarmonyPatches;

/// <summary>
/// 在健康面板顶部增加「谁可用我的奶/奶制品/精液制品」指定按钮，点击打开 Window_ProducerRestrictions。
/// </summary>
[HarmonyPatch(typeof(ITab_Pawn_Health), "FillTab")]
public static class ITab_Pawn_Health_ProducerRestrictionsPatch
{
    private const float ButtonHeight = 26f;
    private const float Margin = 17f;

    [HarmonyPostfix]
    public static void Postfix(ITab __instance)
    {
        Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
        if (pawn == null || !PawnColumnWorker_ProducerRestrictions.ShouldShowFor(pawn))
            return;

        // 在健康面板内容区顶部画一行：标签 + 指定按钮（与现有窗口逻辑一致，不依赖具体 CardSize）
        float width = 350f;
        float y = Margin;
        Rect row = new Rect(Margin, y, width, ButtonHeight);
        Rect labelRect = new Rect(row.x, row.y, row.width - 100f, row.height);
        Rect buttonRect = new Rect(row.xMax - 96f, row.y, 96f, row.height - 2f);

        string label = "EM.HealthTabRestrictionsButton".Translate();
        Widgets.Label(labelRect, label);
        if (Widgets.ButtonText(buttonRect, "EM.Specify".Translate()))
        {
            Find.WindowStack.Add(new Window_ProducerRestrictions(pawn));
        }
        TooltipHandler.TipRegion(row, "EM.ProducerRestrictionsColumnTip".Translate());
    }
}
