using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
namespace EqualMilking;
public class EqualMilkingMod : Mod
{
	internal static EqualMilkingSettings Settings;
	public static Harmony Harmony;
	public static ModContentPack equalMilkingMod;
	public EqualMilkingMod(ModContentPack content)
		: base(content)
	{
		Harmony = new Harmony("com.akaster.rimworld.mod.equalmilking");
		Settings = base.GetSettings<EqualMilkingSettings>();
		equalMilkingMod = content;
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		Settings.DoWindowContents(inRect);
	}
	public override void WriteSettings()
	{
		Settings.UpdateEqualMilkingSettings();
		base.WriteSettings();
	}

	public override string SettingsCategory()
	{
		return "Equal_Milking".Translate();
	}

}
