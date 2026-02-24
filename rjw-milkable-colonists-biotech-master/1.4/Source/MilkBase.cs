using System;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using rjw;
using UnityEngine;
using Verse;


namespace Milk.Settings
{
    public class MilkSettingsMain : Mod
    {
        public MilkSettingsMain(ModContentPack content) : base(content)
        {
            GetSettings<MilkSettings>();
        }

        public override string SettingsCategory()
        {
            return "MilkableColonists.settings".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {

            MilkSettings.DoWindowContents(inRect);
        }
    }
}
