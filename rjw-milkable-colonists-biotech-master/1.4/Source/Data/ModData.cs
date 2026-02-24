using HugsLib;
using RimWorld;
using Verse;

namespace Milk
{
    /// <summary>
    /// Milk settings store
    /// </summary>
    public class SaveStorage : ModBase
    {
        public override string ModIdentifier => ModId;
        public static string ModId => "Milk";

        public static DataStore DataStore;//reference to savegame data, hopefully
        public static DesignatorsData DesignatorsData;//reference to savegame data, hopefully

        public override void SettingsChanged()
        {
            ToggleTabIfNeeded();
        }

        public override void WorldLoaded()
        {
            DataStore = Find.World.GetComponent<DataStore>();
            DesignatorsData = Find.World.GetComponent<DesignatorsData>();
            DesignatorsData.Update();
            ToggleTabIfNeeded();
            //this would be where I would add my own on load setup if needed.
            //LoadStuffIfNeeded();
        }
        protected override bool HarmonyAutoPatch { get => false; }//first.cs creates harmony and does some convoulted stuff with it

        private void ToggleTabIfNeeded()
        {
            //DefDatabase<MainButtonDef>.GetNamed("RJW_Brothel").buttonVisible = RJWSettings.whoringtab_enabled;
        }

        //private void LoadStuffIfNeeded() { }

    }
}
