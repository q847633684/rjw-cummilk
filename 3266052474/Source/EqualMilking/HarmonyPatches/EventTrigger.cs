using HarmonyLib;
using Verse;
using EqualMilking.Helpers;
using RimWorld;
namespace EqualMilking.HarmonyPatches;

[HarmonyPatch(typeof(Game))]
public static class Game_Patch
{
    // Add Custom Comps to Defs at game start
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Game.InitNewGame))]
    public static void InitNewGame_Prefix()
    {
        EventHelper.TriggerPostNewGame();

    }
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Game.LoadGame))]
    public static void LoadGame_Prefix()
    {
        EventHelper.TriggerPostLoadGame();
    }
}
[HarmonyPatch(typeof(DefGenerator))]
public static class DefGenerator_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public static void GenerateImpliedDefs_PreResolve_Prefix()
    {
        EventHelper.TriggerPostLoadLong();
    }
}