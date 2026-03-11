using System.Collections.Generic;
using System.Linq;
using MilkCum.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>主 Tab 索引，用于替代魔术数字 0～4，便于维护与 DrawSection 传参。</summary>
public enum MainTabIndex
{
	MilkAndFluids = 0,
	Breastfeed = 1,
	HealthAndRisk = 2,
	EfficiencyAndInterface = 3,
	IntegrationAndAdvanced = 4
}

[StaticConstructorOnStartup]
internal class MilkCumSettings : ModSettings
{
	private static Dictionary<string, RaceMilkType> namesToProducts = new();
	private static Dictionary<string, MilkTag> productsToTags = new();
	/// <summary>挤奶流速基准：baseFlowPerSecond = 60/本值（池单位/秒）。默认 60 → 满池约 1 瓶/秒（现实时间）；调大则变慢。</summary>
	public static float milkingWorkTotalBase = 60f;
	/// <summary>按容量量化：吸奶有效时间随喂奶者 MilkAmount 的系数，effectiveTime *= (1 + 本值×(MilkAmount-1))，限制在 [0.5, 2]。</summary>
	public static float breastfeedCapacityFactor = 0.1f;
	public static bool femaleAnimalAdultAlwaysLactating = false;
	public static bool showMechOptions = true;
	public static bool showColonistOptions = true;
	public static bool showSlaveOptions = true;
	public static bool showPrisonerOptions = true;
	public static bool showAnimalOptions = true;
	public static bool showMiscOptions = true;
	public static float nutritionToEnergyFactor = 100f;
	/// <summary>泌乳灌满期间额外饥饿：滑块 0–300，150=1:1。饱食度每 150 tick 额外下降 = flowPerDay×(150/60000)×(本值/150)。</summary>
	public static int lactationExtraNutritionBasis = 150;
	/// <summary>回缩吸收：满池回缩时，未溢出部分视为被身体吸收，按比例补充饱食度；0=关闭，1=与产奶消耗 1:1 折算。</summary>
	public static bool reabsorbNutritionEnabled = true;
	/// <summary>回缩吸收效率：0~1，吸收的池单位折成营养的比例，默认 0.5 避免满池挂机过强。</summary>
	public static float reabsorbNutritionEfficiency = 0.5f;
	/// <summary>DevMode 且勾选时，每 60 tick 输出泌乳小人的营养/乳池/回缩/吸奶明细到日志。</summary>
	public static bool lactationPoolTickLog = false;
	/// <summary>DevMode 时输出泌乳关键路径日志，便于排查 L/池/药物/分娩 行为。</summary>
	public static void LactationLog(string message)
	{
		if (Verse.Prefs.DevMode && !string.IsNullOrEmpty(message))
			Verse.Log.Message("[MilkCum.Lactation] " + message);
	}
	/// <summary>仅当 DevMode 且 lactationPoolTickLog 为 true 时输出，用于每步营养/乳池/回缩/吸奶明细。</summary>
	public static void PoolTickLog(string message)
	{
		if (Verse.Prefs.DevMode && lactationPoolTickLog && !string.IsNullOrEmpty(message))
			Verse.Log.Message("[MilkCum.Pool] " + message);
	}
	public static HumanlikeBreastfeed humanlikeBreastfeed = new();
	public static AnimalBreastfeed animalBreastfeed = new();
	public static MechanoidBreastfeed mechanoidBreastfeed = new();
	// 泌乳期意识/操纵/移动增益：开关与百分比 (0~0.20 = 0%~20%)
	public static bool lactatingGainEnabled = true;
	public static float lactatingGainCapModPercent = 0.10f;
	// RJW 联动（仅当 rim.job.world 激活时生效）
	public static bool rjwBreastSizeEnabled = true;
	/// <summary>乳房容量系数：左右乳容量 = RJW Severity × 本系数，2=默认，与泌乳效率等可调项对应。</summary>
	public static float rjwBreastCapacityCoefficient = 2f;
	public static bool rjwLustFromNursingEnabled = true;
	public static bool rjwSexNeedLactatingBonusEnabled = true;
	public static bool rjwSexSatisfactionAfterNursingEnabled = true;
	public static float rjwLactationFertilityFactor = 0.85f; // 泌乳期怀孕概率乘数 (0~1)
	public static bool rjwLactatingInSexDescriptionEnabled = true;
	/// <summary>3.2：性行为后为泌乳参与者增加少量池进水（ΔL），可选。</summary>
	public static bool rjwSexAddsLactationBoost = false;
	public static float rjwSexLactationBoostDeltaS = 0.15f;
	/// <summary>阶段3：性行为是否同时视为排乳刺激（调用 NotifyDrained：更新 lastDrainTick、StimulusBuffer），延寿效果更强。</summary>
	public static bool rjwSexCountsAsStimulus = false;
	// 乳腺炎/堵塞：卫生触发是否与 Dubs Bad Hygiene 联动（有 DBH 时用 Hygiene 需求，否则用房间清洁度）
	public static bool useDubsBadHygieneForMastitis = true;
	// 乳腺炎可配置：是否启用、基准 MTB（天）、满池过久风险系数、卫生风险系数
	// 耐受对泌乳效率的影响：关闭则 E_tol 恒为 1；指数控制曲线（1=线性）
	// 建议 13：收拢为 MilkRiskSettings，便于序列化与 UI 分组；对外仍用静态属性，存档兼容旧 key
	private static MilkRiskSettings _risk = new MilkRiskSettings();
	private static MilkRiskSettings Risk => _risk ??= new MilkRiskSettings();
	public static bool allowMastitis { get => Risk.allowMastitis; set => Risk.allowMastitis = value; }
	public static float mastitisBaseMtbDays { get => Risk.mastitisBaseMtbDays; set => Risk.mastitisBaseMtbDays = value; }
	public static float overFullnessRiskMultiplier { get => Risk.overFullnessRiskMultiplier; set => Risk.overFullnessRiskMultiplier = value; }
	public static float hygieneRiskMultiplier { get => Risk.hygieneRiskMultiplier; set => Risk.hygieneRiskMultiplier = value; }
	public static bool allowToleranceAffectMilk { get => Risk.allowToleranceAffectMilk; set => Risk.allowToleranceAffectMilk = value; }
	public static float toleranceFlowImpactExponent { get => Risk.toleranceFlowImpactExponent; set => Risk.toleranceFlowImpactExponent = value; }
	// 耐受动态 dE/dt = μ·L − ν·E：启用时用 mod 维护的 E 计算 E_tol（流速/衰减），替代仅用游戏内耐受严重度 t。
	public static bool enableToleranceDynamic = true;
	/// <summary>耐受累积率 μ（每游戏日）；L 高则 E 上升。</summary>
	public static float toleranceDynamicMu = 0.03f;
	/// <summary>耐受衰减率 ν（每游戏日）；E 自然回落。</summary>
	public static float toleranceDynamicNu = 0.08f;
	public static float mastitisMtbDaysMultiplierHumanlike { get => Risk.mastitisMtbDaysMultiplierHumanlike; set => Risk.mastitisMtbDaysMultiplierHumanlike = value; }
	public static float mastitisMtbDaysMultiplierAnimal { get => Risk.mastitisMtbDaysMultiplierAnimal; set => Risk.mastitisMtbDaysMultiplierAnimal = value; }
	// 满池溢出地面污物：Def 名称，空或无效时回退 Filth_Vomit
	public static string overflowFilthDefName = "Filth_Vomit";
	// 基准泌乳持续天数（药物诱发）：参与 L 衰减计算，单次剂量 L≈0.5 时剩余天数 ≈ 本值；默认约 5 日。
	public static float baselineMilkDurationDays = 5f;
	// 分娩诱发泌乳持续天数：参与 L 衰减计算（分娩分量用本值反推 B_T）；默认 30 日。
	public static float birthInducedMilkDurationDays = 30f;
	/// <summary>催乳素单剂在 XML 中对耐受 Hediff 的 Severity 增量（与 Lactating 同剂叠加一致，默认 0.044）；改 XML 时需同步。</summary>
	public static float ProlactinToleranceGainPerDose = 0.044f;

	/// <summary>药物泌乳衰减用有效 B_T：由 baselineMilkDurationDays 反推，使单次剂量（L≈0.5、E=1）时剩余天数 ≈ 基准天数。D=0.5/baseline ⇒ B_T_eff=1/(0.5/baseline−k×0.5)。</summary>
	public static float GetEffectiveBaseValueTForDecay()
	{
		float baseline = baselineMilkDurationDays;
		if (baseline <= 0f) return PoolModelConstants.BaseValueT;
		float denom = 0.5f / baseline - PoolModelConstants.NegativeFeedbackK * 0.5f;
		if (denom <= 0.01f) return 100f;
		return 1f / denom;
	}
	/// <summary>分娩泌乳衰减用有效 B_T：由 birthInducedMilkDurationDays 反推，公式同药物。</summary>
	public static float GetEffectiveBaseValueTForDecayBirth()
	{
		float baseline = birthInducedMilkDurationDays;
		if (baseline <= 0f) return PoolModelConstants.BaseValueT;
		float denom = 0.5f / baseline - PoolModelConstants.NegativeFeedbackK * 0.5f;
		if (denom <= 0.01f) return 100f;
		return 1f / denom;
	}
	/// <summary>带下限 Logistic：f(P)=f_min+(1−f_min)×1/(1+exp(k×(P−Pc)))，P 大时平滑降速，最低 f_min 永不归零。默认 k=6、Pc=0.9、f_min=0.02，奶量达 90% 才开始往下压。</summary>
	/// <param name="P">该侧满度/该侧撑大容量，0～1（可略大于 1 时仍按公式算）。</param>
	public static float GetPressureFactor(float P)
	{
		if (!enablePressureFactor || P <= 0f)
			return 1f;

		float k = Mathf.Clamp(pressureFactorB, 0.5f, 80f);
		float pc = Mathf.Clamp(pressureFactorPc, 0.3f, 1f);
		float fMin = Mathf.Clamp01(pressureFactorMin);

		// 带下限 Logistic：最低 f_min，P 越大越接近 f_min
		float logistic = 1f / (1f + Mathf.Exp(k * (P - pc)));
		return fMin + (1f - fMin) * logistic;
	}
	/// <summary>四层模型（阶段3）：有效驱动力 D_eff = L·H(L)。H(L)=1−exp(−a·L)；若 L_ref>0 则用参考值归一：D_eff = L·(1−e^{-aL})/(1−e^{-aL_ref})，尾期更平滑。</summary>
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
	// 挤奶工作：是否优先选择满度更高的目标（殖民者会先挤更满的）
	public static bool aiPreferHighFullnessTargets = true;
	// 种族覆盖：白名单（defName 在此列表中视为可产奶）、黑名单（defName 在此列表中视为不可产奶）
	public static List<string> raceCanAlwaysLactate = new();
	public static List<string> raceCannotLactate = new();
	// 人形种族默认流速倍率（2 = 单次剂量约 1 日灌满；与 RJW/种族 mod 平衡时也可调）
	public static float defaultFlowMultiplierForHumanlike = 2f;
	// 四层模型（阶段1）：压力软抑制。启用时流速乘 PressureFactor(P)；带下限 Logistic，默认 Pc=0.9、k=6、f_min=0.02，奶量达 90% 才开始往下压。
	public static bool enablePressureFactor = true;
	public static float pressureFactorPc = 0.9f;
	public static float pressureFactorB = 6f;
	/// <summary>压力曲线下限 f_min：满池时生产倍率不低于此值，永不归零。推荐 0.02～0.15。</summary>
	public static float pressureFactorMin = 0.02f;
	// 四层模型（阶段1）：喷乳反射 R。启用时流速乘 R；R 每 60 tick 指数衰减，挤奶/吸奶时升高。
	public static bool enableLetdownReflex = true;
	/// <summary>喷乳反射衰减率 λ（每分钟）；R_new = R × exp(-λ×Δt)。</summary>
	public static float letdownReflexDecayLambda = 0.03f;
	/// <summary>挤奶/吸奶时 R 的增量 ΔR，R 加上后 Clamp 至 1。设大一些（如 1）可一次刺激即满 R。</summary>
	public static float letdownReflexStimulusDeltaR = 0.45f;
	/// <summary>喷乳反射加成倍率：进水流速倍率 = 1 + R×(本值−1)，R=1 时为本值倍（建议 1.5~2.5），R=0 时为 1 倍；设为 1 即无加成。</summary>
	public static float letdownReflexBoostMultiplier = 2f;
	// 四层模型（阶段2）：炎症 I(t)。启用时每 60 tick 更新 I；I>I_crit 触发乳腺炎；L 衰减加 η·I。
	public static bool enableInflammationModel = true;
	public static float inflammationAlpha = 0.1f;
	public static float inflammationBeta = 0.15f;
	public static float inflammationGamma = 0.2f;
	public static float inflammationRho = 0.05f;
	public static float inflammationCrit = 1f;
	/// <summary>炎症对 L 衰减的抑制因子 η：D += η·I。</summary>
	public static float lactationDecayInflammationEta = 0.1f;
	// 四层模型（阶段2）：挤奶/吸奶时 L 微幅刺激（带上限，防无限循环）。
	public static float milkingLStimulusPerEvent = 0.03f;
	public static float milkingLStimulusCapPerEvent = 0.05f;
	public static float milkingLStimulusCapPerDay = 0.2f;
	// 激素模型（催乳素与排乳反馈）：排乳刺激独立于炎症；见 记忆库/design/激素模型-催乳素与排乳反馈.md
	/// <summary>排乳时是否增加 L（催乳素）刺激；与 enableInflammationModel 独立，关闭炎症时仍可保留排乳延寿。</summary>
	public static bool enableProlactinStimulusFromMilking = true;
	/// <summary>长期未排乳抑制：启用时 D_eff = D×(1+SuppressionFactor)，SuppressionFactor = α×days^β。</summary>
	public static bool enableLactationSuppressionFromNoDrain = true;
	/// <summary>抑制系数 α；SuppressionFactor = α×days_since_drain^β。</summary>
	public static float suppressionCoeff = 0.02f;
	/// <summary>抑制指数 β（≥1）；2=未排乳天数平方。</summary>
	public static float suppressionExponent = 2f;
	/// <summary>压力 P 参与 L 衰减：D_eff 再乘 (1+δ×P)，满池时 L 衰减更快（FIL 类比）。</summary>
	public static bool enableSuppressionFromPressure = true;
	/// <summary>压力对衰减的加成系数 δ；D_eff × (1+δ×P)。</summary>
	public static float pressureDecayDelta = 0.2f;
	/// <summary>排乳维持缓冲：启用时 D_eff 除以 (1+γ×StimulusBuffer)，排乳后一段时间内衰减变慢。</summary>
	public static bool enableStimulusBuffer = true;
	/// <summary>缓冲对衰减的分母系数 γ；D_eff / (1+γ×Buffer)。</summary>
	public static float stimulusBufferGamma = 0.5f;
	/// <summary>每次排乳时 Buffer 增加量 ΔB（有上限）。</summary>
	public static float stimulusBufferDeltaB = 0.08f;
	/// <summary>StimulusBuffer 上限。</summary>
	public static float stimulusBufferCap = 1f;
	/// <summary>StimulusBuffer 每游戏日衰减量。</summary>
	public static float stimulusBufferDecayPerDay = 0.2f;
	// 四层模型（阶段3）：激素饱和 H(L)=1−exp(−a·L)，D_eff=L·H(L)。启用时流速由 D_eff 驱动，低 L 低产、高 L 饱和。
	public static bool enableHormoneSaturation = true;
	/// <summary>H(L) 饱和系数 a；建议 0.5～1.5。</summary>
	public static float hormoneSaturationA = 1f;
	/// <summary>参考 L 归一：D_eff = L·(1−e^{-aL})/(1−e^{-aL_ref})，使尾期更平滑；≤0 时不归一。</summary>
	public static float hormoneSaturationLRef = 1f;
	// 四层模型（阶段3.2）：组织适应。长期高 P 扩容、长期低 P 回缩；dF_max/dt = θ·max(P−0.85,0) − ω·(1−P)，每 60 tick 更新，叠加到基础容量上。
	public static bool enableTissueAdaptation = true;
	/// <summary>扩容率 θ（每游戏日）；P 大于 0.85 时容量增加。</summary>
	public static float adaptationTheta = 0.002f;
	/// <summary>回缩率 ω（每游戏日）；P 小于 1 时容量减少。</summary>
	public static float adaptationOmega = 0.001f;
	/// <summary>适应容量上限：不超过基础容量的此比例（如 0.2=20%）。</summary>
	public static float adaptationCapMaxRatio = 0.2f;
	// 四层模型（阶段3.3）：乳汁质量 MilkQuality = f(Hunger, I)。启用时：质量高→乳腺炎阈值提高；可选在 UI 显示。默认关闭，待办：产出奶物品的 QualityCategory。
	public static bool enableMilkQuality = false;
	/// <summary>炎症对质量的抑制系数；质量 ∝ (1 − 本值×I)，I 大则质量降。</summary>
	public static float milkQualityInflammationWeight = 0.5f;
	/// <summary>质量对乳腺炎阈值的保护系数；有效 I_crit = I_crit×(1 + 本值×MilkQuality)。</summary>
	public static float milkQualityProtectionFactor = 0.5f;
	// 3.3 满池事件：满池过久（约 1 天）时是否发信提醒
	public static bool enableFullPoolLetter = true;
	// 3.3 动物差异化：种族 defName 对应药物进水倍率（未列出的种族为 1）。与参数联动表一致。
	public static List<string> raceDrugDeltaSMultiplierDefNames = new();
	public static List<float> raceDrugDeltaSMultiplierValues = new();
	// Cumpilation（统一到本设置，不再使用单独 Mod 入口）
	public static bool Cumpilation_EnableCumflation = true;
	public static float Cumpilation_GlobalCumflationModifier = 1.0f;
	public static bool Cumpilation_EnableStuffing = true;
	public static float Cumpilation_GlobalStuffingModifier = 1.0f;
	public static bool Cumpilation_EnableBukkake = true;
	public static float Cumpilation_GlobalBukkakeModifier = 1.0f;
	public static bool Cumpilation_EnableFluidGatheringWhileCleaning = true;
	public static float Cumpilation_MaxGatheringCheckDistance = 15.0f;
	public static bool Cumpilation_EnableProgressingConsumptionThoughts = true;
	public static bool Cumpilation_EnableOscillationMechanics = true;
	public static bool Cumpilation_EnableOscillationMechanicsForAnimals = false;
	public static bool Cumpilation_EnableDebugLogging = false;
	public static bool CumpilationLeak_EnableFilthGeneration = true;
	public static bool CumpilationLeak_EnableAutoDeflateBucket = false;
	public static bool CumpilationLeak_EnableAutoDeflateClean = false;
	public static bool CumpilationLeak_EnableAutoDeflateDirty = false;
	public static bool CumpilationLeak_EnablePrivacy = true;
	public static float CumpilationLeak_AutoDeflateMinSeverity = 0.4f;
	public static float CumpilationLeak_AutoDeflateMaxDistance = 100f;
	public static float CumpilationLeak_LeakMult = 5.0f;
	public static float CumpilationLeak_LeakRate = 1.0f;
	public static float CumpilationLeak_DeflateMult = 5.0f;
	public static float CumpilationLeak_DeflateRate = 1.0f;
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
			lactationExtraNutritionBasis = 150; // 旧存档 float 1f 被读成 1，视为 150
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
		Scribe_Values.Look(ref rjwSexCountsAsStimulus, "EM.RjwSexCountsAsStimulus", false);
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
		Scribe_Values.Look(ref enableProlactinStimulusFromMilking, "EM.EnableProlactinStimulusFromMilking", true);
		Scribe_Values.Look(ref enableLactationSuppressionFromNoDrain, "EM.EnableLactationSuppressionFromNoDrain", true);
		Scribe_Values.Look(ref suppressionCoeff, "EM.SuppressionCoeff", 0.02f);
		Scribe_Values.Look(ref suppressionExponent, "EM.SuppressionExponent", 2f);
		Scribe_Values.Look(ref enableSuppressionFromPressure, "EM.EnableSuppressionFromPressure", true);
		Scribe_Values.Look(ref pressureDecayDelta, "EM.PressureDecayDelta", 0.2f);
		Scribe_Values.Look(ref enableStimulusBuffer, "EM.EnableStimulusBuffer", true);
		Scribe_Values.Look(ref stimulusBufferGamma, "EM.StimulusBufferGamma", 0.5f);
		Scribe_Values.Look(ref stimulusBufferDeltaB, "EM.StimulusBufferDeltaB", 0.08f);
		Scribe_Values.Look(ref stimulusBufferCap, "EM.StimulusBufferCap", 1f);
		Scribe_Values.Look(ref stimulusBufferDecayPerDay, "EM.StimulusBufferDecayPerDay", 0.2f);
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
		Scribe_Values.Look(ref Cumpilation_EnableCumflation, "Cumpilation.EnableCumflation", true);
		Scribe_Values.Look(ref Cumpilation_GlobalCumflationModifier, "Cumpilation.GlobalCumflationModifier", 1.0f);
		Scribe_Values.Look(ref Cumpilation_EnableStuffing, "Cumpilation.EnableStuffing", true);
		Scribe_Values.Look(ref Cumpilation_GlobalStuffingModifier, "Cumpilation.GlobalStuffingModifier", 1.0f);
		Scribe_Values.Look(ref Cumpilation_EnableBukkake, "Cumpilation.EnableBukkake", true);
		Scribe_Values.Look(ref Cumpilation_GlobalBukkakeModifier, "Cumpilation.GlobalBukkakeModifier", 1.0f);
		Scribe_Values.Look(ref Cumpilation_EnableFluidGatheringWhileCleaning, "Cumpilation.EnableFluidGatheringWhileCleaning", true);
		Scribe_Values.Look(ref Cumpilation_MaxGatheringCheckDistance, "Cumpilation.MaxGatheringCheckDistance", 15.0f);
		Scribe_Values.Look(ref Cumpilation_EnableProgressingConsumptionThoughts, "Cumpilation.EnableProgressingConsumptionThoughts", true);
		Scribe_Values.Look(ref Cumpilation_EnableOscillationMechanics, "Cumpilation.EnableOscillationMechanics", true);
		Scribe_Values.Look(ref Cumpilation_EnableOscillationMechanicsForAnimals, "Cumpilation.EnableOscillationMechanicsForAnimals", false);
		Scribe_Values.Look(ref Cumpilation_EnableDebugLogging, "Cumpilation.EnableDebugLogging", false);
		Scribe_Values.Look(ref lactationPoolTickLog, "EM.LactationPoolTickLog", false);
		Scribe_Values.Look(ref CumpilationLeak_EnableFilthGeneration, "CumpilationLeak.EnableFilthGeneration", true);
		Scribe_Values.Look(ref CumpilationLeak_EnableAutoDeflateBucket, "CumpilationLeak.EnableAutoDeflateBucket", false);
		Scribe_Values.Look(ref CumpilationLeak_EnableAutoDeflateClean, "CumpilationLeak.EnableAutoDeflateClean", false);
		Scribe_Values.Look(ref CumpilationLeak_EnableAutoDeflateDirty, "CumpilationLeak.EnableAutoDeflateDirty", false);
		Scribe_Values.Look(ref CumpilationLeak_EnablePrivacy, "CumpilationLeak.EnablePrivacy", true);
		Scribe_Values.Look(ref CumpilationLeak_AutoDeflateMinSeverity, "CumpilationLeak.AutoDeflateMinSeverity", 0.4f);
		Scribe_Values.Look(ref CumpilationLeak_AutoDeflateMaxDistance, "CumpilationLeak.AutoDeflateMaxDistance", 100f);
		Scribe_Values.Look(ref CumpilationLeak_LeakMult, "CumpilationLeak.LeakMult", 5.0f);
		Scribe_Values.Look(ref CumpilationLeak_LeakRate, "CumpilationLeak.LeakRate", 1.0f);
		Scribe_Values.Look(ref CumpilationLeak_DeflateMult, "CumpilationLeak.DeflateMult", 5.0f);
		Scribe_Values.Look(ref CumpilationLeak_DeflateRate, "CumpilationLeak.DeflateRate", 1.0f);
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

	/// <summary>确保 Scribe_Deep 反序列化的对象类型正确，避免旧存档或类型变更导致 InvalidCastException。</summary>
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
		// 防止旧存档/错误反序列化导致类型不一致引发 InvalidCastException
		EnsureScribeDeepTypes();
		inRect.yMin += unitSize;
		// 从主菜单打开设置时 PostLoadInit 未执行，需惰性初始化以免 NRE
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

		// 主 Tab 栏（5 个）
		List<TabRecord> mainTabs = new()
		{
			new("EM.Tab.MilkAndFluids".Translate(), () => { mainTabIndex = (int)MainTabIndex.MilkAndFluids; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.MilkAndFluids),
			new("EM.Tab.Breastfeed".Translate(), () => { mainTabIndex = (int)MainTabIndex.Breastfeed; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.Breastfeed),
			new("EM.Tab.HealthAndRisk".Translate(), () => { mainTabIndex = (int)MainTabIndex.HealthAndRisk; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.HealthAndRisk),
			new("EM.Tab.EfficiencyAndInterface".Translate(), () => { mainTabIndex = (int)MainTabIndex.EfficiencyAndInterface; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.EfficiencyAndInterface),
			new("EM.Tab.IntegrationAndAdvanced".Translate(), () => { mainTabIndex = (int)MainTabIndex.IntegrationAndAdvanced; subTabIndex = 0; }, mainTabIndex == (int)MainTabIndex.IntegrationAndAdvanced)
		};
		TabDrawer.DrawTabs(inRect, mainTabs);
		inRect.yMin += unitSize;

		// 子 Tab 栏（随主 Tab 变化）
		List<TabRecord> subTabs = GetSubTabs();
		if (subTabs.Count > 0)
		{
			TabDrawer.DrawTabs(inRect, subTabs);
			inRect.yMin += unitSize;
		}

		Widgets.DrawMenuSection(inRect);
		Rect contentRect = inRect.ContractedBy(unitSize / 2);

		// 根据 mainTabIndex + subTabIndex 分发内容
		switch (mainTabIndex)
		{
			case (int)MainTabIndex.MilkAndFluids:
				switch (subTabIndex)
				{
					case 0:
						milkableTable ??= new Widget_MilkableTable(namesToProducts);
						milkableTable.Draw(contentRect);
						break;
					case 1:
						milkTagsTable ??= new Widget_MilkTagsTable(namesToProducts, productsToTags);
						milkTagsTable.Draw(contentRect);
						break;
					case 2:
						raceOverridesWidget ??= new Widget_RaceOverrides();
						raceOverridesWidget.Draw(contentRect);
						break;
					case 3:
						cumpilationSettings ??= new Widget_CumpilationSettings();
						cumpilationSettings.Draw(contentRect);
						break;
				}
				break;
			case (int)MainTabIndex.Breastfeed:
				// subTabIndex 0=总览，1/2/3=人形/动物/机械族 → DrawTab(contentRect, subTabIndex-1) 即 index 0/1/2
				breastfeedSettings ??= new Widget_BreastfeedSettings(humanlikeBreastfeed, animalBreastfeed, mechanoidBreastfeed);
				if (subTabIndex == 0)
					breastfeedSettings.DrawOverview(contentRect);
				else
					breastfeedSettings.DrawTab(contentRect, subTabIndex - 1);
				break;
			case (int)MainTabIndex.HealthAndRisk:
				advancedSettings ??= new Widget_AdvancedSettings();
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.HealthAndRisk, subTabIndex);
				break;
			case (int)MainTabIndex.EfficiencyAndInterface:
				advancedSettings ??= new Widget_AdvancedSettings();
				if (subTabIndex == 0)
				{
					// 身份与菜单区块固定高度，便于后续扩展；下方需留足空间给「按身份默认」表格，避免 belowRect 高度为负导致无内容
					const float IdentitySectionHeightMax = 280f;
					const float MinBelowHeight = 140f;
					const float Gap = 10f;
					float identitySectionHeight = Mathf.Min(IdentitySectionHeightMax, Mathf.Max(0f, contentRect.height - MinBelowHeight - Gap));
					Rect topRect = new Rect(contentRect.x, contentRect.y, contentRect.width, identitySectionHeight);
					if (identitySectionHeight > 0f)
						advancedSettings.DrawSection(topRect, (int)MainTabIndex.EfficiencyAndInterface, 0);
					float belowHeight = Mathf.Max(0f, contentRect.height - identitySectionHeight - Gap);
					if (belowHeight > 0f)
					{
						Rect belowRect = new Rect(contentRect.x, contentRect.y + identitySectionHeight + Gap, contentRect.width, belowHeight);
						defaultSettingWidget ??= new Widget_DefaultSetting(colonistSetting, slaveSetting, prisonerSetting, animalSetting, mechSetting, entitySetting);
						defaultSettingWidget.Draw(belowRect);
					}
				}
				else
					advancedSettings.DrawSection(contentRect, (int)MainTabIndex.EfficiencyAndInterface, 1);
				break;
			case (int)MainTabIndex.IntegrationAndAdvanced:
				if (subTabIndex == 0 && ModLister.GetModWithIdentifier("rim.job.world") != null)
				{
					advancedSettings ??= new Widget_AdvancedSettings();
					advancedSettings.DrawSection(contentRect, (int)MainTabIndex.IntegrationAndAdvanced, 0);
				}
				else if (subTabIndex == 1)
				{
					advancedSettings ??= new Widget_AdvancedSettings();
					advancedSettings.DrawSection(contentRect, (int)MainTabIndex.IntegrationAndAdvanced, 1);
				}
				else
				{
					float devHeight = Prefs.DevMode ? 220f : 0f;
					Rect geneRect = new Rect(contentRect.x, contentRect.y, contentRect.width, contentRect.height - devHeight);
					geneSetting ??= new Widget_GeneSetting(genes);
					geneSetting.Draw(geneRect);
					if (Prefs.DevMode)
					{
						Rect devRect = new Rect(contentRect.x, contentRect.yMax - devHeight, contentRect.width, devHeight - 10f);
						advancedSettings ??= new Widget_AdvancedSettings();
						advancedSettings.DrawDevModeSection(devRect);
					}
				}
				break;
		}
	}

	private List<TabRecord> GetSubTabs()
	{
		return mainTabIndex switch
		{
			(int)MainTabIndex.MilkAndFluids => new List<TabRecord>
			{
				new("EM.SubTab.Milk".Translate(), () => subTabIndex = 0, subTabIndex == 0),
				new("EM.SubTab.MilkTags".Translate(), () => subTabIndex = 1, subTabIndex == 1),
				new("EM.SubTab.RaceOverrides".Translate(), () => subTabIndex = 2, subTabIndex == 2),
				new("EM.SubTab.Fluids".Translate(), () => subTabIndex = 3, subTabIndex == 3)
			},
			(int)MainTabIndex.Breastfeed => new List<TabRecord>
			{
				new("EM.SubTab.BreastfeedOverview".Translate(), () => subTabIndex = 0, subTabIndex == 0),
				new(Lang.Colonist.CapitalizeFirst(), () => subTabIndex = 1, subTabIndex == 1),
				new(Lang.Animal.CapitalizeFirst(), () => subTabIndex = 2, subTabIndex == 2),
				new(Lang.Mechanoid.CapitalizeFirst(), () => subTabIndex = 3, subTabIndex == 3)
			},
			(int)MainTabIndex.HealthAndRisk => new List<TabRecord>
			{
				new("EM.SubTab.Mastitis".Translate(), () => subTabIndex = 0, subTabIndex == 0),
				new("EM.SubTab.DBH".Translate(), () => subTabIndex = 1, subTabIndex == 1),
				new("EM.SubTab.ToleranceOverflow".Translate(), () => subTabIndex = 2, subTabIndex == 2),
				new("EM.SubTab.LoadFromDef".Translate(), () => subTabIndex = 3, subTabIndex == 3)
			},
			(int)MainTabIndex.EfficiencyAndInterface => new List<TabRecord>
			{
				new("EM.SubTab.IdentityAndMenu".Translate(), () => subTabIndex = 0, subTabIndex == 0),
				new("EM.SubTab.BreastPool".Translate(), () => subTabIndex = 1, subTabIndex == 1)
			},
			(int)MainTabIndex.IntegrationAndAdvanced => new List<TabRecord>
			{
				new("EM.SubTab.RJW".Translate(), () => subTabIndex = 0, subTabIndex == 0),
				new("EM.SubTab.DBH".Translate(), () => subTabIndex = 1, subTabIndex == 1),
				new("EM.SubTab.GenesAndAdvanced".Translate(), () => subTabIndex = 2, subTabIndex == 2)
			},
			_ => new List<TabRecord>()
		};
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
		HediffDefOf.Lactating.maxSeverity = 100f; // 允许 severity 自由叠加
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
		// 人奶：默认开启显示动物名，产主限制「谁可以吃」才生效
		ThingDef humanMilkDef = DefDatabase<ThingDef>.GetNamedSilentFail("EM_HumanMilk");
		if (humanMilkDef != null && !productsToTags.ContainsKey("EM_HumanMilk"))
			productsToTags.Add("EM_HumanMilk", new MilkTag("EM_HumanMilk", true, false));
		// 7.10: rjw-genes cum milk etc. — ensure breast-sourced cum gets producer so allowedConsumers apply
		ThingDef cumDef = DefDatabase<ThingDef>.GetNamedSilentFail("Cumpilation_Cum");
		if (cumDef != null && !productsToTags.ContainsKey("Cumpilation_Cum"))
			productsToTags.Add("Cumpilation_Cum", new MilkTag("Cumpilation_Cum", true, false));
		foreach (Pawn pawn in PawnsFinder.AllMaps)
		{
			pawn.LactatingHediffWithComps()?.SetDirty();
		}
	}
	private void UpdateEqualMilkableComp(ThingDef pawnDef)
	{
		// 设计原则 3：不重复定义「种族是否产奶」底层规则；仅用 namesToProducts 与设置做开关，默认值来自 GetDefaultMilkProduct(Def)。
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

	/// <summary>设计原则 3/4：默认产奶开关与奶量来自 Def（Humanlike/体型），不手写底层规则；实际开关由 namesToProducts + 设置决定。</summary>
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
	/// <summary>建议 22：从 MilkCumDefaultsDef 加载关键默认值到当前设置；若 XML 未加载则使用内置默认值（其他 mod 可 patch GetBuiltinDefaults）。</summary>
	public static void ApplyDefaultsFromDef()
	{
		var def = DefDatabase<MilkCumDefaultsDef>.GetNamedSilentFail("EM_Defaults") ?? MilkCumDefaultsDef.GetBuiltinDefaults();
		if (def == null) return;
		baselineMilkDurationDays = def.baselineMilkDurationDays;
		birthInducedMilkDurationDays = def.birthInducedMilkDurationDays;
		allowMastitis = def.allowMastitis;
		mastitisBaseMtbDays = def.mastitisBaseMtbDays;
		overFullnessRiskMultiplier = def.overFullnessRiskMultiplier;
		hygieneRiskMultiplier = def.hygieneRiskMultiplier;
		allowToleranceAffectMilk = def.allowToleranceAffectMilk;
		toleranceFlowImpactExponent = def.toleranceFlowImpactExponent;
		overflowFilthDefName = def.overflowFilthDefName ?? "Filth_Vomit";
		aiPreferHighFullnessTargets = def.aiPreferHighFullnessTargets;
		defaultFlowMultiplierForHumanlike = def.defaultFlowMultiplierForHumanlike;
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

	/// <summary>奶标签联动：产主限制「谁可以吃我的奶制品」仅当奶标签里对应物品种类开启「显示动物名」时生效。本方法为 true 时，产主限制窗口显示该区块。</summary>
	internal static bool IsProducerRestrictionConsumersEffectiveForMilkProducts()
	{
		if (productsToTags.TryGetValue("EM_HumanMilk", out MilkTag t) && t.TagPawn) return true;
		if (productsToTags.TryGetValue("Milk", out t) && t.TagPawn) return true;
		return false;
	}

	/// <summary>奶标签联动：产主限制「谁可以吃我的精液制品」仅当奶标签里精液开启「显示动物名」时生效。本方法为 true 时，产主限制窗口显示该区块。</summary>
	internal static bool IsProducerRestrictionConsumersEffectiveForCumProducts()
	{
		return productsToTags.TryGetValue("Cumpilation_Cum", out MilkTag t) && t.TagPawn;
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
	/// <summary>当前催乳素耐受严重度 t ∈ [0,1]；完全由游戏内 Hediff/Comp 决定。</summary>
	/// <summary>3.3 动物差异化：种族对催乳药物进水倍率，未配置则 1。</summary>
	internal static float GetRaceDrugDeltaSMultiplier(Pawn pawn)
	{
		if (pawn?.def?.defName == null || raceDrugDeltaSMultiplierDefNames == null || raceDrugDeltaSMultiplierValues == null) return 1f;
		int i = raceDrugDeltaSMultiplierDefNames.IndexOf(pawn.def.defName);
		if (i < 0 || i >= raceDrugDeltaSMultiplierValues.Count) return 1f;
		return Mathf.Clamp(raceDrugDeltaSMultiplierValues[i], 0.1f, 3f);
	}

	internal static float GetProlactinTolerance(Pawn pawn)
		=> pawn?.health?.hediffSet?.GetFirstHediffOfDef(MilkCumDefOf.EM_Prolactin_Tolerance)?.Severity ?? 0f;

	/// <summary>统一耐受系数：E_tol(t) = max(1 − t, 0.05)。启用耐受动态时由 comp 的 E 计算。</summary>
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

	/// <summary>耐受动态：由 mod 维护的 E 得到 E_tol = [max(1−E, 0.05)]^exponent。</summary>
	internal static float GetProlactinToleranceFactorFromE(float E)
	{
		if (!allowToleranceAffectMilk) return 1f;
		float e = Mathf.Max(1f - Mathf.Clamp01(E), PoolModelConstants.EffectiveDrugFactorMin);
		return Mathf.Pow(e, Mathf.Clamp(toleranceFlowImpactExponent, 0.1f, 3f));
	}

	/// <summary>统一耐受系数（按严重度 t）：E_tol(t) = [max(1 − t, 0.05)]^exponent；allowToleranceAffectMilk 关闭时恒为 1。</summary>
	internal static float GetProlactinToleranceFactor(float toleranceSeverity)
	{
		if (!allowToleranceAffectMilk) return 1f;
		float e = Mathf.Max(1f - toleranceSeverity, PoolModelConstants.EffectiveDrugFactorMin);
		return Mathf.Pow(e, Mathf.Clamp(toleranceFlowImpactExponent, 0.1f, 3f));
	}

	#endregion
}

/// <summary>建议 13：乳腺炎/风险与耐受相关设置分组，便于序列化与 UI；ExposeData 仍写旧 key 以兼容存档。</summary>
public class MilkRiskSettings : IExposable
{
	public bool allowMastitis = true;
	public float mastitisBaseMtbDays = 1.5f;
	public float overFullnessRiskMultiplier = 1.5f;
	public float hygieneRiskMultiplier = 1f;
	public bool allowToleranceAffectMilk = true;
	public float toleranceFlowImpactExponent = 1f;
	/// <summary>建议 8：人形/动物乳腺炎 MTB 乘数，便于区分平衡。</summary>
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
