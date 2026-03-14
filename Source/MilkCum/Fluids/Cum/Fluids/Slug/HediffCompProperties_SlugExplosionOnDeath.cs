using MilkCum.Fluids.Cum.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MilkCum.Fluids.Cum.Fluids.Slug
{
    public class HediffCompProperties_SlugExplosionOnDeath : HediffCompProperties
    {

        public float baseRadius = 4;
        public bool radiusMultipliedByBodySize = false;
        public bool radiusMultipliedBySeverity = true;

        public bool doToxicCloud = true;
        public bool doFireExplosion = true;

        public HediffCompProperties_SlugExplosionOnDeath() => this.compClass = typeof(HediffComp_SlugExplosionOnDeath);

    }
}
