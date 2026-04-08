using System;
using System.Collections.Generic;

namespace MilkCum.Core.Utils;

/// <summary>逗号分隔的 defName 列表解析（种族泌乳白/黑名单等）。</summary>
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
}
