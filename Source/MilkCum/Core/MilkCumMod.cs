using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using MilkCum.Core.Settings;

namespace MilkCum.Core;
public class MilkCumMod : Mod
{
	internal static MilkCumSettings Settings;
	public static HarmonyLib.Harmony Harmony;
	public static ModContentPack milkCumMod;
	public MilkCumMod(ModContentPack content)
		: base(content)
	{
		Harmony = new HarmonyLib.Harmony("com.akaster.rimworld.mod.milkcum");
		Settings = GetSettings<MilkCumSettings>();
		milkCumMod = content;
		if (!ModIntegrationGates.RjwModActive)
			Log.Error("[MilkCum] " + "EM.RequiresRJWActive".Translate());
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		if (Settings == null) return;
		try
		{
			Settings.DoWindowContents(inRect);
		}
		catch (System.Exception ex)
		{
			Verse.Log.Error($"[MilkCum] Settings error: {ex}");
			Widgets.Label(inRect, "MilkCum_ModName".Translate() + ": " + "EM.SettingsLoadError".Translate());
		}
	}
	public override void WriteSettings()
	{
		if (Settings != null) Settings.UpdateMilkCumSettings();
		base.WriteSettings();
	}

	public override string SettingsCategory()
	{
		return "MilkCum_ModName".Translate();
	}

}
