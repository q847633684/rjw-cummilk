using System.Collections.Generic;
using System.Linq;
using MilkCum.Fluids.Shared.Comps;
using RimWorld;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>`MilkCumSettings` 的数据映射、默认产物与相关序列化分块。</summary>
internal partial class MilkCumSettings
{
	private static Dictionary<string, RaceMilkType> namesToProducts = new();
	private static Dictionary<string, MilkTag> productsToTags = new();
	public static List<Gene_MilkTypeData> genes = new();

	private static void ExposeDataMappings()
	{
		Scribe_Collections.Look(ref genes, "MC2.EM.Genes", LookMode.Deep);
		Scribe_Collections.Look(ref namesToProducts, "MC2.EM.NamesToProducts", LookMode.Value, LookMode.Deep);
		Scribe_Collections.Look(ref productsToTags, "MC2.EM.ProductsToTags", LookMode.Value, LookMode.Deep);
	}

	private static IEnumerable<ThingDef> GetMilkablePawns()
	{
		return DefDatabase<ThingDef>.AllDefs.Where(x => x.race != null && !x.IsCorpse)
			.OrderByDescending(def => def.race.Humanlike)
			.ThenByDescending(def => def.race.Animal)
			.ThenByDescending(def => def.race.IsMechanoid)
			.ThenBy(def => def.race.Insect)
			.ThenByDescending(def => def.modContentPack?.IsOfficialMod == true)
			.ThenBy(def => def.modContentPack?.Name ?? "")
			.ThenBy(def => def.defName);
	}

	private static IEnumerable<ThingDef> GetProductDefs()
	{
		return namesToProducts.Values
			.Where(product => product?.milkTypeDefName != null && DefDatabase<ThingDef>.GetNamedSilentFail(product.milkTypeDefName) != null)
			.Select(product => DefDatabase<ThingDef>.GetNamedSilentFail(product.milkTypeDefName))
			.Distinct();
	}

	private Dictionary<ThingDef, RaceMilkType> GetDefaultMilkProducts()
	{
		Dictionary<ThingDef, RaceMilkType> milkProducts = new();
		foreach (ThingDef def in pawnDefs)
		{
			RaceMilkType milkProduct = new();
			CompProperties_Milkable compMilkable = def.GetCompProperties<CompProperties_Milkable>();
			if (compMilkable?.milkDef != null)
			{
				milkProduct.milkTypeDefName = compMilkable.milkDef.defName;
				milkProduct.isMilkable = true;
				milkProducts.Add(def, milkProduct);
			}
		}
		return milkProducts;
	}

	private void UpdateEqualMilkableComp(ThingDef pawnDef)
	{
		CompProperties_Milkable compProperties = pawnDef.GetCompProperties<CompProperties_Milkable>();
		if (compProperties == null)
		{
			compProperties = new CompProperties_Milkable();
			pawnDef.comps.Add(compProperties);
		}
		if (!namesToProducts.ContainsKey(pawnDef.defName))
			namesToProducts.Add(pawnDef.defName, GetDefaultMilkProduct(pawnDef));
		compProperties.Set(namesToProducts[pawnDef.defName]);
	}

	internal static RaceMilkType GetDefaultMilkProduct(ThingDef def)
	{
		RaceMilkType milkProduct = new();
		if (defaultMilkProducts == null)
			defaultMilkProducts = new Dictionary<ThingDef, RaceMilkType>();
		if (defaultMilkProducts.ContainsKey(def))
		{
			milkProduct = defaultMilkProducts[def];
		}
		else
		{
			// 与上游一致：默认仅原版人类启用可挤奶；HAR/异种默认关闭，避免“自动全人型可挤奶”。
			if (def == ThingDefOf.Human)
			{
				milkProduct.isMilkable = true;
				milkProduct.milkTypeDefName = MilkCumDefOf.EM_HumanMilk.defName;
			}
			else
			{
				milkProduct.isMilkable = false;
			}
		}
		return milkProduct;
	}

	private void AddOrSetShowProducerComp(ThingDef milkDef)
	{
		CompProperties_ShowProducer compProperties = milkDef.GetCompProperties<CompProperties_ShowProducer>();
		if (compProperties == null)
		{
			compProperties = new CompProperties_ShowProducer();
			milkDef.comps.Add(compProperties);
		}
	}
}
