using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
namespace MilkCum.Core;
public class EqualMilkingMod : Mod
{
	internal static EqualMilkingSettings Settings;
	public static Harmony Harmony;
	public static ModContentPack equalMilkingMod;
	public EqualMilkingMod(ModContentPack content)
		: base(content)
	{
		Harmony = new Harmony("com.akaster.rimworld.mod.milkcum");
		Settings = base.GetSettings<EqualMilkingSettings>();
		equalMilkingMod = content;
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		if (Settings == null) return;
		try
		{
			Settings.DoWindowContents(inRect);
		}
		catch (System.InvalidCastException ex)
		{
			Verse.Log.Error($"[Equal Milking] Settings cast error: {ex.Message}");
			Widgets.Label(inRect, "Equal_Milking".Translate() + ": " + "EM.SettingsLoadError".Translate());
		}
	}
	public override void WriteSettings()
	{
		if (Settings != null) Settings.UpdateEqualMilkingSettings();
		base.WriteSettings();
	}

	public override string SettingsCategory()
	{
		return "Equal_Milking".Translate();
	}

}
