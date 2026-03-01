using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;

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
		try
		{
			Settings = base.GetSettings<EqualMilkingSettings>();
		}
		catch (System.InvalidCastException)
		{
			Verse.Log.Warning("[Equal Milking] Saved settings type was invalid or from an older version, using defaults.");
			Settings = new EqualMilkingSettings();
			// 让基类也使用当前实例，以便保存时写入正确对象
			try
			{
				var field = typeof(Mod).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				field?.SetValue(this, Settings);
			}
			catch { /* 忽略反射失败 */ }
		}
		equalMilkingMod = content;
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
			Verse.Log.Error($"[Equal Milking] Settings error: {ex}");
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
