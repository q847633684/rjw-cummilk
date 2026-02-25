using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;
using System.Threading;
using UnityEngine;
using EqualMilking.Data;

namespace EqualMilking.Helpers;

public static class ListerAsync
{
    private static readonly int updateInterval = 300;
    private static readonly Dictionary<Map, AsyncCollection<HashSet<Pawn>, Pawn>> allColonyPawns = new();
    private static readonly Dictionary<object, int> updateTimes = new();

    private static bool NeedsUpdate(object obj)
    {
        if (!updateTimes.ContainsKey(obj) || Find.TickManager.TicksGame > updateTimes[obj])
        {
            updateTimes[obj] = Find.TickManager.TicksGame + updateInterval;
            return true;
        }
        return false;
    }

    public static HashSet<Pawn> AllColonyPawns(this Map map)
    {
        if (!allColonyPawns.ContainsKey(map))
        {
            allColonyPawns.Add(map, new AsyncCollection<HashSet<Pawn>, Pawn>());
        }

        if (NeedsUpdate(allColonyPawns[map]))
        {
            //TODO find why async not working
            UpdateAllColonyPawns(map);
        }

        return allColonyPawns[map].Active;
    }

    public static bool IsColonyPawn(this Pawn pawn)
    {
        return pawn.MapHeld?.AllColonyPawns().Contains(pawn) == true;
    }

    private static void UpdateAllColonyPawns(Map map)
    {
        using AsyncCollection<HashSet<Pawn>, Pawn> pawns = allColonyPawns[map];
        HashSet<Pawn> temp = pawns.Inactive;
        temp.Clear();
        temp.AddRange(PawnsFinder.HomeMaps_FreeColonistsSpawned);
        foreach (Pawn p in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                                        .Concat(map.mapPawns.SlavesAndPrisonersOfColonySpawned))
        {
            temp.Add(p);
        }
        foreach (Building building in Find.CurrentMap.listerBuildings.AllBuildingsColonistOfGroup(ThingRequestGroup.EntityHolder))
        {
            if (building.TryGetComp<CompEntityHolder>() is CompEntityHolder comp && comp.HeldPawn is Pawn entity)
            {
                temp.Add(entity);
            }
        }
    }
}
