using System;
using Verse;

namespace MilkCum.Fluids.Shared.Helpers;

/// <summary>
/// <see cref="BodyPartRecord.customLabel"/> 与所属 <see cref="BodyPartDef"/> 的 defName 上的左右侧向启发式；泌乳与虚拟睾丸精池共用同一套字符串规则。
/// </summary>
public static class BodyPartLaterality
{
    public static bool PartNameLooksLeft(BodyPartRecord part)
    {
        if (part == null) return false;
        if (!string.IsNullOrEmpty(part.customLabel))
        {
            if (part.customLabel.Contains("左")) return true;
            if (part.customLabel.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        if (part.def?.defName == null) return false;
        return part.def.defName.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool PartNameLooksRight(BodyPartRecord part)
    {
        if (part == null) return false;
        if (!string.IsNullOrEmpty(part.customLabel))
        {
            if (part.customLabel.Contains("右")) return true;
            if (part.customLabel.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        if (part.def?.defName == null) return false;
        return part.def.defName.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>同时匹配左右时 false，用于汇入「独占左 / 独占右 / 模糊」三态。</summary>
    public static bool IsExclusiveLeft(BodyPartRecord part) => PartNameLooksLeft(part) && !PartNameLooksRight(part);

    /// <summary>同时匹配左右时 false。</summary>
    public static bool IsExclusiveRight(BodyPartRecord part) => PartNameLooksRight(part) && !PartNameLooksLeft(part);
}
