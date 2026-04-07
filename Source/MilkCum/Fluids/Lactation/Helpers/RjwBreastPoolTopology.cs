using System.Collections.Generic;
using MilkCum.Core.Settings;
using MilkCum.Fluids.Shared.Data;
using MilkCum.Integration;
using Verse;

namespace MilkCum.Fluids.Lactation.Helpers;

/// <summary>
/// RJW 乳房乳池拓扑：按 <see cref="RjwBreastPoolTopologyMode"/> 分派快照 / 侧行 / 池条目，避免在多处复制 switch。
/// </summary>
internal interface IRjwBreastPoolTopologyStrategy
{
    List<RjwBreastPoolSnapshot> GetSnapshots(Pawn pawn);
    List<RjwBreastPoolSideRow> BuildSideRows(Pawn pawn);
    List<FluidPoolEntry> BuildPoolEntries(Pawn pawn);
}

internal static class RjwBreastPoolTopology
{
    public static IRjwBreastPoolTopologyStrategy Resolve(RjwBreastPoolTopologyMode mode) => mode switch
    {
        RjwBreastPoolTopologyMode.RjwChestUnified => ChestUnifiedStrategy.Instance,
        RjwBreastPoolTopologyMode.PerAnatomicalLeaf => PerAnatomicalLeafStrategy.Instance,
        _ => VirtualLeftRightStrategy.Instance,
    };

    /// <summary>与当前拓扑设置一致的 <see cref="FluidPoolEntry"/> 列表（含组织适应摊入）。</summary>
    public static List<FluidPoolEntry> BuildPoolEntriesForCurrentTopology(Pawn pawn)
    {
        if (pawn == null || !ModIntegrationGates.RjwModActive) return new List<FluidPoolEntry>();
        return Resolve(MilkCumSettings.rjwBreastPoolTopologyMode).BuildPoolEntries(pawn);
    }

    public static List<RjwBreastPoolSnapshot> GetSnapshotsForCurrentTopology(Pawn pawn)
    {
        if (pawn == null || !ModIntegrationGates.RjwModActive) return new List<RjwBreastPoolSnapshot>();
        try
        {
            return Resolve(MilkCumSettings.rjwBreastPoolTopologyMode).GetSnapshots(pawn);
        }
        catch (System.Exception ex)
        {
            RjwBreastPoolEconomy.LogDev(nameof(GetSnapshotsForCurrentTopology), ex);
            return new List<RjwBreastPoolSnapshot>();
        }
    }

    public static List<RjwBreastPoolSideRow> BuildSideRowsUncachedForCurrentTopology(Pawn pawn)
    {
        if (pawn == null || !ModIntegrationGates.RjwModActive) return new List<RjwBreastPoolSideRow>();
        try
        {
            return Resolve(MilkCumSettings.rjwBreastPoolTopologyMode).BuildSideRows(pawn);
        }
        catch (System.Exception ex)
        {
            RjwBreastPoolEconomy.LogDev(nameof(BuildSideRowsUncachedForCurrentTopology), ex);
            return new List<RjwBreastPoolSideRow>();
        }
    }

    private sealed class ChestUnifiedStrategy : IRjwBreastPoolTopologyStrategy
    {
        internal static readonly ChestUnifiedStrategy Instance = new();

        public List<RjwBreastPoolSnapshot> GetSnapshots(Pawn pawn) => RjwBreastPoolEconomy.SnapshotsChestUnified(pawn);
        public List<RjwBreastPoolSideRow> BuildSideRows(Pawn pawn) => RjwBreastPoolEconomy.BuildChestUnifiedSideRows(pawn);
        public List<FluidPoolEntry> BuildPoolEntries(Pawn pawn) => RjwBreastPoolEconomy.BuildChestUnifiedBreastPoolEntries(pawn);
    }

    private sealed class VirtualLeftRightStrategy : IRjwBreastPoolTopologyStrategy
    {
        internal static readonly VirtualLeftRightStrategy Instance = new();

        public List<RjwBreastPoolSnapshot> GetSnapshots(Pawn pawn) => RjwBreastPoolEconomy.SnapshotsVirtualLeftRight(pawn);
        public List<RjwBreastPoolSideRow> BuildSideRows(Pawn pawn) => RjwBreastPoolEconomy.BuildVirtualLeftRightSideRowsUncached(pawn);
        public List<FluidPoolEntry> BuildPoolEntries(Pawn pawn) => RjwBreastPoolEconomy.BuildVirtualLeftRightBreastPoolEntries(pawn);
    }

    private sealed class PerAnatomicalLeafStrategy : IRjwBreastPoolTopologyStrategy
    {
        internal static readonly PerAnatomicalLeafStrategy Instance = new();

        public List<RjwBreastPoolSnapshot> GetSnapshots(Pawn pawn) => RjwBreastPoolEconomy.SnapshotsPerAnatomicalLeaf(pawn);
        public List<RjwBreastPoolSideRow> BuildSideRows(Pawn pawn) => RjwBreastPoolEconomy.BuildPerAnatomicalLeafSideRowsUncached(pawn);
        public List<FluidPoolEntry> BuildPoolEntries(Pawn pawn) => RjwBreastPoolEconomy.BuildPerAnatomicalLeafBreastPoolEntries(pawn);
    }
}
