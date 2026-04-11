using HarmonyLib;
using RimWorld;
namespace MilkCum.Fluids.Cum.SemenPool.Patches;

[HarmonyPatch(typeof(ITab_Pawn_Health), "FillTab")]
public static class Patch_ITab_Pawn_Health_DrawVirtualSemenPool
{
    public static void Postfix(ITab_Pawn_Health __instance)
    {
        // 虚拟精液槽改由 HediffComp_SexPart.CompTipStringExtra（阴茎/雄性产卵器行悬停）展示，避免与健康表底部重复占用空间。
        _ = __instance;
    }
}
