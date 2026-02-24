using Verse;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;

namespace Milk
{
    /// <summary>
    /// Collection of pawn designators lists
    /// </summary>
    public class DesignatorsData : WorldComponent
    {
        public DesignatorsData(World world) : base(world)
        {
            //nothing needed here yet
        }

        public static List<Pawn> milkAllowManual = new List<Pawn>();
        public static List<Pawn> milkAllowMachine = new List<Pawn>();
        public static List<Pawn> milkAllowSelf = new List<Pawn>();
        public static List<Pawn> milkAllowBfAdult = new List<Pawn>();
        public static List<Pawn> milkAllowBfBaby = new List<Pawn>();
        public static List<Pawn> milkWillManual = new List<Pawn>();
        public static List<Pawn> milkWillBfAdult = new List<Pawn>();
        public static List<Pawn> milkShouldProduce = new List<Pawn>();

        /// <summary>
        /// update designators on game load
        /// </summary>
        public void Update()
        {

            milkAllowManual = PawnsFinder.All_AliveOrDead.Where(p => p.IsDesignatedMilkAllowManual()).ToList();
            milkAllowMachine = PawnsFinder.All_AliveOrDead.Where(p => p.IsDesignatedMilkAllowMachine()).ToList();
            milkAllowSelf = PawnsFinder.All_AliveOrDead.Where(p => p.IsDesignatedMilkAllowSelf()).ToList();
            milkAllowBfAdult = PawnsFinder.All_AliveOrDead.Where(p => p.IsDesignatedMilkAllowBfAdult()).ToList();
            milkAllowBfBaby = PawnsFinder.All_AliveOrDead.Where(p => p.IsDesignatedMilkAllowBfBaby()).ToList();
            milkWillManual = PawnsFinder.All_AliveOrDead.Where(p => p.IsDesignatedMilkWillManual()).ToList();
            milkWillBfAdult = PawnsFinder.All_AliveOrDead.Where(p => p.IsDesignatedMilkWillBfAdult()).ToList();
            milkShouldProduce = PawnsFinder.All_AliveOrDead.Where(p => p.IsDesignatedMilkShouldProduce()).ToList();

        }
    }
}
