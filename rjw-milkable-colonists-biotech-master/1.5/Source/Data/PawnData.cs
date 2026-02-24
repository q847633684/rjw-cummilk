using System;
using Verse;
using System.Linq;
using RimWorld;
using System.Collections.Generic;
using rjw;

namespace Milk
{
    public class PawnData : IExposable
    {
        public Pawn Pawn = null;
        public bool MilkAllowManual = true;
        public bool MilkAllowMachine = true;
        public bool MilkAllowSelf = true;
        public bool MilkAllowBfAdult = true;
        public bool MilkAllowBfBaby = true;

        public bool MilkWillManual = true;
        public bool MilkWillBfAdult = true;

        public bool MilkAvailable = false;

        public bool MilkShouldProduce = false;

        public bool CanDesignateMilkAllowManual = false;
        public bool CanDesignateMilkAllowMachine = false;
        public bool CanDesignateMilkAllowSelf = false;
        public bool CanDesignateMilkAllowBfAdult = false;
        public bool CanDesignateMilkAllowBfBaby = false;

        public bool CanDesignateMilkWillManual = false;
        public bool CanDesignateMilkWillBfAdult = false;

        public bool CanDesignateMilkShouldProduce = false;

        //public Pawn_DrugPolicyTracker drugs;

        //save these as a list? do I need it?
        //public List<Hediff> lactations = new List<Hediff>();

        public PawnData() { }

        public PawnData(Pawn pawn)
        {
            Pawn = pawn;
            //do I need to do a check if the pawn has lactating hediffs here?
            //I don't think so... I'll just let the options be set so you don't have to reset them if you lose/add the hediffs
        }
        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "Pawn");
            Scribe_Values.Look(ref MilkAllowManual, "MilkAllowManual", true, true);
            Scribe_Values.Look(ref MilkAllowMachine, "MilkAllowMachine", true, true);
            Scribe_Values.Look(ref MilkAllowSelf, "MilkAllowSelf", false, true);
            Scribe_Values.Look(ref MilkAllowBfAdult, "MilkAllowBFAdult", true, true);
            Scribe_Values.Look(ref MilkAllowBfBaby, "MilkAllowBFBaby", true, true);
            Scribe_Values.Look(ref MilkWillManual, "MilkWillManual", false, true);
            Scribe_Values.Look(ref MilkWillBfAdult, "MilkWillBFAdult", false, true);
            Scribe_Values.Look(ref MilkShouldProduce, "MilkShouldProduce", false, true);
            Scribe_Values.Look(ref CanDesignateMilkAllowManual, "CanDesignateMilkAllowManual", false, true);
            Scribe_Values.Look(ref CanDesignateMilkAllowMachine, "CanDesignateMilkAllowMachine", false, true);
            Scribe_Values.Look(ref CanDesignateMilkAllowSelf, "CanDesignateMilkAllowSelf", false, true);
            Scribe_Values.Look(ref CanDesignateMilkAllowBfAdult, "CanDesignateMilkAllowBfAdult", false, true);
            Scribe_Values.Look(ref CanDesignateMilkAllowBfBaby, "CanDesignateMilkAllowBfBaby", true, true);
            Scribe_Values.Look(ref CanDesignateMilkWillManual, "CanDesignateMilkWillManual", false, true);
            Scribe_Values.Look(ref CanDesignateMilkWillBfAdult, "CanDesignateMilkWillBfAdult", false, true);
            Scribe_Values.Look(ref CanDesignateMilkShouldProduce, "CanDesignateMilkShouldProduce", false, true);

        }
        public bool IsValid { get { return Pawn != null; } }
    }
}


