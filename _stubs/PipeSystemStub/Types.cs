#pragma warning disable CS0626, CS0824, CS0114, CS0108, CS0067, CS0649, CS0169, CS0414, CS0109
using System;
using System.Collections.Generic;
using Verse;

namespace PipeSystem
{
    public class CompResourceStorage : ThingComp
    {
        public float amountStored;
        public int ticksWithoutPower;
        public float AmountStored => amountStored;
        public float AmountCanAccept => 0;
        public virtual void AddResource(float amount) { }
        public virtual void DrawResource(float amount) { }
    }
    public class CompProperties_ResourceStorage : CompProperties
    {
        public float storageCapacity;
        public bool destroyOnEmpty;
        public float barSize;
    }
    public class Alert_NoStorage
    {
        public List<Thing> ThingsList => null;
    }
    public class PipeNetDef : Def { }
    public class CompResource : ThingComp { }
}
