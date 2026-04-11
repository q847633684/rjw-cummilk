using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using rjw;
using RJW_Menstruation;
using RJW_Menstruation_Fluids;
using RwMenstruationCum = RJW_Menstruation.Cum;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Fluids.Cum.Common;

/// <summary>
/// Menstruation Fluids 程序集联动：从 MF 内部精液列表读取/扣除，供桶泄、脏泄、手术提取使用（与 CumpilationLite 同源逻辑）。
/// 硬前置 RJW Menstruation + Menstruation Fluids（见 About.xml / MilkCum.csproj）。
/// </summary>
public partial class FluidUtility
{
	public static float GetCumflationFluidCapacity(FluidsCumWithHediff fluidscum)
	{
		if (fluidscum == null)
			return 1f;
		Hediff cumholdheddif = fluidscum.container;
		HediffComp_FluidsCumComp fluidInCumhold = cumholdheddif.TryGetComp<HediffComp_FluidsCumComp>();
		if (fluidInCumhold == null)
		{
			if (cumholdheddif.def is not HediffDef_SexPart sexPart || sexPart.genitalFamily != GenitalFamily.Vagina)
				return 1f;
			HediffComp_Menstruation vaginalCumhold = cumholdheddif.TryGetComp<HediffComp_Menstruation>();
			if (vaginalCumhold == null)
				return 1f;
			return vaginalCumhold.Props.maxCumCapacity * vaginalCumhold.Pawn.BodySize;
		}
		return RJW_Menstruation_Fluids.Settings.L1AnalCumCapacity * fluidInCumhold.Pawn.BodySize;
	}
}

public class FluidsCumWithHediff : FluidsCum
{
	public Hediff container;
}

public static class CreateFluidList
{
	public static List<FluidsCumWithHediff> GetSources(Hediff cumflationHediff)
	{
		Pawn pawn = cumflationHediff.pawn;
		var list = new List<FluidsCumWithHediff>();
		List<Hediff> cumholder;
		if (cumflationHediff.def == global::MilkCum.Fluids.Cum.MenstruationFluidsCompat.MenstruationCumflationAnal)
			cumholder = Genital_Helper.get_PartsHediffList(pawn, Genital_Helper.get_anusBPR(pawn));
		else
			cumholder = Genital_Helper.get_PartsHediffList(pawn, Genital_Helper.get_genitalsBPR(pawn));

		foreach (Hediff cumholdheddif in cumholder)
		{
			HediffComp_FluidsCumComp fluidInCumhold = cumholdheddif.TryGetComp<HediffComp_FluidsCumComp>();
			if (fluidInCumhold == null)
			{
				if (cumholdheddif.def is not HediffDef_SexPart sexPart || sexPart.genitalFamily != GenitalFamily.Vagina)
					continue;
				HediffComp_Menstruation vaginalCumhold = cumholdheddif.TryGetComp<HediffComp_Menstruation>();
				var field = AccessTools.Field(typeof(HediffComp_Menstruation), "cums");
				var cumList = (List<RwMenstruationCum>)field.GetValue(vaginalCumhold);
				foreach (RwMenstruationCum element in cumList)
				{
					if (element.CumThing == null && element.FilthDef == null)
						continue;
					List<SexFluidDef> allFluids = DefDatabase<SexFluidDef>.AllDefsListForReading;
					SexFluidDef correctSexFluid = null;
					foreach (SexFluidDef fluid in allFluids)
					{
						if ((fluid.consumable == element.CumThing?.def && fluid.consumable != null)
						    || (fluid.filth == element.FilthDef && fluid.filth != null))
						{
							correctSexFluid = fluid;
							break;
						}
					}

					list.Add(new FluidsCumWithHediff
					{
						sourcePawn = element.pawn,
						sexFluid = correctSexFluid,
						fluidVolume = element.Volume,
						fluidColor = new Color(1f, 1f, 1f),
						container = cumholdheddif
					});
				}
			}
			else
			{
				list.AddRange(
					fluidInCumhold.cumListAnal.Select(c => new FluidsCumWithHediff
					{
						sourcePawn = c.sourcePawn,
						sexFluid = c.sexFluid,
						fluidVolume = c.fluidVolume,
						fluidColor = c.fluidColor,
						container = cumholdheddif
					}));
			}
		}

		if (list.Count == 0)
		{
			list.Add(new FluidsCumWithHediff
			{
				sourcePawn = pawn,
				sexFluid = DefDatabase<SexFluidDef>.GetNamed("Cum"),
				fluidVolume = 10f,
				fluidColor = new Color(1f, 1f, 1f),
				container = null
			});
		}

		return list;
	}
}

public static class MenstruationCumExtensions
{
	static readonly FieldInfo VolumeField = typeof(RwMenstruationCum).GetField("volume", BindingFlags.Instance | BindingFlags.NonPublic);

	public static void ReduceVolume(this RwMenstruationCum cum, float amount)
	{
		if (cum == null || VolumeField == null)
			return;
		float currentVolume = (float)VolumeField.GetValue(cum);
		currentVolume = Math.Max(0f, currentVolume - amount);
		VolumeField.SetValue(cum, currentVolume);
	}

	public static void SetVolumeZero(this RwMenstruationCum cum)
	{
		if (cum == null || VolumeField == null)
			return;
		VolumeField.SetValue(cum, 0f);
	}
}
