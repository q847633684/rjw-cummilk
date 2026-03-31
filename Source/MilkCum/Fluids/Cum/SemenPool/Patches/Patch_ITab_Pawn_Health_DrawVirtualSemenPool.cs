using HarmonyLib;
using MilkCum.Fluids.Cum.Common;
using RimWorld;
using UnityEngine;
using Verse;
using MilkCum.Core.Settings;

namespace MilkCum.Fluids.Cum.SemenPool.Patches;

[HarmonyPatch(typeof(ITab_Pawn_Health), "FillTab")]
public static class Patch_ITab_Pawn_Health_DrawVirtualSemenPool
{
    public static void Postfix(ITab_Pawn_Health __instance)
    {
        if (!MilkCumSettings.Cum_EnableVirtualSemenPool) return;
        Thing sel = AccessTools.Property(typeof(ITab), "SelThing")?.GetValue(__instance) as Thing;
        Pawn pawn = sel as Pawn;
        if (pawn == null && sel is Corpse corpse)
            pawn = corpse.InnerPawn;
        if (pawn == null || pawn.Dead) return;

        var rows = pawn.CompVirtualSemenPool().GetSemenPoolDisplayRows(pawn);
        if (rows.Count == 0) return;

        Vector2 size = Traverse.Create(__instance).Field("size").GetValue<Vector2>();
        float lineH = Text.LineHeight;
        float pad = 6f;
        float headerH = lineH + 2f;
        float totalH = headerH + rows.Count * lineH + pad * 2f;
        float width = Mathf.Max(120f, size.x - 34f);
        Rect area = new Rect(17f, Mathf.Max(0f, size.y - totalH - 8f), width, totalH);

        Widgets.DrawMenuSection(area);
        Rect inner = area.ContractedBy(4f);
        float y = inner.y;
        Widgets.Label(new Rect(inner.x, y, inner.width, headerH), "EM.HealthTabVirtualSemenPoolHeader".Translate());
        y += headerH;
        foreach ((FluidSiteKind site, float current, float capacity) in rows)
        {
            string side = site == FluidSiteKind.TesticleLeft
                ? "EM.VirtualSemenPoolLeft".Translate()
                : "EM.VirtualSemenPoolRight".Translate();
            string line = $"{side}: {current:F1} / {capacity:F1}";
            Widgets.Label(new Rect(inner.x, y, inner.width, lineH), line);
            y += lineH;
        }
    }
}
