using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MilkCum.Milk.Comps;

/// <summary>药物诱发泌乳时的能力增益，健康页显示。capMods 由设置与 Lactating severity 动态计算。</summary>
public class Hediff_LactatingGain : HediffWithComps
{
    private HediffStage _stage;
    private List<PawnCapacityModifier> _capMods;

    public override HediffStage CurStage
    {
        get
        {
            if (_stage == null) _stage = new HediffStage();
            if (!EqualMilkingSettings.lactatingGainEnabled || EqualMilkingSettings.lactatingGainCapModPercent <= 0f)
            {
                _stage.capMods = null;
                return _stage;
            }
            float severity = pawn?.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Lactating)?.Severity ?? 0f;
            float pct = UnityEngine.Mathf.Clamp(EqualMilkingSettings.lactatingGainCapModPercent, 0f, 0.20f);
            float offset = pct * severity;
            if (_capMods == null)
            {
                _capMods = new List<PawnCapacityModifier>
                {
                    new() { capacity = PawnCapacityDefOf.Consciousness, offset = offset },
                    new() { capacity = PawnCapacityDefOf.Manipulation, offset = offset },
                    new() { capacity = PawnCapacityDefOf.Moving, offset = offset }
                };
            }
            else
            {
                _capMods[0].offset = offset;
                _capMods[1].offset = offset;
                _capMods[2].offset = offset;
            }
            _stage.capMods = _capMods;
            return _stage;
        }
    }
}
