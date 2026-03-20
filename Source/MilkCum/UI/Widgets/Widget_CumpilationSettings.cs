using MilkCum.Core;
using UnityEngine;
using Verse;

namespace MilkCum.UI;

/// <summary>绮炬恫/Cumpilation 璁剧疆锛氳啫鑳€銆佸～鍏呫€佽鐩栥€佹敹闆嗐€佹硠绮剧瓑锛岀粺涓€鍒颁富 mod 璁剧疆鐨勪竴涓?Tab銆</summary>
public class Widget_CumpilationSettings
{
	private const float Gap = 4f;
	private const float GapSection = 12f;

	public void Draw(Rect inRect)
	{
		var listing = new Listing_Standard { maxOneColumn = true, ColumnWidth = inRect.width - 20f };
		listing.Begin(inRect);

		// --- Cumpilation 涓昏缃?---
		Widgets.Label(listing.GetRect(24f), "cumpilation_settings_menuname".Translate());
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_settings_enable_cumflation_key".Translate() + ": ", ref MilkCumSettings.Cum_EnableCumflation);
		if (MilkCumSettings.Cum_EnableCumflation)
		{
			listing.Gap(Gap);
			listing.Label("cumpilation_settings_cumflation_modifier_key".Translate() + ": " + MilkCumSettings.Cum_GlobalCumflationModifier.ToString("F1"));
			MilkCumSettings.Cum_GlobalCumflationModifier = listing.Slider(MilkCumSettings.Cum_GlobalCumflationModifier, 0.1f, 5f);
		}
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_settings_enable_stuffing_key".Translate() + ": ", ref MilkCumSettings.Cum_EnableStuffing);
		if (MilkCumSettings.Cum_EnableStuffing)
		{
			listing.Gap(Gap);
			listing.Label("cumpilation_settings_stuffing_modifier_key".Translate() + ": " + MilkCumSettings.Cum_GlobalStuffingModifier.ToString("F1"));
			MilkCumSettings.Cum_GlobalStuffingModifier = listing.Slider(MilkCumSettings.Cum_GlobalStuffingModifier, 0.1f, 5f);
		}
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_settings_enable_bukkake_key".Translate() + ": ", ref MilkCumSettings.Cum_EnableBukkake);
		if (MilkCumSettings.Cum_EnableBukkake)
		{
			listing.Gap(Gap);
			listing.Label("cumpilation_settings_bukkake_modifier_key".Translate() + ": " + MilkCumSettings.Cum_GlobalBukkakeModifier.ToString("F1"));
			MilkCumSettings.Cum_GlobalBukkakeModifier = listing.Slider(MilkCumSettings.Cum_GlobalBukkakeModifier, 0.1f, 5f);
		}
		listing.CheckboxLabeled("cumpilation_settings_enable_fluid_gathering_while_cleaning_key".Translate() + ": ", ref MilkCumSettings.Cum_EnableFluidGatheringWhileCleaning);
		listing.Gap(Gap);
		listing.Label("cumpilation_settings_max_gathering_check_distance_key".Translate() + ": " + MilkCumSettings.Cum_MaxGatheringCheckDistance.ToString("F1"));
		MilkCumSettings.Cum_MaxGatheringCheckDistance = listing.Slider(MilkCumSettings.Cum_MaxGatheringCheckDistance, 3f, 50f);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_settings_enable_progressing_consumption_thoughts_key".Translate() + ": ", ref MilkCumSettings.Cum_EnableProgressingConsumptionThoughts);
		listing.Gap(GapSection);
		listing.CheckboxLabeled("cumpilation_settings_enable_oscillation_mechanics_key".Translate() + ": ", ref MilkCumSettings.Cum_EnableOscillationMechanics);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_settings_enable_oscillation_mechanics_animals_key".Translate() + ": ", ref MilkCumSettings.Cum_EnableOscillationMechanicsForAnimals);
		listing.Gap(GapSection);
		listing.CheckboxLabeled("cumpilation_settings_enable_debug_logging_key".Translate() + ": ", ref MilkCumSettings.Cum_EnableDebugLogging);

		listing.Gap(GapSection * 2);

		// --- 娉勭簿/Leaking 璁剧疆 ---
		Widgets.Label(listing.GetRect(24f), "cumpilation_cumsettings_menuname".Translate());
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_cumsettings_enable_filth_key".Translate(), ref MilkCumSettings.CumLeak_EnableFilthGeneration);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_autodeflatebucket_key".Translate(), ref MilkCumSettings.CumLeak_EnableAutoDeflateBucket);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_autodeflatedubs_key".Translate(), ref MilkCumSettings.CumLeak_EnableAutoDeflateClean);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_autodeflatedirty_key".Translate(), ref MilkCumSettings.CumLeak_EnableAutoDeflateDirty);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_privacy_key".Translate(), ref MilkCumSettings.CumLeak_EnablePrivacy);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_min_autodeflate_key".Translate() + MilkCumSettings.CumLeak_AutoDeflateMinSeverity.ToString("F2"));
		MilkCumSettings.CumLeak_AutoDeflateMinSeverity = listing.Slider(MilkCumSettings.CumLeak_AutoDeflateMinSeverity, 0f, 3f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_max_deflatedistance_key".Translate() + MilkCumSettings.CumLeak_AutoDeflateMaxDistance.ToString("F1"));
		MilkCumSettings.CumLeak_AutoDeflateMaxDistance = listing.Slider(MilkCumSettings.CumLeak_AutoDeflateMaxDistance, 0f, 1000f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_leak_amount_multi_key".Translate() + MilkCumSettings.CumLeak_LeakMult.ToString("F1"));
		MilkCumSettings.CumLeak_LeakMult = listing.Slider(MilkCumSettings.CumLeak_LeakMult, 0.1f, 10f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_leak_speed_multi_key".Translate() + MilkCumSettings.CumLeak_LeakRate.ToString("F1"));
		MilkCumSettings.CumLeak_LeakRate = listing.Slider(MilkCumSettings.CumLeak_LeakRate, 0.1f, 10f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_deflate_amount_multi_key".Translate() + MilkCumSettings.CumLeak_DeflateMult.ToString("F1"));
		MilkCumSettings.CumLeak_DeflateMult = listing.Slider(MilkCumSettings.CumLeak_DeflateMult, 0.1f, 10f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_deflate_speed_multi_key".Translate() + MilkCumSettings.CumLeak_DeflateRate.ToString("F1"));
		MilkCumSettings.CumLeak_DeflateRate = listing.Slider(MilkCumSettings.CumLeak_DeflateRate, 0.1f, 10f);

		listing.End();
	}
}
