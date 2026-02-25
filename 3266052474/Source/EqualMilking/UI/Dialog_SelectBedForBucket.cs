using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace EqualMilking.UI;

public class Dialog_SelectBedForBucket : Window
{
    private readonly Map _map;
    private readonly CompCumBucketLink _comp;
    private Vector2 _scrollPosition;
    private List<Building_Bed> _bedsInRoom;
    private List<Building_Bed> _bedsElsewhere;

    public override Vector2 InitialSize => new(320f, 420f);

    public Dialog_SelectBedForBucket(Map map, CompCumBucketLink comp)
    {
        _map = map;
        _comp = comp;
        doCloseX = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;
        draggable = true;
        RefreshBeds();
    }

    private void RefreshBeds()
    {
        _bedsInRoom = new List<Building_Bed>();
        _bedsElsewhere = new List<Building_Bed>();
        if (_map == null) return;
        var room = _comp?.parent?.GetRoom();
        foreach (var bed in _map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>())
        {
            if (room != null && bed.GetRoom() == room)
                _bedsInRoom.Add(bed);
            else
                _bedsElsewhere.Add(bed);
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        float y = inRect.y;
        Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "EM_CumBucket_SelectBed".Translate());
        y += 28f;

        var outRect = new Rect(inRect.x, y, inRect.width, inRect.height - y);
        var viewRect = new Rect(0f, 0f, outRect.width - 24f, (_bedsInRoom.Count + _bedsElsewhere.Count) * 32f + 64f);
        Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);

        float row = 0f;
        if (_bedsInRoom.Count > 0)
        {
            Widgets.Label(new Rect(0f, row, viewRect.width, 22f), "EM_CumBucket_BedsInRoom".Translate());
            row += 24f;
            foreach (var bed in _bedsInRoom)
            {
                DrawBedRow(viewRect.width, ref row, bed);
            }
            row += 8f;
        }
        if (_bedsElsewhere.Count > 0)
        {
            Widgets.Label(new Rect(0f, row, viewRect.width, 22f), "EM_CumBucket_BedsElsewhere".Translate());
            row += 24f;
            foreach (var bed in _bedsElsewhere)
            {
                DrawBedRow(viewRect.width, ref row, bed);
            }
        }
        Widgets.EndScrollView();
    }

    private void DrawBedRow(float width, ref float row, Building_Bed bed)
    {
        var rect = new Rect(0f, row, width, 30f);
        string label = bed.LabelCap;
        if (bed.AssigningPawn != null)
            label += " (" + bed.AssigningPawn.LabelShort + ")";
        if (Widgets.ButtonText(rect, label))
        {
            if (_comp != null)
                _comp.LinkedBed = bed;
            Close();
        }
        row += 32f;
    }
}
