using RimWorld;
using Verse;

namespace EqualMilking
{
    /// <summary>Provides safe tooltip for 催乳素耐受; avoids "Error getting tip text" from vanilla DrugEffectFactor comp.</summary>
    public class Hediff_ProlactinTolerance : HediffWithComps
    {
        public override string TipString
        {
            get
            {
                try
                {
                    string text = def.LabelCap;
                    string desc = def.description;
                    if (desc.NullOrEmpty()) desc = "EM_Prolactin_Tolerance.description".Translate();
                    text += "\n\n" + desc;
                    float days = SeverityChangePerDay;
                    if (days < 0f)
                        text += "\n\n" + "DaysToRecover".Translate((Severity / Mathf.Abs(days)).ToString("0.0")).Resolve();
                    return text;
                }
                catch
                {
                    return def.LabelCap + "\n\n" + (def.description ?? "EM_Prolactin_Tolerance.description".Translate());
                }
            }
        }

        private float SeverityChangePerDay
        {
            get
            {
                var comp = this.TryGetComp<HediffComp_SeverityPerDay>();
                return comp != null ? comp.SeverityChangePerDay() : -0.015f;
            }
        }
    }
}
