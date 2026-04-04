using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MilkCum.Core.Utils;

/// <summary>逗号分隔的 defName 列表与「种族:倍率」泌乳药 ΔS 修正表解析。</summary>
public static class CommaSeparatedDefNames
{
	public static List<string> Parse(string text)
	{
		var list = new List<string>();
		if (string.IsNullOrWhiteSpace(text)) return list;
		foreach (string s in text.Split(','))
		{
			string t = s.Trim();
			if (!string.IsNullOrEmpty(t)) list.Add(t);
		}
		return list;
	}

	/// <summary>形如 <c>Human:1.2, SomeRace:0.8</c>；倍率钳制 0.1–3，与泌乳药种族 ΔS 修正读取逻辑一致。</summary>
	public static void ParseRaceDrugDeltaSText(string text, List<string> defNames, List<float> values)
	{
		defNames.Clear();
		values.Clear();
		if (string.IsNullOrWhiteSpace(text)) return;
		foreach (string part in text.Split(','))
		{
			string t = part.Trim();
			if (string.IsNullOrEmpty(t)) continue;
			int colon = t.IndexOf(':');
			if (colon <= 0) continue;
			string def = t.Substring(0, colon).Trim();
			string num = t.Substring(colon + 1).Trim();
			if (string.IsNullOrEmpty(def)) continue;
			if (!float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
				v = 1f;
			defNames.Add(def);
			values.Add(Mathf.Clamp(v, 0.1f, 3f));
		}
	}

	public static string FormatRaceDrugDeltaSText(IReadOnlyList<string> defNames, IReadOnlyList<float> values)
	{
		if (defNames == null || values == null) return "";
		int n = Math.Min(defNames.Count, values.Count);
		if (n == 0) return "";
		var parts = new string[n];
		for (int i = 0; i < n; i++)
			parts[i] = defNames[i] + ":" + values[i].ToString(CultureInfo.InvariantCulture);
		return string.Join(", ", parts);
	}
}
