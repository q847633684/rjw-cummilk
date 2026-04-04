using System.Collections.Generic;
using System.Text;
using MilkCum.Core.Settings;
using UnityEngine;
using Verse;

namespace MilkCum.UI;

/// <summary>
/// 设置页：容量顶线、有效驱动（含激素饱和与 L 封顶）、池满度（示意）随归一化时间。
/// 用于直观理解参数联动；非存档 tick 级仿真。
/// </summary>
public static class LactationPoolSchematicGraph
{
	private static readonly Color PlotBg = new(0.12f, 0.12f, 0.14f, 0.92f);
	private static readonly Color AxisColor = new(0.55f, 0.55f, 0.55f);
	private static readonly Color GridColor = new(0.3f, 0.3f, 0.32f, 0.55f);
	private static readonly Color CapacityColor = new(1f, 0.82f, 0.35f, 0.95f);
	private static readonly Color FullnessColor = new(0.45f, 0.95f, 0.55f, 0.95f);
	private static readonly Color LeffColor = new(0.45f, 0.78f, 1f, 0.95f);
	private static readonly Color DoseMarkerColor = new(1f, 0.45f, 0.45f, 0.88f);

	private const float TitleH = 20f;
	private const float CurveBlockH = 142f;
	private const float BottomBlockH = 132f;

	public static void DrawInRect(Rect outer)
	{
		GameFont font = Text.Font;
		Color prevGui = GUI.color;
		try
		{
			Text.Font = GameFont.Small;
			Widgets.Label(new Rect(outer.x, outer.y, outer.width, TitleH), "EM.PoolSchematicTitle".Translate());
			Rect plotOuter = new Rect(outer.x, outer.y + TitleH + 2f, outer.width, CurveBlockH + BottomBlockH);
			Widgets.DrawBoxSolid(plotOuter, PlotBg);
			Widgets.DrawBox(plotOuter);
			TooltipHandler.TipRegion(plotOuter, BuildSchematicTooltip());

			const float padL = 36f;
			const float padR = 8f;
			const float padTopPlot = 14f;
			Rect plot = new Rect(plotOuter.x + padL, plotOuter.y + padTopPlot, plotOuter.width - padL - padR, CurveBlockH - padTopPlot - 22f);

			for (int gi = 0; gi <= 2; gi++)
			{
				float ny = gi * 0.5f;
				Widgets.DrawLine(ToScreen(plot, 0f, ny), ToScreen(plot, 1f, ny), GridColor, 1f);
			}

			Widgets.DrawLine(ToScreen(plot, 0f, 0f), ToScreen(plot, 0f, 1f), AxisColor, 1.5f);
			Widgets.DrawLine(ToScreen(plot, 0f, 0f), ToScreen(plot, 1f, 0f), AxisColor, 1.5f);

			Text.Font = GameFont.Tiny;
			GUI.color = Color.gray;
			Widgets.Label(new Rect(plot.x - padL, plot.yMax - 8f, padL - 2f, 14f), "0");
			Widgets.Label(new Rect(plot.x - padL, plot.y - 4f, padL - 2f, 14f), "1");
			GUI.color = new Color(0.78f, 0.78f, 0.78f);
			Widgets.Label(new Rect(plot.x, plot.yMax + 2f, plot.width, 16f), "EM.PoolSchematicAxisTime".Translate());
			Widgets.Label(new Rect(plotOuter.x + 4f, plotOuter.y + 2f, plot.width, 14f), "EM.PoolSchematicAxisLevel".Translate());
			GUI.color = Color.white;
			Text.Font = GameFont.Small;

			float capSetting = MilkCumSettings.lactationLevelCap;
			float capDur = MilkCumSettings.lactationLevelCapDurationMultiplier;
			int substeps = Mathf.Clamp(MilkCumSettings.inflowEventSubsteps, 1, 12);
			float burstTicks = MilkCumSettings.inflowEventBurstDurationTicks;

			float burstSharp = Mathf.Lerp(2.2f, 7.5f, substeps / 12f);
			float fillEase = 1f + burstTicks / 600f;
			float displayLMax = capSetting > 0.01f ? Mathf.Max(capSetting * 1.15f, 2f) : 2.5f;
			float refLForNorm = capSetting > 0.01f ? capSetting : displayLMax;
			float driveDenom = Mathf.Max(1e-4f, MilkCumSettings.GetEffectiveDrive(refLForNorm));

			float residualR = Mathf.Clamp01(MilkCumSettings.overflowResidualFlowFactor);
			float milkingDrain = 0.14f
				* Mathf.Clamp01(MilkCumSettings.defaultFlowMultiplierForHumanlike / 2f)
				* Mathf.Clamp(60f / Mathf.Max(15f, MilkCumSettings.milkingWorkTotalBase), 0.35f, 2.2f);

			const int segments = 80;
			var ptsFull = new List<Vector2>(segments + 1);
			var ptsL = new List<Vector2>(segments + 1);
			for (int i = 0; i <= segments; i++)
			{
				float t = i / (float)segments;
				float lRaw = displayLMax * (1f - Mathf.Exp(-burstSharp * t * fillEase));
				float lCapped = capSetting > 0.01f ? Mathf.Min(lRaw, capSetting) : lRaw;
				float drive = MilkCumSettings.GetEffectiveDrive(lCapped);
				float nLeff = Mathf.Clamp01(drive / driveDenom);

				float pressureEase = MilkCumSettings.enablePressureFactor
					? Mathf.Lerp(0.88f, 1.12f, MilkCumSettings.pressureFactorPc)
					: 1f;
				float fullness = Mathf.Clamp01((1f - Mathf.Exp(-2.2f * t * pressureEase * (0.35f + nLeff))) * (0.22f + 0.78f * nLeff));

				// 近满残余压力：r 越高，示意上更「贴高水位」（与动态 L/I 缩放无关，仅滑块 r）。
				fullness += residualR * 0.2f * t * fullness * (1.6f - 0.9f * fullness);
				fullness = Mathf.Clamp01(fullness);

				// 后段轻量挤出：流速↑、工作量基准↓ → 绿线略下探
				if (t > 0.48f)
				{
					float u = (t - 0.48f) / 0.52f;
					fullness = Mathf.Max(0f, fullness - milkingDrain * u * u);
				}

				ptsL.Add(ToScreen(plot, t, nLeff));
				ptsFull.Add(ToScreen(plot, t, fullness));
			}

			DashLine(ToScreen(plot, 0f, 1f), ToScreen(plot, 1f, 1f), CapacityColor, 2f, 5f, 4f);

			if (capSetting > 0.01f)
			{
				float tMark = 0.42f;
				Vector2 m0 = ToScreen(plot, tMark, 0f);
				Vector2 m1 = ToScreen(plot, tMark, 1f);
				Widgets.DrawLine(m0, m1, DoseMarkerColor, 1.2f);
				Text.Font = GameFont.Tiny;
				Widgets.Label(new Rect(m0.x - 28f, plot.y - 2f, 56f, 14f), "EM.PoolSchematicDoseHint".Translate());
				Text.Font = GameFont.Small;
			}

			DrawPolyline(ptsL, LeffColor, 2f);
			DrawPolyline(ptsFull, FullnessColor, 2.2f);

			float legendY = plotOuter.y + CurveBlockH + 4f;
			float lx = plot.x + 2f;
			Text.Font = GameFont.Tiny;
			GUI.color = new Color(0.8f, 0.8f, 0.8f);
			LegendRow(new Rect(lx, legendY, plot.width, 14f), FullnessColor, "EM.PoolSchematicLegendFullness".Translate());
			legendY += 15f;
			LegendRow(new Rect(lx, legendY, plot.width, 14f), LeffColor, "EM.PoolSchematicLegendDrive".Translate());
			legendY += 15f;
			LegendRow(new Rect(lx, legendY, plot.width, 14f), CapacityColor, "EM.PoolSchematicLegendCapacity".Translate());
			if (capSetting > 0.01f)
			{
				legendY += 15f;
				LegendRow(new Rect(lx, legendY, plot.width, 14f), DoseMarkerColor, "EM.PoolSchematicLegendStack".Translate());
			}

			legendY += 17f;
			string capNote = capSetting > 0.01f
				? "EM.PoolSchematicCapOnNote".Translate(capSetting.ToString("F1"), capDur.ToString("F1"))
				: "EM.PoolSchematicCapOffNote".Translate();
			Widgets.Label(new Rect(plot.x, legendY, plot.width, 40f), capNote);
			legendY += 38f;
			GUI.color = new Color(0.65f, 0.65f, 0.65f);
			Widgets.Label(new Rect(plot.x, legendY, plot.width, 28f), "EM.PoolSchematicFooterSeeTooltip".Translate());
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
		}
		finally
		{
			Text.Font = font;
			GUI.color = prevGui;
		}
	}

	private static string BuildSchematicTooltip()
	{
		var sb = new StringBuilder();
		sb.Append("EM.PoolSchematicTip".Translate());
		sb.Append("\n\n");
		sb.Append("EM.PoolSchematicReflectedTitle".Translate());
		sb.Append('\n');
		sb.Append("EM.PoolSchematicReflectedBullets".Translate());
		sb.Append("\n\n");
		sb.Append("EM.PoolSchematicExcludedTitle".Translate());
		sb.Append('\n');
		sb.Append("EM.PoolSchematicExcludedBullets".Translate());
		return sb.ToString();
	}

	private static Vector2 ToScreen(Rect plot, float nx, float ny)
		=> new(plot.x + nx * plot.width, plot.yMax - ny * plot.height);

	private static void DrawPolyline(List<Vector2> pts, Color color, float width)
	{
		for (int i = 1; i < pts.Count; i++)
			Widgets.DrawLine(pts[i - 1], pts[i], color, width);
	}

	private static void DashLine(Vector2 a, Vector2 b, Color color, float width, float dash, float gap)
	{
		Vector2 d = b - a;
		float len = d.magnitude;
		if (len < 0.01f) return;
		Vector2 dir = d / len;
		float traveled = 0f;
		bool draw = true;
		while (traveled < len - 0.01f)
		{
			float segLen = draw ? dash : gap;
			float t0 = traveled;
			float t1 = Mathf.Min(traveled + segLen, len);
			if (draw)
				Widgets.DrawLine(a + dir * t0, a + dir * t1, color, width);
			traveled = t1;
			draw = !draw;
		}
	}

	private static void LegendRow(Rect r, Color lineCol, string label)
	{
		Widgets.DrawLine(new Vector2(r.x, r.y + 5f), new Vector2(r.x + 18f, r.y + 5f), lineCol, 3f);
		Widgets.Label(new Rect(r.x + 22f, r.y, r.width - 24f, 16f), label);
	}
}
