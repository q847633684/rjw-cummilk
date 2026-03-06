using System.Collections.Generic;
using MilkCum.Core;
using RimWorld;
using Verse;

namespace MilkCum.Fluids.Lactation.Hediffs;

/// <summary>药物诱发泌乳时的能力增益。capMods 由设置与驱动(Drive)动态计算，对意�?操纵/移动均加 offset，与流速、剩余时间同源</summary>
public class Hediff_LactatingGain : HediffWithComps
{
    private HediffStage _stage;
    private List<PawnCapacityModifier> _capMods;

    public override HediffStage CurStage
    {
        get
        {
            if (_stage == null) _stage = new HediffStage();
            float gainOffset = 0f;
            if (EqualMilkingSettings.lactatingGainEnabled && EqualMilkingSettings.lactatingGainCapModPercent > 0f)
            {
                float L = pawn?.LactatingHediffComp()?.CurrentLactationAmount ?? 0f;
                float drive = EqualMilkingSettings.GetEffectiveDrive(L);
                float pct = UnityEngine.Mathf.Clamp(EqualMilkingSettings.lactatingGainCapModPercent, 0f, 0.20f);
                gainOffset = pct * drive;
            }
            if (_capMods == null)
            {
                _capMods = new List<PawnCapacityModifier>
                {
                    new() { capacity = PawnCapacityDefOf.Consciousness, offset = gainOffset },
                    new() { capacity = PawnCapacityDefOf.Manipulation, offset = gainOffset },
                    new() { capacity = PawnCapacityDefOf.Moving, offset = gainOffset }
                };
            }
            else
            {
                _capMods[0].offset = gainOffset;
                _capMods[1].offset = gainOffset;
                _capMods[2].offset = gainOffset;
            }
            _stage.capMods = _capMods;
            return _stage;
        }
    }
}
