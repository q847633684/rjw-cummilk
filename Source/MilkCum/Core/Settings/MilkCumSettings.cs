using System.Collections.Generic;
using System.Linq;
using MilkCum.Fluids.Shared.Comps;
using MilkCum.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>涓撲笟绾?UI锛氭寜绯荤粺绫诲瀷鍒嗗眰銆傛牳蹇冩満鍒?/ 鍋ュ悍椋庨櫓 / 鏉冮檺瑙勫垯 / 鏁板€煎钩琛?/ 妯＄粍鑱斿姩 / 鏁版嵁绉嶆棌 / 璋冭瘯宸ュ叿锛堜粎 DevMode锛夈€</summary>
public enum MainTabIndex
{
	CoreSystems = 0,
	HealthRisk = 1,
	Permissions = 2,
	Balance = 3,
	Integrations = 4,
	DataRaces = 5,
	DevTools = 6
}

[StaticConstructorOnStartup]
internal class MilkCumSettings : ModSettings
{
	private static Dictionary<string, RaceMilkType> namesToProducts = new();
	private static Dictionary<string, MilkTag> productsToTags = new();
	/// <summary>鎸ゅザ娴侀€熷熀鍑嗭細baseFlowPerSecond = 60/鏈€硷紙姹犲崟浣?绉掞級銆傞粯璁?60 鈫?婊℃睜绾?1 鐡?绉掞紙鐜板疄鏃堕棿锛夛紱璋冨ぇ鍒欏彉鎱€</summary>
	public static float milkingWorkTotalBase = 60f;
	/// <summary>鎸夊閲忛噺鍖栵細鍚稿ザ鏈夋晥鏃堕棿闅忓杺濂惰€?MilkAmount 鐨勭郴鏁帮紝effectiveTime *= (1 + 鏈€济?MilkAmount-1))锛岄檺鍒跺湪 [0.5, 2]銆</summary>
	public static float breastfeedCapacityFactor = 0.1f;
	public static bool femaleAnimalAdultAlwaysLactating = false;
	public static bool showMechOptions = true;
	public static bool showColonistOptions = true;
	public static bool showSlaveOptions = true;
	public static bool showPrisonerOptions = true;
	public static bool showAnimalOptions = true;
	public static bool showMiscOptions = true;
	public static float nutritionToEnergyFactor = 100f;
	/// <summary>娉屼钩鐏屾弧鏈熼棿棰濆楗ラタ锛氭粦鍧?0鈥?00锛?50=1:1銆傞ケ椋熷害姣?150 tick 棰濆涓嬮檷 = flowPerDay脳(150/60000)脳(鏈€?150)銆</summary>
	public static int lactationExtraNutritionBasis = 150;
	/// <summary>鍥炵缉鍚告敹锛氭弧姹犲洖缂╂椂锛屾湭婧㈠嚭閮ㄥ垎瑙嗕负琚韩浣撳惛鏀讹紝鎸夋瘮渚嬭ˉ鍏呴ケ椋熷害锛?=鍏抽棴锛?=涓庝骇濂舵秷鑰?1:1 鎶樼畻銆</summary>
	public static bool reabsorbNutritionEnabled = true;
	/// <summary>鍥炵缉鍚告敹鏁堢巼锛?~1锛屽惛鏀剁殑姹犲崟浣嶆姌鎴愯惀鍏荤殑姣斾緥锛岄粯璁?0.5 閬垮厤婊℃睜鎸傛満杩囧己銆</summary>
	public static float reabsorbNutritionEfficiency = 0.5f;
	/// <summary>DevMode 涓斿嬀閫夋椂锛屾瘡 60 tick 杈撳嚭娉屼钩灏忎汉鐨勮惀鍏?涔虫睜/鍥炵缉/鍚稿ザ鏄庣粏鍒版棩蹇椼€</summary>
	public static bool lactationPoolTickLog = false;
	/// <summary>DevMode 涓斿嬀閫夋椂锛岃緭鍑哄惛濂?鎸ゅザ/鏈哄櫒浜уザ鍏ュ彛姹囨€绘棩蹇楋紙姣忔鎿嶄綔涓€缁勶級锛岀敤浜庡钩琛′笌 AI 璋冭瘯銆</summary>
	public static bool milkingActionLog = false;
	/// <summary>DevMode 鏃跺嬀閫夊垯杈撳嚭娉屼钩鍏抽敭璺緞鏃ュ織锛堝垎濞┿€佽繘姘淬€佺Щ闄ゆ硨涔崇瓑锛夛紱鍏抽棴鍙噺灏戝埛灞忥紝浠呯敤 PoolTickLog 鐪嬫槑缁嗐€</summary>
	public static bool lactationLog = true;
	/// <summary>鍕鹃€夋椂锛屾瘡娆″悆鑽繘姘达紙AddFromDrug锛夋椂杈撳嚭璋冭瘯鏃ュ織锛毼攕銆佽繘姘次擫銆佸墿浣欐椂闂村彉鍖栥€</summary>
	public static bool lactationDrugIntakeLog = false;
	/// <summary>DevMode 鏃惰緭鍑烘硨涔冲叧閿矾寰勬棩蹇楋紝渚夸簬鎺掓煡 L/姹?鑽墿/鍒嗗ī 琛屼负銆傚彈 lactationLog 寮€鍏虫帶鍒躲€</summary>
	public static void LactationLog(string message)
	{
		if (Verse.Prefs.DevMode && lactationLog && !string.IsNullOrEmpty(message))
			Verse.Log.Message("[MilkCum.Lactation] " + message);
	}
	/// <summary>浠呭綋 DevMode 涓?lactationPoolTickLog 涓?true 鏃惰緭鍑猴紝鐢ㄤ簬姣忔钀ュ吇/涔虫睜/鍥炵缉/鍚稿ザ鏄庣粏銆</summary>
	public static void PoolTickLog(string message)
	{
		if (Verse.Prefs.DevMode && lactationPoolTickLog && !string.IsNullOrEmpty(message))
			Verse.Log.Message("[MilkCum.Pool] " + message);
	}
	public static HumanlikeBreastfeed humanlikeBreastfeed = new();
	public static AnimalBreastfeed animalBreastfeed = new();
	public static MechanoidBreastfeed mechanoidBreastfeed = new();
	// 娉屼钩鏈熸剰璇?鎿嶇旱/绉诲姩澧炵泭锛氬紑鍏充笌鐧惧垎姣?(0~0.20 = 0%~20%)
	public static bool lactatingGainEnabled = true;
	public static float lactatingGainCapModPercent = 0.10f;
	// RJW 鑱斿姩锛堜粎褰?rim.job.world 婵€娲绘椂鐢熸晥锛?
	public static bool rjwBreastSizeEnabled = true;
	/// <summary>涔虫埧瀹归噺绯绘暟锛氬乏鍙充钩瀹归噺 = RJW Severity 脳 鏈郴鏁帮紝2=榛樿锛屼笌娉屼钩鏁堢巼绛夊彲璋冮」瀵瑰簲銆</summary>
	public static float rjwBreastCapacityCoefficient = 2f;
	public static bool rjwLustFromNursingEnabled = true;
	public static bool rjwSexNeedLactatingBonusEnabled = true;
	public static bool rjwSexSatisfactionAfterNursingEnabled = true;
	public static float rjwLactationFertilityFactor = 0.85f; // 娉屼钩鏈熸€€瀛曟鐜囦箻鏁?(0~1)
	public static bool rjwLactatingInSexDescriptionEnabled = true;
	/// <summary>3.2锛氭€ц涓哄悗涓烘硨涔冲弬涓庤€呭鍔犲皯閲忔睜杩涙按锛埼擫锛夛紝鍙€夈€</summary>
	public static bool rjwSexAddsLactationBoost = false;
	public static float rjwSexLactationBoostDeltaS = 0.15f;
	// 涔宠吅鐐?鍫靛锛氬崼鐢熻Е鍙戞槸鍚︿笌 Dubs Bad Hygiene 鑱斿姩锛堟湁 DBH 鏃剁敤 Hygiene 闇€姹傦紝鍚﹀垯鐢ㄦ埧闂存竻娲佸害锛?
	public static bool useDubsBadHygieneForMastitis = true;
	// 涔宠吅鐐庡彲閰嶇疆锛氭槸鍚﹀惎鐢ㄣ€佸熀鍑?MTB锛堝ぉ锛夈€佹弧姹犺繃涔呴闄╃郴鏁般€佸崼鐢熼闄╃郴鏁?
	// 鑰愬彈瀵规硨涔虫晥鐜囩殑褰卞搷锛氬叧闂垯 E_tol 鎭掍负 1锛涙寚鏁版帶鍒舵洸绾匡紙1=绾挎€э級
	// 寤鸿 13锛氭敹鎷负 MilkRiskSettings锛屼究浜庡簭鍒楀寲涓?UI 鍒嗙粍锛涘澶栦粛鐢ㄩ潤鎬佸睘鎬э紝瀛樻。鍏煎鏃?key
	private static MilkRiskSettings _risk = new MilkRiskSettings();
	private static MilkRiskSettings Risk => _risk ??= new MilkRiskSettings();
	public static bool allowMastitis { get => Risk.allowMastitis; set => Risk.allowMastitis = value; }
	public static float mastitisBaseMtbDays { get => Risk.mastitisBaseMtbDays; set => Risk.mastitisBaseMtbDays = value; }
	public static float overFullnessRiskMultiplier { get => Risk.overFullnessRiskMultiplier; set => Risk.overFullnessRiskMultiplier = value; }
	public static float hygieneRiskMultiplier { get => Risk.hygieneRiskMultiplier; set => Risk.hygieneRiskMultiplier = value; }
	public static bool allowToleranceAffectMilk { get => Risk.allowToleranceAffectMilk; set => Risk.allowToleranceAffectMilk = value; }
	public static float toleranceFlowImpactExponent { get => Risk.toleranceFlowImpactExponent; set => Risk.toleranceFlowImpactExponent = value; }
	// 鑰愬彈鍔ㄦ€?dE/dt = 渭路L 鈭?谓路E锛氬惎鐢ㄦ椂鐢?mod 缁存姢鐨?E 璁＄畻 E_tol锛堟祦閫?琛板噺锛夛紝鏇夸唬浠呯敤娓告垙鍐呰€愬彈涓ラ噸搴?t銆?
	public static bool enableToleranceDynamic = true;
	/// <summary>鑰愬彈绱Н鐜?渭锛堟瘡娓告垙鏃ワ級锛汱 楂樺垯 E 涓婂崌銆</summary>
	public static float toleranceDynamicMu = 0.03f;
	/// <summary>鑰愬彈琛板噺鐜?谓锛堟瘡娓告垙鏃ワ級锛汦 鑷劧鍥炶惤銆</summary>
	public static float toleranceDynamicNu = 0.08f;
	public static float mastitisMtbDaysMultiplierHumanlike { get => Risk.mastitisMtbDaysMultiplierHumanlike; set => Risk.mastitisMtbDaysMultiplierHumanlike = value; }
	public static float mastitisMtbDaysMultiplierAnimal { get => Risk.mastitisMtbDaysMultiplierAnimal; set => Risk.mastitisMtbDaysMultiplierAnimal = value; }
	// 婊℃睜婧㈠嚭鍦伴潰姹＄墿锛欴ef 鍚嶇О锛岀┖鎴栨棤鏁堟椂鍥為€€ Filth_Vomit
	public static string overflowFilthDefName = "Filth_Vomit";
	// 鍩哄噯娉屼钩鎸佺画澶╂暟锛堣嵂鐗╋級锛氫粎鐢ㄤ簬鏃?SeverityPerDay 鏃剁殑 RemainingDays 涓?GetDailyLactationDecay 鏄剧ず锛涗富娴佺▼宸茬敱 LactatingPatch 鐨?severityPerDay 鍐冲畾锛屼笉鍐嶆毚闇插埌 UI銆?
	public static float baselineMilkDurationDays = 5f;
	// 鍒嗗ī璇卞彂娉屼钩鎸佺画澶╂暟锛氫粎鐢ㄤ簬鏃?SeverityPerDay 鏃剁殑 RemainingDays 涓?GetDailyLactationDecay锛涗富娴佺▼鍚屼笂锛屼笉鍐嶆毚闇插埌 UI銆?
	public static float birthInducedMilkDurationDays = 30f;
	/// <summary>鍌钩绱犲崟鍓傚湪 XML 涓鑰愬彈 Hediff 鐨?Severity 澧為噺锛堜笌 Lactating 鍚屽墏鍙犲姞涓€鑷达紝榛樿 0.044锛夛紱鏀?XML 鏃堕渶鍚屾銆</summary>
	public static float ProlactinToleranceGainPerDose = 0.044f;

	/// <summary>鑽墿娉屼钩琛板噺鐢ㄦ湁鏁?B_T锛氱敱 baselineMilkDurationDays 鍙嶆帹锛屼娇鍗曟鍓傞噺锛圠鈮?.5銆丒=1锛夋椂鍓╀綑澶╂暟 鈮?鍩哄噯澶╂暟銆侱=0.5/baseline 鈬?B_T_eff=1/(0.5/baseline鈭択脳0.5)銆</summary>
	public static float GetEffectiveBaseValueTForDecay()
	{
		float baseline = baselineMilkDurationDays;
		if (baseline <= 0f) return PoolModelConstants.BaseValueT;
		float denom = 0.5f / baseline - PoolModelConstants.NegativeFeedbackK * 0.5f;
		if (denom <= 0.01f) return 100f;
		return 1f / denom;
	}
	/// <summary>鍒嗗ī娉屼钩琛板噺鐢ㄦ湁鏁?B_T锛氱敱 birthInducedMilkDurationDays 鍙嶆帹锛屽叕寮忓悓鑽墿銆</summary>
	public static float GetEffectiveBaseValueTForDecayBirth()
	{
		float baseline = birthInducedMilkDurationDays;
		if (baseline <= 0f) return PoolModelConstants.BaseValueT;
		float denom = 0.5f / baseline - PoolModelConstants.NegativeFeedbackK * 0.5f;
		if (denom <= 0.01f) return 100f;
		return 1f / denom;
	}
	/// <summary>甯︿笅闄?Logistic锛歠(P)=f_min+(1鈭抐_min)脳1/(1+exp(k脳(P鈭扨c)))锛孭 澶ф椂骞虫粦闄嶉€燂紝鏈€浣?f_min 姘镐笉褰掗浂銆傞粯璁?k=6銆丳c=0.9銆乫_min=0.02锛屽ザ閲忚揪 90% 鎵嶅紑濮嬪線涓嬪帇銆</summary>
	/// <param name="P">璇ヤ晶婊″害/璇ヤ晶鎾戝ぇ瀹归噺锛?锝?锛堝彲鐣ュぇ浜?1 鏃朵粛鎸夊叕寮忕畻锛夈€</param>
	public static float GetPressureFactor(float P)
	{
		if (!enablePressureFactor || P <= 0f)
			return 1f;

		float k = Mathf.Clamp(pressureFactorB, 0.5f, 80f);
		float pc = Mathf.Clamp(pressureFactorPc, 0.3f, 1f);
		float fMin = Mathf.Clamp01(pressureFactorMin);

		// 甯︿笅闄?Logistic锛氭渶浣?f_min锛孭 瓒婂ぇ瓒婃帴杩?f_min
		float logistic = 1f / (1f + Mathf.Exp(k * (P - pc)));
		return fMin + (1f - fMin) * logistic;
	}
	/// <summary>鍥涘眰妯″瀷锛堥樁娈?锛夛細鏈夋晥椹卞姩鍔?D_eff = L路H(L)銆侶(L)=1鈭抏xp(鈭抋路L)锛涜嫢 L_ref>0 鍒欑敤鍙傝€冨€煎綊涓€锛欴_eff = L路(1鈭抏^{-aL})/(1鈭抏^{-aL_ref})锛屽熬鏈熸洿骞虫粦銆</summary>
	public static float GetEffectiveDrive(float L)
	{
		if (!enableHormoneSaturation || L <= 0f) return L;
		float a = Mathf.Clamp(hormoneSaturationA, 0.1f, 3f);
		float H = 1f - Mathf.Exp(-a * L);
		float lRef = Mathf.Max(0f, hormoneSaturationLRef);
		if (lRef >= 0.01f)
		{
			float denom = 1f - Mathf.Exp(-a * lRef);
			if (denom >= 1E-6f)
				H /= denom;
		}
		return L * H;
	}
	// 鎸ゅザ宸ヤ綔锛氭槸鍚︿紭鍏堥€夋嫨婊″害鏇撮珮鐨勭洰鏍囷紙娈栨皯鑰呬細鍏堟尋鏇存弧鐨勶級
	public static bool aiPreferHighFullnessTargets = true;
	// 绉嶆棌瑕嗙洊锛氱櫧鍚嶅崟锛坉efName 鍦ㄦ鍒楄〃涓涓哄彲浜уザ锛夈€侀粦鍚嶅崟锛坉efName 鍦ㄦ鍒楄〃涓涓轰笉鍙骇濂讹級
	public static List<string> raceCanAlwaysLactate = new();
	public static List<string> raceCannotLactate = new();
	// 浜哄舰绉嶆棌榛樿娴侀€熷€嶇巼锛? = 鍗曟鍓傞噺绾?1 鏃ョ亴婊★紱涓?RJW/绉嶆棌 mod 骞宠　鏃朵篃鍙皟锛?
	public static float defaultFlowMultiplierForHumanlike = 2f;
	// 鍥涘眰妯″瀷锛堥樁娈?锛夛細鍘嬪姏杞姂鍒躲€傚惎鐢ㄦ椂娴侀€熶箻 PressureFactor(P)锛涘甫涓嬮檺 Logistic锛岄粯璁?Pc=0.9銆乲=6銆乫_min=0.02锛屽ザ閲忚揪 90% 鎵嶅紑濮嬪線涓嬪帇銆?
	public static bool enablePressureFactor = true;
	public static float pressureFactorPc = 0.9f;
	public static float pressureFactorB = 6f;
	/// <summary>鍘嬪姏鏇茬嚎涓嬮檺 f_min锛氭弧姹犳椂鐢熶骇鍊嶇巼涓嶄綆浜庢鍊硷紝姘镐笉褰掗浂銆傛帹鑽?0.02锝?.15銆</summary>
	public static float pressureFactorMin = 0.02f;
	// 鍥涘眰妯″瀷锛堥樁娈?锛夛細鍠蜂钩鍙嶅皠 R銆傚惎鐢ㄦ椂娴侀€熶箻 R锛汻 姣?60 tick 鎸囨暟琛板噺锛屾尋濂?鍚稿ザ鏃跺崌楂樸€?
	public static bool enableLetdownReflex = true;
	/// <summary>鍠蜂钩鍙嶅皠琛板噺鐜?位锛堟瘡鍒嗛挓锛夛紱R_new = R 脳 exp(-位脳螖t)銆</summary>
	public static float letdownReflexDecayLambda = 0.03f;
	/// <summary>鎸ゅザ/鍚稿ザ鏃?R 鐨勫閲?螖R锛孯 鍔犱笂鍚?Clamp 鑷?1銆傝澶т竴浜涳紙濡?1锛夊彲涓€娆″埡婵€鍗虫弧 R銆</summary>
	public static float letdownReflexStimulusDeltaR = 0.45f;
	/// <summary>鍠蜂钩鍙嶅皠鍔犳垚鍊嶇巼锛氳繘姘存祦閫熷€嶇巼 = 1 + R脳(鏈€尖垝1)锛孯=1 鏃朵负鏈€煎€嶏紙寤鸿 1.5~2.5锛夛紝R=0 鏃朵负 1 鍊嶏紱璁句负 1 鍗虫棤鍔犳垚銆</summary>
	public static float letdownReflexBoostMultiplier = 2f;
	// 鍥涘眰妯″瀷锛堥樁娈?锛夛細鐐庣棁 I(t)銆傚惎鐢ㄦ椂姣?60 tick 鏇存柊 I锛汭>I_crit 瑙﹀彂涔宠吅鐐庯紱L 琛板噺鍔?畏路I銆?
	public static bool enableInflammationModel = true;
	public static float inflammationAlpha = 0.1f;
	public static float inflammationBeta = 0.15f;
	public static float inflammationGamma = 0.2f;
	public static float inflammationRho = 0.05f;
	public static float inflammationCrit = 1f;
	/// <summary>鐐庣棁瀵?L 琛板噺鐨勬姂鍒跺洜瀛?畏锛欴 += 畏路I銆</summary>
	public static float lactationDecayInflammationEta = 0.1f;
	// 鍥涘眰妯″瀷锛堥樁娈?锛夛細鎸ゅザ/鍚稿ザ鏃?L 寰箙鍒烘縺锛堝甫涓婇檺锛岄槻鏃犻檺寰幆锛夈€?
	public static float milkingLStimulusPerEvent = 0.03f;
	public static float milkingLStimulusCapPerEvent = 0.05f;
	public static float milkingLStimulusCapPerDay = 0.2f;
	// 鍥涘眰妯″瀷锛堥樁娈?锛夛細鎸ゅザ/鍚稿ザ鏃?L 寰箙鍒烘縺锛堝甫涓婇檺锛夛紝浠?enableInflammationModel 鏃剁敓鏁堛€?
	// 鍥涘眰妯″瀷锛堥樁娈?锛夛細婵€绱犻ケ鍜?H(L)=1鈭抏xp(鈭抋路L)锛孌_eff=L路H(L)銆傚惎鐢ㄦ椂娴侀€熺敱 D_eff 椹卞姩锛屼綆 L 浣庝骇銆侀珮 L 楗卞拰銆?
	public static bool enableHormoneSaturation = true;
	/// <summary>H(L) 楗卞拰绯绘暟 a锛涘缓璁?0.5锝?.5銆</summary>
	public static float hormoneSaturationA = 1f;
	/// <summary>鍙傝€?L 褰掍竴锛欴_eff = L路(1鈭抏^{-aL})/(1鈭抏^{-aL_ref})锛屼娇灏炬湡鏇村钩婊戯紱鈮? 鏃朵笉褰掍竴銆</summary>
	public static float hormoneSaturationLRef = 1f;
	// 鍥涘眰妯″瀷锛堥樁娈?.2锛夛細缁勭粐閫傚簲銆傞暱鏈熼珮 P 鎵╁銆侀暱鏈熶綆 P 鍥炵缉锛沝F_max/dt = 胃路max(P鈭?.85,0) 鈭?蠅路(1鈭扨)锛屾瘡 60 tick 鏇存柊锛屽彔鍔犲埌鍩虹瀹归噺涓娿€?
	public static bool enableTissueAdaptation = true;
	/// <summary>鎵╁鐜?胃锛堟瘡娓告垙鏃ワ級锛汸 澶т簬 0.85 鏃跺閲忓鍔犮€</summary>
	public static float adaptationTheta = 0.002f;
	/// <summary>鍥炵缉鐜?蠅锛堟瘡娓告垙鏃ワ級锛汸 灏忎簬 1 鏃跺閲忓噺灏戙€</summary>
	public static float adaptationOmega = 0.001f;
	/// <summary>閫傚簲瀹归噺涓婇檺锛氫笉瓒呰繃鍩虹瀹归噺鐨勬姣斾緥锛堝 0.2=20%锛夈€</summary>
	public static float adaptationCapMaxRatio = 0.2f;
	// 鍥涘眰妯″瀷锛堥樁娈?.3锛夛細涔虫眮璐ㄩ噺 MilkQuality = f(Hunger, I)銆傚惎鐢ㄦ椂锛氳川閲忛珮鈫掍钩鑵虹値闃堝€兼彁楂橈紱鍙€夊湪 UI 鏄剧ず銆傞粯璁ゅ叧闂紝寰呭姙锛氫骇鍑哄ザ鐗╁搧鐨?QualityCategory銆?
	public static bool enableMilkQuality = false;
	/// <summary>鐐庣棁瀵硅川閲忕殑鎶戝埗绯绘暟锛涜川閲?鈭?(1 鈭?鏈€济桰)锛孖 澶у垯璐ㄩ噺闄嶃€</summary>
	public static float milkQualityInflammationWeight = 0.5f;
	/// <summary>璐ㄩ噺瀵逛钩鑵虹値闃堝€肩殑淇濇姢绯绘暟锛涙湁鏁?I_crit = I_crit脳(1 + 鏈€济桵ilkQuality)銆</summary>
	public static float milkQualityProtectionFactor = 0.5f;
	// 3.3 婊℃睜浜嬩欢锛氭弧姹犺繃涔咃紙绾?1 澶╋級鏃舵槸鍚﹀彂淇℃彁閱?
	public static bool enableFullPoolLetter = true;
	// 3.3 鍔ㄧ墿宸紓鍖栵細绉嶆棌 defName 瀵瑰簲鑽墿杩涙按鍊嶇巼锛堟湭鍒楀嚭鐨勭鏃忎负 1锛夈€備笌鍙傛暟鑱斿姩琛ㄤ竴鑷淬€?
	public static List<string> raceDrugDeltaSMultiplierDefNames = new();
	public static List<float> raceDrugDeltaSMultiplierValues = new();
	// 精液/Leak 设置（统一到本 Mod）。存档键 EM.Cum.* / EM.Cum.Leak.*。
	public static bool Cum_EnableCumflation = true;
	public static float Cum_GlobalCumflationModifier = 1.0f;
	public static bool Cum_EnableStuffing = true;
	public static float Cum_GlobalStuffingModifier = 1.0f;
	public static bool Cum_EnableBukkake = true;
	public static float Cum_GlobalBukkakeModifier = 1.0f;
	public static bool Cum_EnableFluidGatheringWhileCleaning = true;
	public static float Cum_MaxGatheringCheckDistance = 15.0f;
	public static bool Cum_EnableProgressingConsumptionThoughts = true;
	public static bool Cum_EnableOscillationMechanics = true;
	public static bool Cum_EnableOscillationMechanicsForAnimals = false;
	public static bool Cum_EnableDebugLogging = false;
	public static bool CumLeak_EnableFilthGeneration = true;
	public static bool CumLeak_EnableAutoDeflateBucket = false;
	public static bool CumLeak_EnableAutoDeflateClean = false;
	public static bool CumLeak_EnableAutoDeflateDirty = false;
	public static bool CumLeak_EnablePrivacy = true;
	public static float CumLeak_AutoDeflateMinSeverity = 0.4f;
	public static float CumLeak_AutoDeflateMaxDistance = 100f;
	public static float CumLeak_LeakMult = 5.0f;
	public static float CumLeak_LeakRate = 1.0f;
	public static float CumLeak_DeflateMult = 5.0f;
	public static float CumLeak_DeflateRate = 1.0f;
	public static List<Gene_MilkTypeData> genes = new();
	public static MilkSettings colonistSetting = new();
	public static MilkSettings slaveSetting = new();
	public static MilkSettings prisonerSetting = new();
	public static MilkSettings animalSetting = new();
	public static MilkSettings mechSetting = new();
	public static MilkSettings entitySetting = new();
	private int mainTabIndex = 0;
	private int subTabIndex = 0;
	private static readonly float unitSize = 32f;
	public static IEnumerable<ThingDef> pawnDefs;
	public static IEnumerable<ThingDef> productDefs;
	public static Dictionary<ThingDef, RaceMilkType> defaultMilkProducts;
	private static Widget_MilkableTable milkableTable;
	private static Widget_MilkTagsTable milkTagsTable;
	private static Widget_AdvancedSettings advancedSettings;
	private static Widget_BreastfeedSettings breastfeedSettings;
	private static Widget_GeneSetting geneSetting;
	private static Widget_DefaultSetting defaultSettingWidget;
	private static Widget_CumpilationSettings cumpilationSettings;
	private static Widget_RaceOverrides raceOverridesWidget;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref milkingWorkTotalBase, "EM.MilkingWorkTotalBase", 60f);
		Scribe_Values.Look(ref breastfeedCapacityFactor, "EM.BreastfeedCapacityFactor", 0.1f);
		Scribe_Values.Look(ref femaleAnimalAdultAlwaysLactating, "EM.FemaleAnimalAdultAlwaysLactating", false);
		Scribe_Values.Look(ref showMechOptions, "EM.ShowMechOptions", true);
		Scribe_Values.Look(ref showColonistOptions, "EM.ShowColonistOptions", true);
		Scribe_Values.Look(ref showSlaveOptions, "EM.ShowSlaveOptions", true);
		Scribe_Values.Look(ref showPrisonerOptions, "EM.ShowPrisonerOptions", true);
		Scribe_Values.Look(ref showAnimalOptions, "EM.ShowAnimalOptions", true);
		Scribe_Values.Look(ref showMiscOptions, "EM.ShowMiscOptions", true);
		Scribe_Values.Look(ref nutritionToEnergyFactor, "EM.NutritionToEnergyFactor", 100f);
		Scribe_Values.Look(ref lactationExtraNutritionBasis, "EM.LactationExtraNutritionFactor", 150);
		if (Scribe.mode == LoadSaveMode.LoadingVars && lactationExtraNutritionBasis is >= 1 and < 150)
			lactationExtraNutritionBasis = 150; // 鏃у瓨妗?float 1f 琚鎴?1锛岃涓?150
		Scribe_Values.Look(ref reabsorbNutritionEnabled, "EM.ReabsorbNutritionEnabled", true);
		Scribe_Values.Look(ref reabsorbNutritionEfficiency, "EM.ReabsorbNutritionEfficiency", 0.5f);
		Scribe_Values.Look(ref lactatingGainEnabled, "EM.LactatingGainEnabled", true);
		Scribe_Values.Look(ref lactatingGainCapModPercent, "EM.LactatingGainCapModPercent", 0.10f);
		Scribe_Values.Look(ref rjwBreastSizeEnabled, "EM.RjwBreastSizeEnabled", true);
		Scribe_Values.Look(ref rjwBreastCapacityCoefficient, "EM.RjwBreastCapacityCoefficient", 2f);
		Scribe_Values.Look(ref rjwLustFromNursingEnabled, "EM.RjwLustFromNursingEnabled", true);
		Scribe_Values.Look(ref rjwSexNeedLactatingBonusEnabled, "EM.RjwSexNeedLactatingBonusEnabled", true);
		Scribe_Values.Look(ref rjwSexSatisfactionAfterNursingEnabled, "EM.RjwSexSatisfactionAfterNursingEnabled", true);
		Scribe_Values.Look(ref rjwLactationFertilityFactor, "EM.RjwLactationFertilityFactor", 0.85f);
		Scribe_Values.Look(ref rjwLactatingInSexDescriptionEnabled, "EM.RjwLactatingInSexDescriptionEnabled", true);
		Scribe_Values.Look(ref rjwSexAddsLactationBoost, "EM.RjwSexAddsLactationBoost", false);
		Scribe_Values.Look(ref rjwSexLactationBoostDeltaS, "EM.RjwSexLactationBoostDeltaS", 0.15f);
		Scribe_Values.Look(ref useDubsBadHygieneForMastitis, "EM.UseDubsBadHygieneForMastitis", true);
		Scribe_Deep.Look(ref _risk, "EM.MilkRiskSettings");
		if (_risk == null) _risk = new MilkRiskSettings();
		Scribe_Values.Look(ref _risk.allowMastitis, "EM.AllowMastitis", true);
		Scribe_Values.Look(ref _risk.mastitisBaseMtbDays, "EM.MastitisBaseMtbDays", 1.5f);
		Scribe_Values.Look(ref _risk.overFullnessRiskMultiplier, "EM.OverFullnessRiskMultiplier", 1.5f);
		Scribe_Values.Look(ref _risk.hygieneRiskMultiplier, "EM.HygieneRiskMultiplier", 1f);
		Scribe_Values.Look(ref _risk.allowToleranceAffectMilk, "EM.AllowToleranceAffectMilk", true);
		Scribe_Values.Look(ref _risk.toleranceFlowImpactExponent, "EM.ToleranceFlowImpactExponent", 1f);
		Scribe_Values.Look(ref enableToleranceDynamic, "EM.EnableToleranceDynamic", true);
		Scribe_Values.Look(ref toleranceDynamicMu, "EM.ToleranceDynamicMu", 0.03f);
		Scribe_Values.Look(ref toleranceDynamicNu, "EM.ToleranceDynamicNu", 0.08f);
		Scribe_Values.Look(ref _risk.mastitisMtbDaysMultiplierHumanlike, "EM.MastitisMtbDaysMultiplierHumanlike", 1f);
		Scribe_Values.Look(ref _risk.mastitisMtbDaysMultiplierAnimal, "EM.MastitisMtbDaysMultiplierAnimal", 1f);
		Scribe_Values.Look(ref overflowFilthDefName, "EM.OverflowFilthDefName", "Filth_Vomit");
		Scribe_Values.Look(ref baselineMilkDurationDays, "EM.BaselineMilkDurationDays", 5f);
		Scribe_Values.Look(ref birthInducedMilkDurationDays, "EM.BirthInducedMilkDurationDays", 30f);
		Scribe_Values.Look(ref aiPreferHighFullnessTargets, "EM.AiPreferHighFullnessTargets", true);
		Scribe_Collections.Look(ref raceCanAlwaysLactate, "EM.RaceCanAlwaysLactate", LookMode.Value);
		Scribe_Collections.Look(ref raceCannotLactate, "EM.RaceCannotLactate", LookMode.Value);
		Scribe_Values.Look(ref defaultFlowMultiplierForHumanlike, "EM.DefaultFlowMultiplierForHumanlike", 2f);
		Scribe_Values.Look(ref enablePressureFactor, "EM.EnablePressureFactor", true);
		Scribe_Values.Look(ref pressureFactorPc, "EM.PressureFactorPc", 0.9f);
		Scribe_Values.Look(ref pressureFactorB, "EM.PressureFactorB", 6f);
		Scribe_Values.Look(ref pressureFactorMin, "EM.PressureFactorMin", 0.02f);
		Scribe_Values.Look(ref enableLetdownReflex, "EM.EnableLetdownReflex", true);
		Scribe_Values.Look(ref letdownReflexDecayLambda, "EM.LetdownReflexDecayLambda", 0.03f);
		Scribe_Values.Look(ref letdownReflexStimulusDeltaR, "EM.LetdownReflexStimulusDeltaR", 0.45f);
		Scribe_Values.Look(ref letdownReflexBoostMultiplier, "EM.LetdownReflexBoostMultiplier", 2f);
		Scribe_Values.Look(ref enableInflammationModel, "EM.EnableInflammationModel", true);
		Scribe_Values.Look(ref inflammationAlpha, "EM.InflammationAlpha", 0.1f);
		Scribe_Values.Look(ref inflammationBeta, "EM.InflammationBeta", 0.15f);
		Scribe_Values.Look(ref inflammationGamma, "EM.InflammationGamma", 0.2f);
		Scribe_Values.Look(ref inflammationRho, "EM.InflammationRho", 0.05f);
		Scribe_Values.Look(ref inflammationCrit, "EM.InflammationCrit", 1f);
		Scribe_Values.Look(ref lactationDecayInflammationEta, "EM.LactationDecayInflammationEta", 0.1f);
		Scribe_Values.Look(ref milkingLStimulusPerEvent, "EM.MilkingLStimulusPerEvent", 0.03f);
		Scribe_Values.Look(ref milkingLStimulusCapPerEvent, "EM.MilkingLStimulusCapPerEvent", 0.05f);
		Scribe_Values.Look(ref milkingLStimulusCapPerDay, "EM.MilkingLStimulusCapPerDay", 0.2f);
		Scribe_Values.Look(ref enableHormoneSaturation, "EM.EnableHormoneSaturation", true);
		Scribe_Values.Look(ref hormoneSaturationA, "EM.HormoneSaturationA", 1f);
		Scribe_Values.Look(ref hormoneSaturationLRef, "EM.HormoneSaturationLRef", 1f);
		Scribe_Values.Look(ref enableTissueAdaptation, "EM.EnableTissueAdaptation", true);
		Scribe_Values.Look(ref adaptationTheta, "EM.AdaptationTheta", 0.002f);
		Scribe_Values.Look(ref adaptationOmega, "EM.AdaptationOmega", 0.001f);
		Scribe_Values.Look(ref adaptationCapMaxRatio, "EM.AdaptationCapMaxRatio", 0.2f);
		Scribe_Values.Look(ref enableMilkQuality, "EM.EnableMilkQuality", false);
		Scribe_Values.Look(ref milkQualityInflammationWeight, "EM.MilkQualityInflammationWeight", 0.5f);
		Scribe_Values.Look(ref milkQualityProtectionFactor, "EM.MilkQualityProtectionFactor", 0.5f);
		Scribe_Values.Look(ref enableFullPoolLetter, "EM.EnableFullPoolLetter", true);
		Scribe_Collections.Look(ref raceDrugDeltaSMultiplierDefNames, "EM.RaceDrugDeltaSMultiplierDefNames", LookMode.Value);
		Scribe_Collections.Look(ref raceDrugDeltaSMultiplierValues, "EM.RaceDrugDeltaSMultiplierValues", LookMode.Value);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			raceDrugDeltaSMultiplierDefNames ??= new List<string>();
			raceDrugDeltaSMultiplierValues ??= new List<float>();
		}
		Scribe_Values.Look(ref Cum_EnableCumflation, "EM.Cum.EnableCumflation", true);
		Scribe_Values.Look(ref Cum_GlobalCumflationModifier, "EM.Cum.GlobalCumflationModifier", 1.0f);
		Scribe_Values.Look(ref Cum_EnableStuffing, "EM.Cum.EnableStuffing", true);
		Scribe_Values.Look(ref Cum_GlobalStuffingModifier, "EM.Cum.GlobalStuffingModifier", 1.0f);
		Scribe_Values.Look(ref Cum_EnableBukkake, "EM.Cum.EnableBukkake", true);
		Scribe_Values.Look(ref Cum_GlobalBukkakeModifier, "EM.Cum.GlobalBukkakeModifier", 1.0f);
		Scribe_Values.Look(ref Cum_EnableFluidGatheringWhileCleaning, "EM.Cum.EnableFluidGatheringWhileCleaning", true);
		Scribe_Values.Look(ref Cum_MaxGatheringCheckDistance, "EM.Cum.MaxGatheringCheckDistance", 15.0f);
		Scribe_Values.Look(ref Cum_EnableProgressingConsumptionThoughts, "EM.Cum.EnableProgressingConsumptionThoughts", true);
		Scribe_Values.Look(ref Cum_EnableOscillationMechanics, "EM.Cum.EnableOscillationMechanics", true);
		Scribe_Values.Look(ref Cum_EnableOscillationMechanicsForAnimals, "EM.Cum.EnableOscillationMechanicsForAnimals", false);
		Scribe_Values.Look(ref Cum_EnableDebugLogging, "EM.Cum.EnableDebugLogging", false);
		Scribe_Values.Look(ref lactationPoolTickLog, "EM.LactationPoolTickLog", false);
		Scribe_Values.Look(ref lactationLog, "EM.LactationLog", true);
		Scribe_Values.Look(ref lactationDrugIntakeLog, "EM.LactationDrugIntakeLog", false);
		Scribe_Values.Look(ref CumLeak_EnableFilthGeneration, "EM.Cum.Leak.EnableFilthGeneration", true);
		Scribe_Values.Look(ref CumLeak_EnableAutoDeflateBucket, "EM.Cum.Leak.EnableAutoDeflateBucket", false);
		Scribe_Values.Look(ref CumLeak_EnableAutoDeflateClean, "EM.Cum.Leak.EnableAutoDeflateClean", false);
		Scribe_Values.Look(ref CumLeak_EnableAutoDeflateDirty, "EM.Cum.Leak.EnableAutoDeflateDirty", false);
		Scribe_Values.Look(ref CumLeak_EnablePrivacy, "EM.Cum.Leak.EnablePrivacy", true);
		Scribe_Values.Look(ref CumLeak_AutoDeflateMinSeverity, "EM.Cum.Leak.AutoDeflateMinSeverity", 0.4f);
		Scribe_Values.Look(ref CumLeak_AutoDeflateMaxDistance, "EM.Cum.Leak.AutoDeflateMaxDistance", 100f);
		Scribe_Values.Look(ref CumLeak_LeakMult, "EM.Cum.Leak.LeakMult", 5.0f);
		Scribe_Values.Look(ref CumLeak_LeakRate, "EM.Cum.Leak.LeakRate", 1.0f);
		Scribe_Values.Look(ref CumLeak_DeflateMult, "EM.Cum.Leak.DeflateMult", 5.0f);
		Scribe_Values.Look(ref CumLeak_DeflateRate, "EM.Cum.Leak.DeflateRate", 1.0f);
		Scribe_Deep.Look(ref humanlikeBreastfeed, "EM.HumanlikeBreastfeed");
		Scribe_Deep.Look(ref animalBreastfeed, "EM.AnimalBreastfeed");
		Scribe_Deep.Look(ref mechanoidBreastfeed, "EM.MechanoidBreastfeed");
		Scribe_Collections.Look(ref genes, "EM.Genes", LookMode.Deep);
		Scribe_Collections.Look(ref namesToProducts, "EM.NamesToProducts", LookMode.Value, LookMode.Deep);
		Scribe_Collections.Look(ref productsToTags, "EM.ProductsToTags", LookMode.Value, LookMode.Deep);
		Scribe_Deep.Look(ref colonistSetting, "EM.ColonistSetting");
		Scribe_Deep.Look(ref slaveSetting, "EM.SlaveSetting");
		Scribe_Deep.Look(ref prisonerSetting, "EM.PrisonerSetting");
		Scribe_Deep.Look(ref animalSetting, "EM.AnimalSetting");
		Scribe_Deep.Look(ref mechSetting, "EM.MechSetting");
		Scribe_Deep.Look(ref entitySetting, "EM.EntitySetting");
		genes ??= new List<Gene_MilkTypeData>();
		namesToProducts ??= new Dictionary<string, RaceMilkType>();
		raceCanAlwaysLactate ??= new List<string>();
		raceCannotLactate ??= new List<string>();
		// Initialize widgets
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			pawnDefs ??= GetMilkablePawns();
			productDefs ??= GetProductDefs();
			humanlikeBreastfeed ??= new HumanlikeBreastfeed();
			animalBreastfeed ??= new AnimalBreastfeed();
			mechanoidBreastfeed ??= new MechanoidBreastfeed();
			milkableTable = new Widget_MilkableTable(namesToProducts);
			milkTagsTable = new Widget_MilkTagsTable(namesToProducts, productsToTags);
			advancedSettings = new Widget_AdvancedSettings();
			breastfeedSettings = new Widget_BreastfeedSettings(humanlikeBreastfeed, animalBreastfeed, mechanoidBreastfeed);
			geneSetting = new Widget_GeneSetting(genes);
			colonistSetting ??= new MilkSettings();
			slaveSetting ??= new MilkSettings();
			prisonerSetting ??= new MilkSettings();
			animalSetting ??= new MilkSettings();
			mechSetting ??= new MilkSettings();
			entitySetting ??= new MilkSettings();
		}
	}

	/// <summary>纭繚 Scribe_Deep 鍙嶅簭鍒楀寲鐨勫璞＄被鍨嬫纭紝閬垮厤鏃у瓨妗ｆ垨绫诲瀷鍙樻洿瀵艰嚧 InvalidCastException銆</summary>
	private static void EnsureScribeDeepTypes()
	{
		if (_risk == null || _risk.GetType() != typeof(MilkRiskSettings))
			_risk = new MilkRiskSettings();
		if (humanlikeBreastfeed == null || humanlikeBreastfeed.GetType() != typeof(HumanlikeBreastfeed))
			humanlikeBreastfeed = new HumanlikeBreastfeed();
		if (animalBreastfeed == null || animalBreastfeed.GetType() != typeof(AnimalBreastfeed))
			animalBreastfeed = new AnimalBreastfeed();
		if (mechanoidBreastfeed == null || mechanoidBreastfeed.GetType() != typeof(MechanoidBreastfeed))
			mechanoidBreastfeed = new MechanoidBreastfeed();
		if (colonistSetting == null || colonistSetting.GetType() != typeof(MilkSettings))
			colonistSetting = new MilkSettings();
		if (slaveSetting == null || slaveSetting.GetType() != typeof(MilkSettings))
			slaveSetting = new MilkSettings();
		if (prisonerSetting == null || prisonerSetting.GetType() != typeof(MilkSettings))
			prisonerSetting = new MilkSettings();
		if (animalSetting == null || animalSetting.GetType() != typeof(MilkSettings))
			animalSetting = new MilkSettings();
		if (mechSetting == null || mechSetting.GetType() != typeof(MilkSettings))
			mechSetting = new MilkSettings();
		if (entitySetting == null || entitySetting.GetType() != typeof(MilkSettings))
			entitySetting = new MilkSettings();
		genes ??= new List<Gene_MilkTypeData>();
	}

	public void DoWindowContents(Rect inRect)
	{
		// 闃叉鏃у瓨妗?閿欒鍙嶅簭鍒楀寲瀵艰嚧绫诲瀷涓嶄竴鑷村紩鍙?InvalidCastException
		EnsureScribeDeepTypes();
		inRect.yMin += unitSize;
		// 浠庝富鑿滃崟鎵撳紑璁剧疆鏃?PostLoadInit 鏈墽琛岋紝闇€鎯版€у垵濮嬪寲浠ュ厤 NRE
		pawnDefs ??= GetMilkablePawns();
		defaultMilkProducts ??= GetDefaultMilkProducts();
		humanlikeBreastfeed ??= new HumanlikeBreastfeed();
		animalBreastfeed ??= new AnimalBreastfeed();
		mechanoidBreastfeed ??= new MechanoidBreastfeed();
		colonistSetting ??= new MilkSettings();
		slaveSetting ??= new MilkSettings();
		prisonerSetting ??= new MilkSettings();
		animalSetting ??= new MilkSettings();
		mechSetting ??= new MilkSettings();
		entitySetting ??= new MilkSettings();

		// 涓?Tab 鏍忥紙涓撲笟绾э細6 涓父椹?+ 1 涓粎 DevMode锛?
		List<TabRecord> mainTabs = new()
		{
			new("EM.Tab.CoreSystems".Translate(), () => { mainTabIndex = (int)MainTabIndex.CoreSystems; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.CoreSystems),
			new("EM.Tab.HealthRisk".Translate(), () => { mainTabIndex = (int)MainTabIndex.HealthRisk; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.HealthRisk),
			new("EM.Tab.Permissions".Translate(), () => { mainTabIndex = (int)MainTabIndex.Permissions; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Permissions),
			new("EM.Tab.Balance".Translate(), () => { mainTabIndex = (int)MainTabIndex.Balance; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Balance),
			new("EM.Tab.Integrations".Translate(), () => { mainTabIndex = (int)MainTabIndex.Integrations; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Integrations),
			new("EM.Tab.DataRaces".Translate(), () => { mainTabIndex = (int)MainTabIndex.DataRaces; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.DataRaces)
		};
		if (Prefs.DevMode)
			mainTabs.Add(new TabRecord("EM.Tab.DevTools".Translate(), () => { mainTabIndex = (int)MainTabIndex.DevTools; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.DevTools));
		if (mainTabIndex >= mainTabs.Count)
			mainTabIndex = 0;
		TabDrawer.DrawTabs(inRect, mainTabs);
		inRect.yMin += unitSize;

		// 瀛?Tab 鏍忥紙闅忎富 Tab 鍙樺寲锛?
		List<TabRecord> subTabs = GetSubTabs();
		if (subTabIndex >= subTabs.Count)
			subTabIndex = 0;
		if (subTabs.Count > 0)
		{
			TabDrawer.DrawTabs(inRect, subTabs);
			inRect.yMin += unitSize;
		}

		Widgets.DrawMenuSection(inRect);
		Rect contentRect = inRect.ContractedBy(unitSize / 2);

		// 涓撲笟绾?7 涓?Tab 鍐呭鍒嗗彂
		switch (mainTabIndex)
		{
			case (int)MainTabIndex.CoreSystems:
				breastfeedSettings ??= new Widget_BreastfeedSettings(humanlikeBreastfeed, animalBreastfeed, mechanoidBreastfeed);
				cumpilationSettings ??= new Widget_CumpilationSettings();
				if (subTabIndex == 0)
					breastfeedSettings.DrawBreastfeedSystemFull(contentRect);
				else if (subTabIndex == 1)
					cumpilationSettings.Draw(contentRect);
				else
				{
					GUI.color = Color.gray;
					Widgets.Label(contentRect, "EM.SubTab.FluidsGirlJuicePlaceholder".Translate());
					GUI.color = Color.white;
				}
				break;
			case (int)MainTabIndex.HealthRisk:
				advancedSettings ??= new Widget_AdvancedSettings();
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.HealthRisk, subTabIndex);
				break;
			case (int)MainTabIndex.Permissions:
				advancedSettings ??= new Widget_AdvancedSettings();
				defaultSettingWidget ??= new Widget_DefaultSetting(colonistSetting, slaveSetting, prisonerSetting, animalSetting, mechSetting, entitySetting);
				if (subTabIndex == 0)
					advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Permissions, 0);
				else
					defaultSettingWidget.Draw(contentRect);
				break;
			case (int)MainTabIndex.Balance:
				advancedSettings ??= new Widget_AdvancedSettings();
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Balance, 0);
				break;
			case (int)MainTabIndex.Integrations:
				advancedSettings ??= new Widget_AdvancedSettings();
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Integrations, subTabIndex);
				break;
			case (int)MainTabIndex.DataRaces:
				milkableTable ??= new Widget_MilkableTable(namesToProducts);
				milkTagsTable ??= new Widget_MilkTagsTable(namesToProducts, productsToTags);
				raceOverridesWidget ??= new Widget_RaceOverrides();
				geneSetting ??= new Widget_GeneSetting(genes);
				if (subTabIndex == 0)
					milkableTable.Draw(contentRect);
				else if (subTabIndex == 1)
					milkTagsTable.Draw(contentRect);
				else if (subTabIndex == 2)
					raceOverridesWidget.Draw(contentRect);
				else
				{
					float devHeight = Prefs.DevMode ? 220f : 0f;
					Rect geneRect = new Rect(contentRect.x, contentRect.y, contentRect.width, contentRect.height - devHeight);
					geneSetting.Draw(geneRect);
					if (Prefs.DevMode)
					{
						Rect devRect = new Rect(contentRect.x, contentRect.yMax - devHeight, contentRect.width, devHeight - 10f);
						advancedSettings ??= new Widget_AdvancedSettings();
						advancedSettings.DrawDevModeSection(devRect);
					}
				}
				break;
			case (int)MainTabIndex.DevTools:
				advancedSettings ??= new Widget_AdvancedSettings();
				advancedSettings.DrawDevModeSection(contentRect);
				break;
		}
	}

	private List<TabRecord> GetSubTabs()
	{
		switch (mainTabIndex)
		{
			case (int)MainTabIndex.CoreSystems:
				return new List<TabRecord>
				{
					new("EM.SubTab.BreastfeedSystem".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.CumSystem".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.FluidBehavior".Translate(), () => subTabIndex = 2, subTabIndex == 2)
				};
			case (int)MainTabIndex.HealthRisk:
				return new List<TabRecord>
				{
					new("EM.SubTab.MastitisSystem".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.HygieneSystem".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.ToleranceSystem".Translate(), () => subTabIndex = 2, subTabIndex == 2),
					new("EM.SubTab.OverflowPollution".Translate(), () => subTabIndex = 3, subTabIndex == 3)
				};
			case (int)MainTabIndex.Permissions:
				return new List<TabRecord>
				{
					new("EM.SubTab.MenuVisibility".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.DefaultBehavior".Translate(), () => subTabIndex = 1, subTabIndex == 1)
				};
			case (int)MainTabIndex.Balance:
				return new List<TabRecord>
				{
					new("EM.SubTab.BalanceScaling".Translate(), () => subTabIndex = 0, subTabIndex == 0)
				};
			case (int)MainTabIndex.Integrations:
				return new List<TabRecord>
				{
					new("EM.SubTab.RJWIntegration".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.DBHIntegration".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.NutritionSystem".Translate(), () => subTabIndex = 2, subTabIndex == 2)
				};
			case (int)MainTabIndex.DataRaces:
				return new List<TabRecord>
				{
					new("EM.SubTab.Milk".Translate(), () => subTabIndex = 0, subTabIndex == 0),
					new("EM.SubTab.MilkTags".Translate(), () => subTabIndex = 1, subTabIndex == 1),
					new("EM.SubTab.RaceOverrides".Translate(), () => subTabIndex = 2, subTabIndex == 2),
					new("EM.SubTab.GenesAndAdvanced".Translate(), () => subTabIndex = 3, subTabIndex == 3)
				};
			case (int)MainTabIndex.DevTools:
				return new List<TabRecord>();
			default:
				return new List<TabRecord>();
		}
	}
	private static IEnumerable<ThingDef> GetMilkablePawns()
	{
		IEnumerable<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefs.Where(x => x.race != null && !x.IsCorpse)
			.OrderByDescending(def => def.race.Humanlike)
			.ThenByDescending(def => def.race.Animal)
			.ThenByDescending(def => def.race.IsMechanoid)
			.ThenBy(def => def.race.Insect)
			.ThenByDescending(def => def.modContentPack?.IsOfficialMod == true)
			.ThenBy(def => def.modContentPack?.Name ?? "")
			.ThenBy(def => def.defName);
		return allDefs;
	}
	private static IEnumerable<ThingDef> GetProductDefs()
	{
		return namesToProducts.Values.Where(product => product?.milkTypeDefName != null && DefDatabase<ThingDef>.GetNamedSilentFail(product.milkTypeDefName) != null)
									.Select(product => DefDatabase<ThingDef>.GetNamedSilentFail(product.milkTypeDefName)).Distinct();
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
				milkProduct.milkAmount = compMilkable.milkAmount;
				milkProduct.isMilkable = true;
				milkProducts.Add(def, milkProduct);
			}
		}
		return milkProducts;
	}
	internal void UpdateMilkCumSettings()
	{
		EventHelper.TriggerSettingsChanged();
		pawnDefs ??= GetMilkablePawns();
		defaultMilkProducts ??= GetDefaultMilkProducts();
		HediffDefOf.Lactating.maxSeverity = 100f; // 鍏佽 severity 鑷敱鍙犲姞
		humanlikeBreastfeed ??= new HumanlikeBreastfeed();
		animalBreastfeed ??= new AnimalBreastfeed();
		mechanoidBreastfeed ??= new MechanoidBreastfeed();
		foreach (ThingDef pawnDef in pawnDefs)
		{
			UpdateEqualMilkableComp(pawnDef);
		}
		productDefs = GetProductDefs();
		foreach (ThingDef milkDef in productDefs)
		{
			AddOrSetShowProducerComp(milkDef);
		}
		// Recipe products from human milk (e.g. vanilla Milk) can show producer
		ThingDef vanillaMilk = DefDatabase<ThingDef>.GetNamedSilentFail("Milk");
		if (vanillaMilk != null)
		{
			AddOrSetShowProducerComp(vanillaMilk);
			if (!productsToTags.ContainsKey("Milk"))
				productsToTags.Add("Milk", new MilkTag("Milk", true, false));
		}
		// 浜哄ザ锛氶粯璁ゅ紑鍚樉绀哄姩鐗╁悕锛屼骇涓婚檺鍒躲€岃皝鍙互鍚冦€嶆墠鐢熸晥
		ThingDef humanMilkDef = DefDatabase<ThingDef>.GetNamedSilentFail("EM_HumanMilk");
		if (humanMilkDef != null && !productsToTags.ContainsKey("EM_HumanMilk"))
			productsToTags.Add("EM_HumanMilk", new MilkTag("EM_HumanMilk", true, false));
		// 7.10: rjw-genes cum milk etc. 鈥?ensure breast-sourced cum gets producer so allowedConsumers apply
		ThingDef cumDef = DefDatabase<ThingDef>.GetNamedSilentFail("Cum_Cum");
		if (cumDef != null && !productsToTags.ContainsKey("Cum_Cum"))
			productsToTags.Add("Cum_Cum", new MilkTag("Cum_Cum", true, false));
		foreach (Pawn pawn in PawnsFinder.AllMaps)
		{
			pawn.LactatingHediffWithComps()?.SetDirty();
		}
	}
	private void UpdateEqualMilkableComp(ThingDef pawnDef)
	{
		// 璁捐鍘熷垯 3锛氫笉閲嶅瀹氫箟銆岀鏃忔槸鍚︿骇濂躲€嶅簳灞傝鍒欙紱浠呯敤 namesToProducts 涓庤缃仛寮€鍏筹紝榛樿鍊兼潵鑷?GetDefaultMilkProduct(Def)銆?
		CompProperties_Milkable compProperties = pawnDef.GetCompProperties<CompProperties_Milkable>();
		if (compProperties == null)
		{
			compProperties = new CompProperties_Milkable();
			pawnDef.comps.Add(compProperties);
		}
		if (!namesToProducts.ContainsKey(pawnDef.defName))
		{
			namesToProducts.Add(pawnDef.defName, GetDefaultMilkProduct(pawnDef));
		}
		compProperties.Set(namesToProducts[pawnDef.defName]);
	}

	/// <summary>璁捐鍘熷垯 3/4锛氶粯璁や骇濂跺紑鍏充笌濂堕噺鏉ヨ嚜 Def锛圚umanlike/浣撳瀷锛夛紝涓嶆墜鍐欏簳灞傝鍒欙紱瀹為檯寮€鍏崇敱 namesToProducts + 璁剧疆鍐冲畾銆</summary>
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
			if (def.race.Humanlike)
			{
				milkProduct.milkAmount = Mathf.FloorToInt(3f * def.race.baseBodySize / ThingDefOf.Human.race.baseBodySize);
				milkProduct.isMilkable = true;
				milkProduct.milkTypeDefName = MilkCumDefOf.EM_HumanMilk.defName;
			}
			else
			{
				milkProduct.isMilkable = false;
				milkProduct.milkAmount = Mathf.FloorToInt(14f * def.race.baseBodySize / ThingDefOf.Cow.race.baseBodySize);
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
	#region Internal setting getters
	internal static bool IsMilkable(string name)
	{
		if (raceCannotLactate != null && raceCannotLactate.Contains(name))
			return false;
		if (raceCanAlwaysLactate != null && raceCanAlwaysLactate.Contains(name))
			return true;
		if (namesToProducts.ContainsKey(name))
		{
			return namesToProducts[name].isMilkable;
		}
		return false;
	}
	internal static bool IsMilkable(Pawn pawn)
	{
		return IsMilkable(pawn.def.defName);
	}
	internal static bool HasPawnTag(Thing thing)
	{
		if (thing == null) { return false; }
		if (productsToTags.ContainsKey(thing.def.defName))
		{
			MilkTag tag = productsToTags[thing.def.defName];
			if (tag.TagPawn && thing is ThingWithComps)
			{
				return true;
			}
		}
		return false;
	}
	internal static bool HasRaceTag(Thing thing)
	{
		if (thing == null) { return false; }
		if (productsToTags.ContainsKey(thing.def.defName))
		{
			MilkTag tag = productsToTags[thing.def.defName];
			if (tag.TagRace && thing is ThingWithComps)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>濂舵爣绛捐仈鍔細浜т富闄愬埗銆岃皝鍙互鍚冩垜鐨勫ザ鍒跺搧銆嶄粎褰撳ザ鏍囩閲屽搴旂墿鍝佺绫诲紑鍚€屾樉绀哄姩鐗╁悕銆嶆椂鐢熸晥銆傛湰鏂规硶涓?true 鏃讹紝浜т富闄愬埗绐楀彛鏄剧ず璇ュ尯鍧椼€</summary>
	internal static bool IsProducerRestrictionConsumersEffectiveForMilkProducts()
	{
		if (productsToTags.TryGetValue("EM_HumanMilk", out MilkTag t) && t.TagPawn) return true;
		if (productsToTags.TryGetValue("Milk", out t) && t.TagPawn) return true;
		return false;
	}

	/// <summary>濂舵爣绛捐仈鍔細浜т富闄愬埗銆岃皝鍙互鍚冩垜鐨勭簿娑插埗鍝併€嶄粎褰撳ザ鏍囩閲岀簿娑插紑鍚€屾樉绀哄姩鐗╁悕銆嶆椂鐢熸晥銆傛湰鏂规硶涓?true 鏃讹紝浜т富闄愬埗绐楀彛鏄剧ず璇ュ尯鍧椼€</summary>
	internal static bool IsProducerRestrictionConsumersEffectiveForCumProducts()
	{
		return productsToTags.TryGetValue("Cum_Cum", out MilkTag t) && t.TagPawn;
	}

	internal static bool MilkTypeCanBreastfeed(Pawn mom)
	{
		ThingDef milkDef = mom.MilkDef();
		if (milkDef?.ingestible == null) { return false; }
		if (milkDef.IsNutritionGivingIngestible) { return true; }
		if (milkDef.ingestible.drugCategory == DrugCategory.None && milkDef.ingestible.outcomeDoers.NullOrEmpty()) { return false; }
		return true;
	}
	internal static bool CanBreastfeedEver(Pawn mom, Pawn baby)
	{
		if (mom == baby) { return false; }
		if (mom.RaceProps.Humanlike && humanlikeBreastfeed.AllowBreastfeeding)
		{
			if (baby.RaceProps.Humanlike && humanlikeBreastfeed.BreastfeedHumanlike)
			{
				return true;
			}
			if (baby.RaceProps.Animal && humanlikeBreastfeed.BreastfeedAnimal)
			{
				return true;
			}
			if (baby.RaceProps.IsMechanoid)
			{
				if (humanlikeBreastfeed.BreastfeedMechanoid) { return true; }
				if (MechanitorUtility.IsMechanitor(mom) && MechanitorUtility.GetOverseer(baby) == mom && humanlikeBreastfeed.OverseerBreastfeed) { return true; }
			}
			return false;
		}
		if (mom.RaceProps.Animal && animalBreastfeed.AllowBreastfeeding)
		{
			if (baby.RaceProps.Humanlike && animalBreastfeed.BreastfeedHumanlike)
			{
				return true;
			}
			if (baby.RaceProps.Animal && animalBreastfeed.BreastfeedAnimal)
			{
				return true;
			}
			if (baby.RaceProps.IsMechanoid && animalBreastfeed.BreastfeedMechanoid)
			{
				return true;
			}
			return false;
		}
		if (mom.RaceProps.IsMechanoid && mechanoidBreastfeed.AllowBreastfeeding)
		{
			if (baby.RaceProps.Humanlike && mechanoidBreastfeed.BreastfeedHumanlike)
			{
				return true;
			}
			if (baby.RaceProps.Animal && mechanoidBreastfeed.BreastfeedAnimal)
			{
				return true;
			}
			if (baby.RaceProps.IsMechanoid && mechanoidBreastfeed.BreastfeedMechanoid)
			{
				return true;
			}
			return false;
		}
		return false;
	}
	internal static List<Pawn> GetAutoBreastfeedablePawnsList(Pawn mom)
	{
		if (mom.MilkTypeCanBreastfeed() == false) { return new List<Pawn>(); }
		return mom.MapHeld.mapPawns.AllPawns.Where(baby => mom.AllowedToAutoBreastFeed(baby)).ToList();
	}
	internal static ThingDef GetMilkProductDef(Pawn pawn)
	{
		if (pawn.genes?.GenesListForReading.Where(x => x.Active && x.def.defName.StartsWith(MilkCum.Core.Constants.Constants.MILK_TYPE_PREFIX)).FirstOrDefault()?.def is GeneDef geneDef)
		{
			ThingDef thingDef = DefDatabase<ThingDef>.GetNamed(geneDef.defName.Replace(MilkCum.Core.Constants.Constants.MILK_TYPE_PREFIX, ""));
			if (thingDef != null)
			{
				return thingDef;
			}
		}
		return namesToProducts.GetWithFallback(pawn.def.defName, new RaceMilkType()).MilkTypeDef;
	}
	internal static float GetMilkAmount(Pawn pawn)
	{
		return namesToProducts.GetWithFallback(pawn.def.defName, new RaceMilkType()).milkAmount;
	}
	/// <summary>褰撳墠鍌钩绱犺€愬彈涓ラ噸搴?t 鈭?[0,1]锛涘畬鍏ㄧ敱娓告垙鍐?Hediff/Comp 鍐冲畾銆</summary>
	/// <summary>3.3 鍔ㄧ墿宸紓鍖栵細绉嶆棌瀵瑰偓涔宠嵂鐗╄繘姘村€嶇巼锛屾湭閰嶇疆鍒?1銆</summary>
	internal static float GetRaceDrugDeltaSMultiplier(Pawn pawn)
	{
		if (pawn?.def?.defName == null || raceDrugDeltaSMultiplierDefNames == null || raceDrugDeltaSMultiplierValues == null) return 1f;
		int i = raceDrugDeltaSMultiplierDefNames.IndexOf(pawn.def.defName);
		if (i < 0 || i >= raceDrugDeltaSMultiplierValues.Count) return 1f;
		return Mathf.Clamp(raceDrugDeltaSMultiplierValues[i], 0.1f, 3f);
	}

	internal static float GetProlactinTolerance(Pawn pawn)
		=> pawn?.health?.hediffSet?.GetFirstHediffOfDef(MilkCumDefOf.EM_Prolactin_Tolerance)?.Severity ?? 0f;

	/// <summary>缁熶竴鑰愬彈绯绘暟锛欵_tol(t) = max(1 鈭?t, 0.05)銆傚惎鐢ㄨ€愬彈鍔ㄦ€佹椂鐢?comp 鐨?E 璁＄畻銆</summary>
	internal static float GetProlactinToleranceFactor(Pawn pawn)
	{
		if (!allowToleranceAffectMilk) return 1f;
		if (enableToleranceDynamic && pawn != null)
		{
			var lactating = pawn.LactatingHediffWithComps();
			var comp = lactating?.TryGetComp<HediffComp_EqualMilkingLactating>();
			if (comp != null)
				return GetProlactinToleranceFactorFromE(comp.GetEffectiveToleranceE());
		}
		float t = GetProlactinTolerance(pawn);
		return GetProlactinToleranceFactor(t);
	}

	/// <summary>鑰愬彈鍔ㄦ€侊細鐢?mod 缁存姢鐨?E 寰楀埌 E_tol = [max(1鈭扙, 0.05)]^exponent銆</summary>
	internal static float GetProlactinToleranceFactorFromE(float E)
	{
		if (!allowToleranceAffectMilk) return 1f;
		float e = Mathf.Max(1f - Mathf.Clamp01(E), PoolModelConstants.EffectiveDrugFactorMin);
		return Mathf.Pow(e, Mathf.Clamp(toleranceFlowImpactExponent, 0.1f, 3f));
	}

	/// <summary>缁熶竴鑰愬彈绯绘暟锛堟寜涓ラ噸搴?t锛夛細E_tol(t) = [max(1 鈭?t, 0.05)]^exponent锛沘llowToleranceAffectMilk 鍏抽棴鏃舵亽涓?1銆</summary>
	internal static float GetProlactinToleranceFactor(float toleranceSeverity)
	{
		if (!allowToleranceAffectMilk) return 1f;
		float e = Mathf.Max(1f - toleranceSeverity, PoolModelConstants.EffectiveDrugFactorMin);
		return Mathf.Pow(e, Mathf.Clamp(toleranceFlowImpactExponent, 0.1f, 3f));
	}

	#endregion
}

/// <summary>寤鸿 13锛氫钩鑵虹値/椋庨櫓涓庤€愬彈鐩稿叧璁剧疆鍒嗙粍锛屼究浜庡簭鍒楀寲涓?UI锛汦xposeData 浠嶅啓鏃?key 浠ュ吋瀹瑰瓨妗ｃ€</summary>
public class MilkRiskSettings : IExposable
{
	public bool allowMastitis = true;
	public float mastitisBaseMtbDays = 1.5f;
	public float overFullnessRiskMultiplier = 1.5f;
	public float hygieneRiskMultiplier = 1f;
	public bool allowToleranceAffectMilk = true;
	public float toleranceFlowImpactExponent = 1f;
	/// <summary>寤鸿 8锛氫汉褰?鍔ㄧ墿涔宠吅鐐?MTB 涔樻暟锛屼究浜庡尯鍒嗗钩琛°€</summary>
	public float mastitisMtbDaysMultiplierHumanlike = 1f;
	public float mastitisMtbDaysMultiplierAnimal = 1f;

	public void ExposeData()
	{
		Scribe_Values.Look(ref allowMastitis, "EM.AllowMastitis", true);
		Scribe_Values.Look(ref mastitisBaseMtbDays, "EM.MastitisBaseMtbDays", 1.5f);
		Scribe_Values.Look(ref overFullnessRiskMultiplier, "EM.OverFullnessRiskMultiplier", 1.5f);
		Scribe_Values.Look(ref hygieneRiskMultiplier, "EM.HygieneRiskMultiplier", 1f);
		Scribe_Values.Look(ref allowToleranceAffectMilk, "EM.AllowToleranceAffectMilk", true);
		Scribe_Values.Look(ref toleranceFlowImpactExponent, "EM.ToleranceFlowImpactExponent", 1f);
		Scribe_Values.Look(ref mastitisMtbDaysMultiplierHumanlike, "EM.MastitisMtbDaysMultiplierHumanlike", 1f);
		Scribe_Values.Look(ref mastitisMtbDaysMultiplierAnimal, "EM.MastitisMtbDaysMultiplierAnimal", 1f);
	}
}
