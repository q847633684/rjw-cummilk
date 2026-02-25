using UnityEngine;
using Verse;

namespace EqualMilking.UI;

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

		listing.CheckboxLabeled("cumpilation_settings_enable_cumflation_key".Translate() + ": ", ref EqualMilkingSettings.Cumpilation_EnableCumflation);
		if (EqualMilkingSettings.Cumpilation_EnableCumflation)
		{
			listing.Gap(Gap);
			listing.Label("cumpilation_settings_cumflation_modifier_key".Translate() + ": " + EqualMilkingSettings.Cumpilation_GlobalCumflationModifier.ToString("F1"));
			EqualMilkingSettings.Cumpilation_GlobalCumflationModifier = listing.Slider(EqualMilkingSettings.Cumpilation_GlobalCumflationModifier, 0.1f, 5f);
		}
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_settings_enable_stuffing_key".Translate() + ": ", ref EqualMilkingSettings.Cumpilation_EnableStuffing);
		if (EqualMilkingSettings.Cumpilation_EnableStuffing)
		{
			listing.Gap(Gap);
			listing.Label("cumpilation_settings_stuffing_modifier_key".Translate() + ": " + EqualMilkingSettings.Cumpilation_GlobalStuffingModifier.ToString("F1"));
			EqualMilkingSettings.Cumpilation_GlobalStuffingModifier = listing.Slider(EqualMilkingSettings.Cumpilation_GlobalStuffingModifier, 0.1f, 5f);
		}
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_settings_enable_bukkake_key".Translate() + ": ", ref EqualMilkingSettings.Cumpilation_EnableBukkake);
		if (EqualMilkingSettings.Cumpilation_EnableBukkake)
		{
			listing.Gap(Gap);
			listing.Label("cumpilation_settings_bukkake_modifier_key".Translate() + ": " + EqualMilkingSettings.Cumpilation_GlobalBukkakeModifier.ToString("F1"));
			EqualMilkingSettings.Cumpilation_GlobalBukkakeModifier = listing.Slider(EqualMilkingSettings.Cumpilation_GlobalBukkakeModifier, 0.1f, 5f);
		}
		listing.CheckboxLabeled("cumpilation_settings_enable_fluid_gathering_while_cleaning_key".Translate() + ": ", ref EqualMilkingSettings.Cumpilation_EnableFluidGatheringWhileCleaning);
		listing.Gap(Gap);
		listing.Label("cumpilation_settings_max_gathering_check_distance_key".Translate() + ": " + EqualMilkingSettings.Cumpilation_MaxGatheringCheckDistance.ToString("F1"));
		EqualMilkingSettings.Cumpilation_MaxGatheringCheckDistance = listing.Slider(EqualMilkingSettings.Cumpilation_MaxGatheringCheckDistance, 3f, 50f);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_settings_enable_progressing_consumption_thoughts_key".Translate() + ": ", ref EqualMilkingSettings.Cumpilation_EnableProgressingConsumptionThoughts);
		listing.Gap(GapSection);
		listing.CheckboxLabeled("cumpilation_settings_enable_oscillation_mechanics_key".Translate() + ": ", ref EqualMilkingSettings.Cumpilation_EnableOscillationMechanics);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_settings_enable_oscillation_mechanics_animals_key".Translate() + ": ", ref EqualMilkingSettings.Cumpilation_EnableOscillationMechanicsForAnimals);
		listing.Gap(GapSection);
		listing.CheckboxLabeled("cumpilation_settings_enable_debug_logging_key".Translate() + ": ", ref EqualMilkingSettings.Cumpilation_EnableDebugLogging);

		listing.Gap(GapSection * 2);

		// --- 泄精/Leaking 设置 ---
		Widgets.Label(listing.GetRect(24f), "cumpilation_cumsettings_menuname".Translate());
		listing.Gap(Gap);

		listing.CheckboxLabeled("cumpilation_cumsettings_enable_filth_key".Translate(), ref EqualMilkingSettings.CumpilationLeak_EnableFilthGeneration);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_autodeflatebucket_key".Translate(), ref EqualMilkingSettings.CumpilationLeak_EnableAutoDeflateBucket);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_autodeflatedubs_key".Translate(), ref EqualMilkingSettings.CumpilationLeak_EnableAutoDeflateClean);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_autodeflatedirty_key".Translate(), ref EqualMilkingSettings.CumpilationLeak_EnableAutoDeflateDirty);
		listing.Gap(Gap);
		listing.CheckboxLabeled("cumpilation_cumsettings_enable_privacy_key".Translate(), ref EqualMilkingSettings.CumpilationLeak_EnablePrivacy);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_min_autodeflate_key".Translate() + EqualMilkingSettings.CumpilationLeak_AutoDeflateMinSeverity.ToString("F2"));
		EqualMilkingSettings.CumpilationLeak_AutoDeflateMinSeverity = listing.Slider(EqualMilkingSettings.CumpilationLeak_AutoDeflateMinSeverity, 0f, 3f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_max_deflatedistance_key".Translate() + EqualMilkingSettings.CumpilationLeak_AutoDeflateMaxDistance.ToString("F1"));
		EqualMilkingSettings.CumpilationLeak_AutoDeflateMaxDistance = listing.Slider(EqualMilkingSettings.CumpilationLeak_AutoDeflateMaxDistance, 0f, 1000f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_leak_amount_multi_key".Translate() + EqualMilkingSettings.CumpilationLeak_LeakMult.ToString("F1"));
		EqualMilkingSettings.CumpilationLeak_LeakMult = listing.Slider(EqualMilkingSettings.CumpilationLeak_LeakMult, 0.1f, 10f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_leak_speed_multi_key".Translate() + EqualMilkingSettings.CumpilationLeak_LeakRate.ToString("F1"));
		EqualMilkingSettings.CumpilationLeak_LeakRate = listing.Slider(EqualMilkingSettings.CumpilationLeak_LeakRate, 0.1f, 10f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_deflate_amount_multi_key".Translate() + EqualMilkingSettings.CumpilationLeak_DeflateMult.ToString("F1"));
		EqualMilkingSettings.CumpilationLeak_DeflateMult = listing.Slider(EqualMilkingSettings.CumpilationLeak_DeflateMult, 0.1f, 10f);
		listing.Gap(Gap);
		listing.Label("cumpilation_cumsettings_deflate_speed_multi_key".Translate() + EqualMilkingSettings.CumpilationLeak_DeflateRate.ToString("F1"));
		EqualMilkingSettings.CumpilationLeak_DeflateRate = listing.Slider(EqualMilkingSettings.CumpilationLeak_DeflateRate, 0.1f, 10f);

		listing.End();
	}
}
