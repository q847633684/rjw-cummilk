using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using MilkCum.Core;
using MilkCum.Fluids.Shared.Comps;
using MilkCum.UI.Cum;

namespace MilkCum.Fluids.Cum.Comps;

/// <summary>
/// 绮炬恫妗跺叧鑱斿簥锛氬彲閾炬帴鍒颁竴寮犲簥銆?
/// 閾炬帴鍚庝粎搴婁富鍙娇鐢紝浜т富绮炬恫 = 搴婁富锛堝嵆鏀堕泦鐨勭簿娑蹭竴寰嬭涓哄簥涓讳骇鐢燂級銆?
/// 鏈摼鎺ユ椂澶氫汉鍙娇鐢紝澶氫汉鐢ㄨ繃鍒欎负娣峰悎绮炬恫锛堟棤浜т富锛夈€?
/// 鑻ョ綈鍐呭凡鏈夌簿娑蹭笖浜т富涓嶆槸璇ュ簥鐨勫簥涓伙紝鍒欐棤娉曢摼鎺ュ埌璇ュ簥锛圕anLinkToBed 杩斿洖 false锛屽脊绐楁彁绀猴級銆?
/// </summary>
public class CompCumBucketLink : ThingComp
{
    public const string CumThingDefName = "Cumpilation_Cum";

    private Building_Bed _linkedBed;
    private List<Pawn> _usedByPawns = new();

    public Building_Bed LinkedBed
    {
        get => _linkedBed;
        set => _linkedBed = value;
    }

    public List<Pawn> UsedByPawns => _usedByPawns;

    /// <summary>搴婁富锛堥€氳繃 CompAssignableToPawn 鑾峰彇锛屽吋瀹瑰悇鐗堟湰锛</summary>
    public Pawn Owner => _linkedBed?.GetBedOwner();

    /// <summary>鍏变韩鍗у锛氭埧闂村唴澶氬紶搴婏紝鎴栭摼鎺ョ殑搴婃棤涓</summary>
    public bool IsSharedRoom
    {
        get
        {
            if (_linkedBed == null) return true;
            if (_linkedBed.GetBedOwner() == null) return true;
            var room = _linkedBed.GetRoom();
            if (room == null) return false;
            int bedCount = room.ContainedAndAdjacentThings.OfType<Building_Bed>().Count();
            return bedCount > 1;
        }
    }

    /// <summary>妗舵牸涓婃湁绮炬恫鐗╁搧鍗宠涓烘湁鍗犵敤</summary>
    public bool HasContent
    {
        get
        {
            if (parent?.Map == null) return false;
            var things = parent.Map.thingGrid.ThingsListAt(parent.Position);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def?.defName == CumThingDefName)
                    return true;
            }
            return false;
        }
    }

    /// <summary>妗跺唴鏈夌簿娑叉椂锛屼粎褰撴墍鏈夌簿娑插潎灞炰簬璇ュ簥涓伙紙鎴栨棤浜т富锛夋椂鎵嶅厑璁稿叧鑱旓紱鍚﹀垯杩斿洖 false 骞跺簲鐢辫皟鐢ㄦ柟鎻愮ず鏃犳硶閾炬帴</summary>
    public bool CanLinkToBed(Building_Bed bed)
    {
        if (bed == null) return true;
        if (!HasContent) return true;
        var bedOwner = bed.GetBedOwner();
        var things = parent.Map.thingGrid.ThingsListAt(parent.Position);
        for (int i = 0; i < things.Count; i++)
        {
            if (things[i].def?.defName != CumThingDefName) continue;
            var comp = things[i].TryGetComp<CompShowProducer>();
            if (comp == null) continue;
            if (comp.producer != null && comp.producer != bedOwner)
                return false;
        }
        return true;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref _linkedBed, "linkedBed");
        Scribe_Collections.Look(ref _usedByPawns, "usedByPawns", LookMode.Reference);
        if (_usedByPawns == null) _usedByPawns = new List<Pawn>();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (parent.IsHashIntervalTick(60) && !HasContent && _usedByPawns.Count > 0)
            _usedByPawns.Clear();
    }

    /// <summary>璇ュ皬浜烘槸鍚﹁鍏佽浣跨敤姝ゆ《锛堝仛 DeflateBucket 绛夛級</summary>
    public bool CanPawnUse(Pawn pawn)
    {
        if (pawn == null) return false;
        if (IsSharedRoom) return true;
        if (Owner == null) return true;
        if (!HasContent) return pawn == Owner;
        if (pawn == Owner && _usedByPawns.Count == 1 && _usedByPawns[0] == Owner) return true;
        return false;
    }

    /// <summary>鏄惁搴旀彁绀衡€滀笉灞炰簬浠栫殑绮炬恫妗垛€濓紙鏈夊崰鐢ㄤ笖涓嶆槸浠栫殑妗讹級</summary>
    public bool ShouldWarnNotYourBucket(Pawn pawn)
    {
        if (pawn == null || !HasContent) return false;
        if (IsSharedRoom) return false;
        if (Owner == null) return false;
        if (pawn == Owner && _usedByPawns.Count == 1 && _usedByPawns[0] == Owner) return false;
        return true;
    }

    /// <summary>璁板綍璇ュ皬浜哄悜姝ゆ《鎺掓斁浜嗕竴娆★紱鑻ュ浜虹敤杩囧垯鍚庣画浜у嚭涓烘贩鍚堢簿娑</summary>
    public void NotifyPawnUsed(Pawn pawn)
    {
        if (pawn == null) return;
        if (!_usedByPawns.Contains(pawn))
            _usedByPawns.Add(pawn);
    }

    /// <summary>鏄惁宸叉湁澶氫汉浣跨敤杩囷紙浜у嚭搴斾负娣峰悎绮炬恫锛</summary>
    public bool IsMixed => _usedByPawns.Count > 1;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var g in base.CompGetGizmosExtra())
            yield return g;

        if (parent.Faction != Faction.OfPlayer) yield break;

        if (_linkedBed != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "EM_CumBucket_UnlinkBed".Translate(),
                defaultDesc = "EM_CumBucket_UnlinkBed_Desc".Translate(),
                icon = TexCommand.ClearPrioritizedWork,
                action = () => _linkedBed = null
            };
            yield return new Command_Action
            {
                defaultLabel = "EM_CumBucket_LinkBed".Translate(),
                defaultDesc = "EM_CumBucket_LinkBed_Desc".Translate(),
                icon = TexCommand.Attack,
                action = () => Find.WindowStack.Add(new Dialog_SelectBedForBucket(parent.Map, this))
            };
        }
        else
        {
            yield return new Command_Action
            {
                defaultLabel = "EM_CumBucket_LinkBed".Translate(),
                defaultDesc = "EM_CumBucket_LinkBed_Desc".Translate(),
                icon = TexCommand.Attack,
                action = () => Find.WindowStack.Add(new Dialog_SelectBedForBucket(parent.Map, this))
            };
        }
    }

    public override string CompInspectStringExtra()
    {
        if (_linkedBed == null)
            return "EM_CumBucket_NotLinked".Translate();
        if (IsSharedRoom)
            return "EM_CumBucket_SharedRoom".Translate();
        if (Owner != null)
            return "EM_CumBucket_Owner".Translate(Owner.LabelShort);
        return "EM_CumBucket_UnassignedBed".Translate();
    }
}
