using Verse;
using RimWorld;
using rjw;
using System.Security.Cryptography;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Milk
{ 
    public static class Designator_MilkShouldProduce
    {
        public static bool UpdateCanDesignateMilkShouldProduce(this Pawn pawn)
        {
            if (!pawn.Dead)
            {
                //to do: return false if the pawn is a baby
                //probably more things too.

                return pawn.GetMilkPawnData().CanDesignateMilkShouldProduce = true;
            }

            return pawn.GetMilkPawnData().CanDesignateMilkShouldProduce = false;
        }
        public static bool CanDesignateMilkShouldProduce(this Pawn pawn)
        {
            return pawn.GetMilkPawnData().CanDesignateMilkShouldProduce;
        }
        public static void ToggleMilkShouldProduce(this Pawn pawn)
        {
            pawn.UpdateCanDesignateMilkShouldProduce();
            if (pawn.CanDesignateMilkShouldProduce())
            {
                if (!pawn.IsDesignatedMilkShouldProduce())
                    DesignateMilkShouldProduce(pawn);
                else
                    UnDesignateMilkShouldProduce(pawn);
            }
        }
        public static bool IsDesignatedMilkShouldProduce(this Pawn pawn)
        {
            if (pawn.GetMilkPawnData().MilkShouldProduce)
            {
                if (pawn.Dead)
                    pawn.UnDesignateMilkShouldProduce();
            }
            return pawn.GetMilkPawnData().MilkShouldProduce;
        }
        //[SyncMethod]
        public static void DesignateMilkShouldProduce(this Pawn pawn)
        {
            DesignatorsData.milkShouldProduce.AddDistinct(pawn);
            pawn.GetMilkPawnData().MilkShouldProduce = true;

            //add the comp if they dont have it. note that we'll never remove the comp.
            ThingDef pawnThingDef = DefDatabase<ThingDef>.GetNamed(pawn.def.ToString(), false);
            Log.Message(pawnThingDef.ToString());

            var comp = ThingCompUtility.TryGetComp<CompMilkableHuman>(pawn);
            if (comp == null)
            {
                Log.Message("Comp is Null. Trying to create Comp");
                //I have no idea how to add a comp in code...or if it can even be done. let's find out!
                //trying to tweak code taken from comp initialise...
                //var comps = new List<ThingComp>();
                try
                {

                    //pawnThingDef.comps.Add(new CompProperties(typeof(CompMilkableHuman)));

                    /*

                        //.GetNamed(findDef, false);
                    CompMilkableHuman thingComp = new CompMilkableHuman();

                    //CompMilkableHuman thingComp = (CompMilkableHuman)Activator.CreateInstance(typeof(CompMilkableHuman));
                    thingComp.parent = pawn;
                    //comps.Add(thingComp);

                    CompProperties_MilkableHuman compProps = new CompProperties_MilkableHuman();
                    compProps.compClass = typeof(CompMilkableHuman);
                    compProps.milkAmount = 6f;
                    compProps.milkAmountBase = 2f;
                    compProps.milkIntervalDays = 1f;
                    compProps.milkFemaleOnly = false;
                    compProps.milkDef = ThingDef.Named("HumanoidMilk");

                    thingComp.Initialize(compProps);
                    thingComp.PostPostMake();
                    */

                    //pawn.InitializeComps


                    /*
                                        List<ThingComp> list = typeof(Pawn).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pawn) as List<ThingComp>;
                                        if (list != null)
                                        {
                                            list.Add(thingComp);
                                            typeof(Pawn).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pawn, list);
                                        }
                    */

                    /*
                    var compB = ThingCompUtility.TryGetComp<CompMilkableHuman>(pawn);
                    if (compB == null)
                    {
                        Log.Message("Comp is null");
                    }
                    else
                    {
                        Log.Message("CompInitialised");
                    }*/

                }
                catch (Exception ex)
                {
                    Log.Error("Could not instantiate or initialize a ThingComp: " + ex);
                    //comps.Remove(thingComp);
                }

            }

        }
        //[SyncMethod]
        public static void UnDesignateMilkShouldProduce(this Pawn pawn)
        {
            DesignatorsData.milkShouldProduce.Remove(pawn);
            pawn.GetMilkPawnData().MilkShouldProduce = false;
        }
    }
}
