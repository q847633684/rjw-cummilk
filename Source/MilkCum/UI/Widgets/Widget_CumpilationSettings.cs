using MilkCum.Core;
using UnityEngine;
using Verse;

namespace MilkCum.UI;

/// <summary>精液/Cumpilation 设置：膨胀、填充、覆盖、收集、泄精等，统一到主 mod 设置的一个 Tab。</summary>
public class Widget_CumpilationSettings
{
	private const float Gap = 4f;
	private const float GapSection = 12f;

	public void Draw(Rect inRect)
	{
		var listing = new Listing_Standard { maxOneColumn = true, ColumnWidth = inRect.width - 20f };
		listing.Begin(inRect);

		// --- Cumpilation 主设置 ---
		Widgets.Label(listing.GetRect(24f), "cumpilation_settings_menuname".Translate());
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_settings_enable_cumflation_key".Translate() + ": ", ref MilkCumSettings.Cumpilation_EnableCumflation);
		if (MilkCumSettings.Cumpilation_EnableCumflation)
		{
			listing.Gap(Gap);
			listing.Label("cumpilation_settings_cumflation_modifier_key".Translate() + ": " + MilkCumSettings.Cumpilation_GlobalCumflationModifier.ToString("F1"));
			MilkCumSettings.Cumpilation_GlobalCumflationModifier = listing.Slider(MilkCumSettings.Cumpilation_GlobalCumflationModifier, 0.1f, 5f);
		}
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_settings_enable_stuffing_key".Translate() + ": ", ref MilkCumSettings.Cumpilation_EnableStuffing);
		if (MilkCumSettings.Cumpilation_EnableStuffing)
		{
			listing.Gap(Gap);
			listing.Label("cumpilation_settings_stuffing_modifier_key".Translate() + ": " + MilkCumSettings.Cumpilation_GlobalStuffingModifier.ToString("F1"));
			MilkCumSettings.Cumpilation_GlobalStuffingModifier = listing.Slider(MilkCumSettings.Cumpilation_GlobalStuffingModifier, 0.1f, 5f);
		}
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_settings_enable_bukkake_key".Translate() + ": ", ref MilkCumSettings.Cumpilation_EnableBukkake);
		if (MilkCumSettings.Cumpilation_EnableBukkake)
		{
			listing.Gap(Gap);
			listing.Label("cumpilation_settings_bukkake_modifier_key".Translate() + ": " + MilkCumSettings.Cumpilation_GlobalBukkakeModifier.ToString("F1"));
			MilkCumSettings.Cumpilation_GlobalBukkakeModifier = listing.Slider(MilkCumSettings.Cumpilation_GlobalBukkakeModifier, 0.1f, 5f);
		}
		listing.CheckboxLabeled("cumpilation_settings_enable_fluid_gathering_while_cleaning_key".Translate() + ": ", ref MilkCumSettings.Cumpilation_EnableFluidGatheringWhileCleaning);
		listing.Gap(Gap);
		listing.Label("cumpilation_settings_max_gathering_check_distance_key".Translate() + ": " + MilkCumSettings.Cumpilation_MaxGatheringCheckDistance.ToString("F1"));
		MilkCumSettings.Cumpilation_MaxGatheringCheckDistance = listing.Slider(MilkCumSettings.Cumpilation_MaxGatheringCheckDistance, 3f, 50f);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_settings_enable_progressing_consumption_thoughts_key".Translate() + ": ", ref MilkCumSettings.Cumpilation_EnableProgressingConsumptionThoughts);
		listing.Gap(GapSection);
		listing.CheckboxLabeled("cumpilation_settings_enable_oscillation_mechanics_key".Translate() + ": ", ref MilkCumSettings.Cumpilation_EnableOscillationMechanics);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_settings_enable_oscillation_mechanics_animals_key".Translate() + ": ", ref MilkCumSettings.Cumpilation_EnableOscillationMechanicsForAnimals);
		listing.Gap(GapSection);
		listing.CheckboxLabeled("cumpilation_settings_enable_debug_logging_key".Translate() + ": ", ref MilkCumSettings.Cumpilation_EnableDebugLogging);

		listing.Gap(GapSection * 2);

		// --- 泄精/Leaking 设置 ---
		Widgets.Label(listing.GetRect(24f), "cumpilation_cumsettings_menuname".Translate());
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_cumsettings_enable_filth_key".Translate(), ref MilkCumSettings.CumpilationLeak_EnableFilthGeneration);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_autodeflatebucket_key".Translate(), ref MilkCumSettings.CumpilationLeak_EnableAutoDeflateBucket);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_autodeflatedubs_key".Translate(), ref MilkCumSettings.CumpilationLeak_EnableAutoDeflateClean);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_autodeflatedirty_key".Translate(), ref MilkCumSettings.CumpilationLeak_EnableAutoDeflateDirty);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_privacy_key".Translate(), ref MilkCumSettings.CumpilationLeak_EnablePrivacy);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_min_autodeflate_key".Translate() + MilkCumSettings.CumpilationLeak_AutoDeflateMinSeverity.ToString("F2"));
		MilkCumSettings.CumpilationLeak_AutoDeflateMinSeverity = listing.Slider(MilkCumSettings.CumpilationLeak_AutoDeflateMinSeverity, 0f, 3f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_max_deflatedistance_key".Translate() + MilkCumSettings.CumpilationLeak_AutoDeflateMaxDistance.ToString("F1"));
		MilkCumSettings.CumpilationLeak_AutoDeflateMaxDistance = listing.Slider(MilkCumSettings.CumpilationLeak_AutoDeflateMaxDistance, 0f, 1000f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_leak_amount_multi_key".Translate() + MilkCumSettings.CumpilationLeak_LeakMult.ToString("F1"));
		MilkCumSettings.CumpilationLeak_LeakMult = listing.Slider(MilkCumSettings.CumpilationLeak_LeakMult, 0.1f, 10f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_leak_speed_multi_key".Translate() + MilkCumSettings.CumpilationLeak_LeakRate.ToString("F1"));
		MilkCumSettings.CumpilationLeak_LeakRate = listing.Slider(MilkCumSettings.CumpilationLeak_LeakRate, 0.1f, 10f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_deflate_amount_multi_key".Translate() + MilkCumSettings.CumpilationLeak_DeflateMult.ToString("F1"));
		MilkCumSettings.CumpilationLeak_DeflateMult = listing.Slider(MilkCumSettings.CumpilationLeak_DeflateMult, 0.1f, 10f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_deflate_speed_multi_key".Translate() + MilkCumSettings.CumpilationLeak_DeflateRate.ToString("F1"));
		MilkCumSettings.CumpilationLeak_DeflateRate = listing.Slider(MilkCumSettings.CumpilationLeak_DeflateRate, 0.1f, 10f);

		listing.End();
	}
}
