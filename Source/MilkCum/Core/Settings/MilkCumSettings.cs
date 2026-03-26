using System.Collections.Generic;
using System.Linq;
using MilkCum.Fluids.Shared.Comps;
using MilkCum.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace MilkCum.Core.Settings;

/// <summary>乳池单侧基容量如何从 RJW 乳房 Hediff 推导（再乘 <see cref="MilkCumSettings.rjwBreastCapacityCoefficient"/> 的语义见各模式说明）。仅存三档：纯严重度 / 纯重量 / 纯体积。</summary>
public enum RjwBreastPoolCapacityMode : byte
{
    /// <summary>严重度×系数。</summary>
    Severity = 0,
    /// <summary>RJW PartSize 估算乳房重量（kg）×系数；无有效重量数据时为 0（不进池）。</summary>
    RjwBreastWeight = 1,
    /// <summary>RJW <c>BreastSize.volume</c>（升）×系数；无有效体积数据时为 0（不进池）。新安装默认。</summary>
    RjwBreastVolume = 2,
}

/// <summary>专业级 UI：按系统类型分层。核心机制 / 健康风险 / 权限规则 / 数值平衡 / 模组联动 / 数据种族 / 调试工具（仅 DevMode）。</summary>
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
	/// <summary>挤奶流速基准：baseFlowPerSecond = 60/基准值（池单位/秒）。默认 60 → 满池约 1 瓶/秒（现实时间）；调大则变慢。</summary>
	public static float milkingWorkTotalBase = 60f;
	/// <summary>按容量量化：吸奶有效时间随喂奶量 MilkAmount 的系数，effectiveTime *= (1 + 系数*(MilkAmount-1))，限制在 [0.5, 2]。</summary>
	public static float breastfeedCapacityFactor = 0.1f;
	public static bool femaleAnimalAdultAlwaysLactating = false;
	public static bool showMechOptions = true;
	public static bool showColonistOptions = true;
	public static bool showSlaveOptions = true;
	public static bool showPrisonerOptions = true;
	public static bool showAnimalOptions = true;
	public static bool showMiscOptions = true;
	/// <summary>默认允许用奶规则（名单为空时）：是否把「子女」算入默认允许。</summary>
	public static bool defaultSucklerIncludeChildren = true;
	/// <summary>默认允许用奶规则：是否把「恋人」算入默认允许。</summary>
	public static bool defaultSucklerIncludeLover = true;
	/// <summary>默认允许用奶规则：是否把「配偶」算入默认允许。</summary>
	public static bool defaultSucklerIncludeSpouse = true;
	/// <summary>默认允许用奶规则：是否排除「父母」（母亲/父亲）。</summary>
	public static bool defaultSucklerExcludeParents = true;
	public static float nutritionToEnergyFactor = 100f;
	/// <summary>泌乳额外营养基准：以 150 为 1:1，将每日泌乳进水量折算成额外营养（用于进食上限校正）。</summary>
	public static int lactationExtraNutritionBasis = 150;
	/// <summary>回缩吸收开关：开启时，池内剩余奶量会按下方效率转成营养；关闭时，未被吸奶消耗的部分全部作废。</summary>
	public static bool reabsorbNutritionEnabled = true;
	/// <summary>回缩吸收效率，0~1：吸收的池单位折成营养的比例，默认 0.5 避免满池挂机过强。</summary>
	public static float reabsorbNutritionEfficiency = 0.5f;
	/// <summary>DevMode 且勾选时，每 60 tick 输出泌乳小人的营养/奶池/回缩/吸奶明细到日志。</summary>
	public static bool lactationPoolTickLog = false;
	/// <summary>DevMode 时打开：输出每次挤奶操作（手动/机械）入口、结果等明细日志，用于调试与 AI 测试。</summary>
	public static bool milkingActionLog = false;
	/// <summary>DevMode 时打开：输出泌乳关键流程日志（进水/衰减/溢出等），关闭可减少刷屏；仅用于细查 PoolTickLog 不方便的情况。</summary>
	public static bool lactationLog = true;
	/// <summary>DevMode 时打开：每次药物带来进水（AddFromDrug）时输出调试日志，包含 ΔS、进水量与剩余时间变化。</summary>
	public static bool lactationDrugIntakeLog = false;
	/// <summary>仅在 DevMode 且 lactationLog 打开时调用：辅助输出与 L/池/污物/衰减表相关的详细日志。</summary>
	public static void LactationLog(string message)
	{
		if (Verse.Prefs.DevMode && lactationLog && !string.IsNullOrEmpty(message))
			Verse.Log.Message("[MilkCum.Lactation] " + message);
	}
	/// <summary>仅当 DevMode 且 lactationPoolTickLog 为 true 时输出，用于每步营养/奶池/回缩/吸奶明细。</summary>
	public static void PoolTickLog(string message)
	{
		if (Verse.Prefs.DevMode && lactationPoolTickLog && !string.IsNullOrEmpty(message))
			Verse.Log.Message("[MilkCum.Pool] " + message);
	}
	public static HumanlikeBreastfeed humanlikeBreastfeed = new();
	public static AnimalBreastfeed animalBreastfeed = new();
	public static MechanoidBreastfeed mechanoidBreastfeed = new();
	// 泌乳期意义/操纵/移动收益：开关与百分比(0~0.20 = 0%~20%)
	public static bool lactatingGainEnabled = true;
	public static float lactatingGainCapModPercent = 0.10f;
	// RJW 联动（仅当 rim.job.world 激活时生效）。
	public static bool rjwBreastSizeEnabled = true;
	/// <summary>RJW 胸围容量系数：单侧基容量 =（由 <see cref="rjwBreastPoolCapacityMode"/> 选定的严重度/体积/重量等基值）× 本系数，默认 2。</summary>
	public static float rjwBreastCapacityCoefficient = 2f;
	/// <summary>乳池单侧基容量来源：默认 <see cref="RjwBreastPoolCapacityMode.RjwBreastVolume"/>（体积/重量无有效 RJW 尺寸则为 0）；流速仍独立走 <c>GetFluidMultiplier</c>。</summary>
	public static RjwBreastPoolCapacityMode rjwBreastPoolCapacityMode = RjwBreastPoolCapacityMode.RjwBreastVolume;
	/// <summary>RJW 乳房阶段标签含 “Nipple” 时，对进水/高潮产液流速倍率施加的百分比修正（0=关闭）；限制在约 ±15% 内。</summary>
	public static float rjwNippleStageFlowBonusPercent = 0f;
	/// <summary>泌乳期临时体型增益（0~1 段）：RJW Severity 增量，满 L 时生效；默认 0.15。</summary>
	public static float rjwLactatingSeverityBonus = 0.15f;
	/// <summary>池 1→1.2 撑大段对应的 RJW Severity 增量；默认 0.05。</summary>
	public static float rjwLactatingStretchSeverityBonus = 0.05f;
	/// <summary>是否启用「因泌乳永久撑大」：每达到一定泌乳时长即通过 RJW 的 SetSeverity 永久增加乳房体型，不在本 mod 内维护单独数字。</summary>
	public static bool rjwPermanentBreastGainFromLactationEnabled = false;
	/// <summary>每多少游戏日泌乳触发一次永久体型增益（每里程碑对每乳调用 comp.SetSeverity(Min(1f, base + delta))）。</summary>
	public static float rjwPermanentBreastGainDaysPerMilestone = 10f;
	/// <summary>每次里程碑永久增加的 Severity 量（RJW 定义），上限 1。</summary>
	public static float rjwPermanentBreastGainSeverityDelta = 0.03f;
	public static bool rjwLustFromNursingEnabled = true;
	public static bool rjwSexNeedLactatingBonusEnabled = true;
	public static bool rjwSexSatisfactionAfterNursingEnabled = true;
	/// <summary>泌乳期对 RJW 生育能力的倍率（0~1），1 = 不影响，0.85 = 略微降低怀孕率。</summary>
	public static float rjwLactationFertilityFactor = 0.85f;
	/// <summary>是否在性行为描述中加入「泌乳中」提示。</summary>
	public static bool rjwLactatingInSexDescriptionEnabled = true;
	/// <summary>3.2：性行为结束后是否给泌乳增加一小段额外进水/持续时间加成，可选。</summary>
	public static bool rjwSexAddsLactationBoost = false;
	public static float rjwSexLactationBoostDeltaS = 0.15f;
	// 乳腺炎计算来源：是否与 Dubs Bad Hygiene 联动；开启时用 Hygiene 需求，否则用房间清洁度。
	public static bool useDubsBadHygieneForMastitis = true;
	// 乳腺炎相关参数可配置：是否启用、基础 MTB（天）、过满与卫生惩罚倍率等。
	// 耐受对泌乳效率的影响：关闭时 E_tol 固定为 1；开启时由 E 或耐受严重度 t 决定（见下方函数）。
	// 建议：将乳腺炎/卫生/耐受相关设置收敛到 MilkRiskSettings，便于 UI 分组与 Scribe 字段组织。
	private static MilkRiskSettings _risk = new MilkRiskSettings();
	private static MilkRiskSettings Risk => _risk ??= new MilkRiskSettings();
	public static bool allowMastitis { get => Risk.allowMastitis; set => Risk.allowMastitis = value; }
	public static float mastitisBaseMtbDays { get => Risk.mastitisBaseMtbDays; set => Risk.mastitisBaseMtbDays = value; }
	public static float overFullnessRiskMultiplier { get => Risk.overFullnessRiskMultiplier; set => Risk.overFullnessRiskMultiplier = value; }
	public static float hygieneRiskMultiplier { get => Risk.hygieneRiskMultiplier; set => Risk.hygieneRiskMultiplier = value; }
	/// <summary>医学贴近：卫生差且（淤积或损伤）时感染风险系数，MTB 再除以此值（&gt;1 更易触发）。</summary>
	public static float mastitisInfectionRiskFactor { get => Risk.mastitisInfectionRiskFactor; set => Risk.mastitisInfectionRiskFactor = value; }
	public static bool allowToleranceAffectMilk { get => Risk.allowToleranceAffectMilk; set => Risk.allowToleranceAffectMilk = value; }
	public static float toleranceFlowImpactExponent { get => Risk.toleranceFlowImpactExponent; set => Risk.toleranceFlowImpactExponent = value; }
	// 耐受动态：dE/dt = μ×L − ν×E；启用时由 mod 维护的 E 计算 E_tol（流速/容量），取代仅用游戏内耐受严重度 t。
	public static bool enableToleranceDynamic = true;
	/// <summary>耐受累积率 μ（每游戏日）：越高，长时间高 L 时 E 上升越快。</summary>
	public static float toleranceDynamicMu = 0.03f;
	/// <summary>耐受衰减率 ν（每游戏日）：越高，停药后耐受自然回落越快。</summary>
	public static float toleranceDynamicNu = 0.08f;
	public static float mastitisMtbDaysMultiplierHumanlike { get => Risk.mastitisMtbDaysMultiplierHumanlike; set => Risk.mastitisMtbDaysMultiplierHumanlike = value; }
	public static float mastitisMtbDaysMultiplierAnimal { get => Risk.mastitisMtbDaysMultiplierAnimal; set => Risk.mastitisMtbDaysMultiplierAnimal = value; }
	// 满池溢出地面污物：Def 名称，空或无效时回退 Filth_Vomit
	public static string overflowFilthDefName = "Filth_Vomit";
	/// <summary>泌乳水平上限（L_cap）：&gt;0 时，吃药超过此值的部分不再增加进水量 L，只增加 Severity（延长泌乳时间），流速由 min(L, cap) 决定，更符合生理（身体需更长时间消耗药效）。0 = 关闭。</summary>
	public static float lactationLevelCap = 0f;
	/// <summary>上限溢出转时间时的倍数：超出 cap 的 ΔL 转为 Severity 时乘以该系数，使「只延长时间」的收益更大（&gt;1 则延长更多天），补偿不提高流速。默认 1.5。</summary>
	public static float lactationLevelCapDurationMultiplier = 1.5f;
	/// <summary>泌乳药物单次在 XML 中对耐受 Hediff 的 Severity 增量（与 Lactating 一并叠加），默认 0.044；若改 XML 请同步更新此处说明。</summary>
	public static float ProlactinToleranceGainPerDose = 0.044f;
	/// <summary>近满/顶满撑大时（≥满池阈值×撑大容量）压力系数下限抬升倍率（0~1）：模拟腺泡持续分泌与渗漏；与关压力曲线组合时可将顶满后 0 流速抬到微量。默认 0.04；存档缺键时读档亦为 0.04。</summary>
	public static float overflowResidualFlowFactor = 0.04f;
	/// <summary>残余压力是否随泌乳量 L、炎症 I 缩放；关闭时仅用 overflowResidualFlowFactor。</summary>
	public static bool overflowResidualDynamicScaling = true;
	/// <summary>L 缩放参考：scaleL = L/(L+refL)，refL 越大则需更高 L 才达到同等残余。</summary>
	public static float overflowResidualLactationRefL = 1f;
	/// <summary>炎症倍率：multI = 1 + 本值×Clamp01(I/I_crit)；I_crit 同炎症设置。</summary>
	public static float overflowResidualInflammationBoost = 0.5f;

	/// <summary>带下限 Logistic：f(P)=f_min+(1−f_min)×1/(1+exp(k×(P−Pc)))，P 大时平滑降压，最小 f_min 永不为零。默认 k=6、Pc=0.9、f_min=0.02，奶量达 90% 才开始往下压。</summary>
	/// <param name="P">该侧满度/该侧最大容量，0～1（可略大于 1 时仍按公式算）。</param>
	public static float GetPressureFactor(float P)
	{
		if (!enablePressureFactor || P <= 0f)
			return 1f;

		float k = Mathf.Clamp(pressureFactorB, 0.5f, 80f);
		float pc = Mathf.Clamp(pressureFactorPc, 0.3f, 1f);
		float fMin = Mathf.Clamp01(pressureFactorMin);

		// 下限 Logistic：永远不低于 f_min，P 越大越接近 f_min。
		float logistic = 1f / (1f + Mathf.Exp(k * (P - pc)));
		return fMin + (1f - fMin) * logistic;
	}

	/// <summary>有效残余压力系数（已含 L/I 动态缩放），上限 Clamp 至 1。</summary>
	public static float GetEffectiveOverflowResidualPressure(float lactationL, float inflammationI)
	{
		float r = Mathf.Clamp01(overflowResidualFlowFactor);
		if (r <= 0f) return 0f;
		if (!overflowResidualDynamicScaling) return r;
		float lRef = Mathf.Max(0.01f, overflowResidualLactationRefL);
		float scaleL = lactationL <= 0f ? 0f : lactationL / (lactationL + lRef);
		float crit = Mathf.Max(0.01f, inflammationCrit);
		float boost = Mathf.Clamp(overflowResidualInflammationBoost, 0f, 3f);
		float scaleI = 1f + boost * Mathf.Clamp01(inflammationI / crit);
		return Mathf.Clamp01(r * scaleL * scaleI);
	}

	/// <summary>当该侧池量 ≥ 满池阈值×撑大容量时，将压力系数至少抬到有效残余值；进水与 UI 压力行共用。</summary>
	public static void ApplyOverflowResidualFlow(ref float pressureFactor, float currentFullness, float stretchCap, float lactationL, float inflammationI)
	{
		float rEff = GetEffectiveOverflowResidualPressure(lactationL, inflammationI);
		if (rEff <= 0f || stretchCap < 0.001f) return;
		if (currentFullness >= stretchCap * PoolModelConstants.FullnessThresholdFactor)
			pressureFactor = Mathf.Max(pressureFactor, rEff);
	}

	/// <summary>驱动强度模型：有效驱动力 D_eff = L×H(L)，H(L)=1−exp(−aL)；如 L_ref&gt;0 则做归一化：D_eff = L×(1−e^{−aL})/(1−e^{−aL_ref})，高 L 时趋于饱和。</summary>
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
	// AI 挤奶工作：是否优先选择更满的一侧作为目标（低满度的会先去找更满的一侧）。 
	public static bool aiPreferHighFullnessTargets = true;
	// 物种兼容表：允许名单（defName 在此列表中视为始终可泌乳）；禁止名单（defName 在此列表中视为永不泌乳）。
	public static List<string> raceCanAlwaysLactate = new();
	public static List<string> raceCannotLactate = new();
	// 人形种族默认流速倍率：2 = 在标准体型、乳量等级 1 时约等于一次就能挤空；与 RJW/其他泌乳类 mod 联动时也可调。
	public static float defaultFlowMultiplierForHumanlike = 2f;
	// 压力因子模型（模块 2.1）：启用时流速乘以 PressureFactor(P)，带下限 Logistic；默认 Pc=0.9、k=6、f_min=0.02，奶量达到 90% 才开始明显压流速。
	public static bool enablePressureFactor = true;
	public static float pressureFactorPc = 0.9f;
	public static float pressureFactorB = 6f;
	/// <summary>压力因子下限 f_min：挤奶时产量倍率不会低于该值，避免「一点不出奶」；推荐 0.02～0.15。</summary>
	public static float pressureFactorMin = 0.02f;
	// 乳汁射出反射模型：状态量 R；启用时流速乘以 R，每 60 tick 数值衰减，刺激时提高。
	public static bool enableLetdownReflex = true;
	/// <summary>射出反射衰减率 λ（每分钟）：R_new = R × exp(−λΔt)。</summary>
	public static float letdownReflexDecayLambda = 0.03f;
	/// <summary>每次刺激/持续刺激时 R 的增量 ΔR，叠加后 Clamp 至 1；设高一点（如 1）可模拟一次迅速射奶。</summary>
	public static float letdownReflexStimulusDeltaR = 0.45f;
	/// <summary>射出反射的乘数上限：流速倍率 = 1 + R×(本值−1)；R=1 时为最大倍率（建议 1.5～2.5），R=0 时为 1。</summary>
	public static float letdownReflexBoostMultiplier = 2f;
	// 炎症模型：每侧 I；淤积项仅当该侧满度/撑大上限 &gt; 阈值；卫生项与淤积程度耦合；排空扣 I。见 Docs/泌乳系统-全部说明。
	public static bool enableInflammationModel = true;
	/// <summary>淤积强度系数：该项 = α×max(0,P−P_stasis)^exp，P=该侧奶量/该侧撑大容量。</summary>
	public static float inflammationAlpha = 2.2f;
	public static float inflammationBeta = 0.15f;
	public static float inflammationGamma = 0.2f;
	public static float inflammationRho = 0.05f;
	public static float inflammationCrit = 1f;
	/// <summary>单侧淤积阈值：P（对该侧撑大上限归一）超过此值才开始明显积 I。</summary>
	public static float inflammationStasisFullnessThreshold = 0.85f;
	/// <summary>淤积超出部分的指数：2=平方，1=线性。</summary>
	public static float inflammationStasisExponent = 2f;
	/// <summary>无淤积时卫生项乘数（0~1）：模拟「脏环境需叠加淤积/破损才易转感染」。</summary>
	public static float inflammationHygieneBaselineFactor = 0.2f;
	/// <summary>排空缓解：每事件每侧 I 减少 min(上限, 本值×(移出量/该侧撑大上限))。</summary>
	public static float inflammationDrainReliefScale = 0.35f;
	/// <summary>单次排空事件单侧 I 最大下降量。</summary>
	public static float inflammationDrainReliefMaxPerEvent = 0.14f;
	/// <summary>炎症对 L 衰减的额外系数 η：L += −η×I。</summary>
	public static float lactationDecayInflammationEta = 0.1f;
	// 刺激累积模型：挤奶/吸奶时 L 会短期上升并带有上限，避免无限循环堆高。
	public static float milkingLStimulusPerEvent = 0.03f;
	public static float milkingLStimulusCapPerEvent = 0.05f;
	public static float milkingLStimulusCapPerDay = 0.2f;
	// 炎症与刺激模型：有炎症时 L 衰减更快；刺激则在短时间内提高 L，上方三个参数控制上限与累积速度。
	// 驱动力模型：药效/泌乳驱动由 D_eff 决定，低 L 时几乎线性，高 L 时趋于饱和（参见 GetEffectiveDrive）。 
	public static bool enableHormoneSaturation = true;
	/// <summary>H(L) 的饱和系数 a，建议 0.5～2.5。</summary>
	public static float hormoneSaturationA = 1f;
	/// <summary>参考 L_ref 用于归一化：D_eff = L×(1−e^{−aL})/(1−e^{−aL_ref})，让中等 L 更平滑。</summary>
	public static float hormoneSaturationLRef = 1f;
	// 组织适应模型（模块 2.2）：长期高 P 会增加最大容量，长期低 P 会慢慢缩小；dF_max/dt ≈ θ·max(P−0.85,0) − ω·(1−P)。
	public static bool enableTissueAdaptation = true;
	/// <summary>容量增长系数 θ（每游戏日）：P&gt;0.85 时增加最大容量。</summary>
	public static float adaptationTheta = 0.002f;
	/// <summary>缩减系数 ω（每游戏日）：P&lt;1 时缓慢减小最大容量。</summary>
	public static float adaptationOmega = 0.001f;
	/// <summary>适应后容量上限：不超过基础容量的该比例（如 0.2=20%）。</summary>
	public static float adaptationCapMaxRatio = 0.2f;
	// 鍥涘眰妯″瀷锛堥樁娈?.3锛夛細涔虫眮璐ㄩ噺 MilkQuality = f(Hunger, I)銆傚惎鐢ㄦ椂锛氳川閲忛珮鈫掍钩鑵虹値闃堝€兼彁楂橈紱鍙€夊湪 UI 鏄剧ず銆傞粯璁ゅ叧闂紝寰呭姙锛氫骇鍑哄ザ鐗╁搧鐨?QualityCategory銆?
	public static bool enableMilkQuality = false;
	/// <summary>炎症对营养值的惩罚系数：营养值≈原值×(1−本值×Inflammation)，越大则炎症时品质下降越明显。</summary>
	public static float milkQualityInflammationWeight = 0.5f;
	/// <summary>品质对饱食度阈值的保护系数：高品质牛奶可稍微提高 I_crit，减少轻微炎症触发。</summary>
	public static float milkQualityProtectionFactor = 0.5f;
	// 3.3 婊℃睜浜嬩欢锛氭弧姹犺繃涔咃紙绾?1 澶╋級鏃舵槸鍚﹀彂淇℃彁閱?
	public static bool enableFullPoolLetter = true;
	/// <summary>满池信件冷却天数，同一小人两次提醒至少间隔此天数。默认 2。</summary>
	public static float fullPoolLetterCooldownDays = 2f;
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
		Scribe_Values.Look(ref defaultSucklerIncludeChildren, "EM.DefaultSucklerIncludeChildren", true);
		Scribe_Values.Look(ref defaultSucklerIncludeLover, "EM.DefaultSucklerIncludeLover", true);
		Scribe_Values.Look(ref defaultSucklerIncludeSpouse, "EM.DefaultSucklerIncludeSpouse", true);
		Scribe_Values.Look(ref defaultSucklerExcludeParents, "EM.DefaultSucklerExcludeParents", true);
		Scribe_Values.Look(ref nutritionToEnergyFactor, "EM.NutritionToEnergyFactor", 100f);
		Scribe_Values.Look(ref lactationExtraNutritionBasis, "EM.LactationExtraNutritionFactor", 150);
		Scribe_Values.Look(ref reabsorbNutritionEnabled, "EM.ReabsorbNutritionEnabled", true);
		Scribe_Values.Look(ref reabsorbNutritionEfficiency, "EM.ReabsorbNutritionEfficiency", 0.5f);
		Scribe_Values.Look(ref lactatingGainEnabled, "EM.LactatingGainEnabled", true);
		Scribe_Values.Look(ref lactatingGainCapModPercent, "EM.LactatingGainCapModPercent", 0.10f);
		Scribe_Values.Look(ref rjwBreastSizeEnabled, "EM.RjwBreastSizeEnabled", true);
		Scribe_Values.Look(ref rjwBreastCapacityCoefficient, "EM.RjwBreastCapacityCoefficient", 2f);
		Scribe_Values.Look(ref rjwBreastPoolCapacityMode, "EM.RjwBreastPoolCapMode", RjwBreastPoolCapacityMode.RjwBreastVolume);
		rjwBreastPoolCapacityMode = (RjwBreastPoolCapacityMode)Mathf.Clamp((int)rjwBreastPoolCapacityMode, 0, 2);
		Scribe_Values.Look(ref rjwNippleStageFlowBonusPercent, "EM.RjwNippleStageFlowBonusPct", 0f);
		Scribe_Values.Look(ref rjwLactatingSeverityBonus, "EM.RjwLactatingSeverityBonus", 0.15f);
		Scribe_Values.Look(ref rjwLactatingStretchSeverityBonus, "EM.RjwLactatingStretchSeverityBonus", 0.05f);
		Scribe_Values.Look(ref rjwPermanentBreastGainFromLactationEnabled, "EM.RjwPermanentBreastGainFromLactationEnabled", false);
		Scribe_Values.Look(ref rjwPermanentBreastGainDaysPerMilestone, "EM.RjwPermanentBreastGainDaysPerMilestone", 10f);
		Scribe_Values.Look(ref rjwPermanentBreastGainSeverityDelta, "EM.RjwPermanentBreastGainSeverityDelta", 0.03f);
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
		Scribe_Values.Look(ref _risk.mastitisInfectionRiskFactor, "EM.MastitisInfectionRiskFactor", 1.2f);
		Scribe_Values.Look(ref _risk.allowToleranceAffectMilk, "EM.AllowToleranceAffectMilk", true);
		Scribe_Values.Look(ref _risk.toleranceFlowImpactExponent, "EM.ToleranceFlowImpactExponent", 1f);
		Scribe_Values.Look(ref enableToleranceDynamic, "EM.EnableToleranceDynamic", true);
		Scribe_Values.Look(ref toleranceDynamicMu, "EM.ToleranceDynamicMu", 0.03f);
		Scribe_Values.Look(ref toleranceDynamicNu, "EM.ToleranceDynamicNu", 0.08f);
		Scribe_Values.Look(ref _risk.mastitisMtbDaysMultiplierHumanlike, "EM.MastitisMtbDaysMultiplierHumanlike", 1f);
		Scribe_Values.Look(ref _risk.mastitisMtbDaysMultiplierAnimal, "EM.MastitisMtbDaysMultiplierAnimal", 1f);
		Scribe_Values.Look(ref overflowFilthDefName, "EM.OverflowFilthDefName", "Filth_Vomit");
		Scribe_Values.Look(ref lactationLevelCap, "EM.LactationLevelCap", 0f);
		Scribe_Values.Look(ref lactationLevelCapDurationMultiplier, "EM.LactationLevelCapDurationMultiplier", 1.5f);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			lactationLevelCap = Mathf.Clamp(lactationLevelCap, 0f, 100f);
			lactationLevelCapDurationMultiplier = Mathf.Clamp(lactationLevelCapDurationMultiplier, 0.1f, 10f);
			rjwLactatingSeverityBonus = Mathf.Clamp(rjwLactatingSeverityBonus, 0f, 1f);
			rjwLactatingStretchSeverityBonus = Mathf.Clamp(rjwLactatingStretchSeverityBonus, 0f, 1f);
			rjwNippleStageFlowBonusPercent = Mathf.Clamp(rjwNippleStageFlowBonusPercent, -15f, 15f);
		}
		Scribe_Values.Look(ref aiPreferHighFullnessTargets, "EM.AiPreferHighFullnessTargets", true);
		Scribe_Collections.Look(ref raceCanAlwaysLactate, "EM.RaceCanAlwaysLactate", LookMode.Value);
		Scribe_Collections.Look(ref raceCannotLactate, "EM.RaceCannotLactate", LookMode.Value);
		Scribe_Values.Look(ref defaultFlowMultiplierForHumanlike, "EM.DefaultFlowMultiplierForHumanlike", 2f);
		Scribe_Values.Look(ref enablePressureFactor, "EM.EnablePressureFactor", true);
		Scribe_Values.Look(ref pressureFactorPc, "EM.PressureFactorPc", 0.9f);
		Scribe_Values.Look(ref pressureFactorB, "EM.PressureFactorB", 6f);
		Scribe_Values.Look(ref pressureFactorMin, "EM.PressureFactorMin", 0.02f);
		Scribe_Values.Look(ref overflowResidualFlowFactor, "EM.OverflowResidualFlowFactor", 0.04f);
		Scribe_Values.Look(ref overflowResidualDynamicScaling, "EM.OverflowResidualDynamicScaling", true);
		Scribe_Values.Look(ref overflowResidualLactationRefL, "EM.OverflowResidualLactationRefL", 1f);
		Scribe_Values.Look(ref overflowResidualInflammationBoost, "EM.OverflowResidualInflammationBoost", 0.5f);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			overflowResidualFlowFactor = Mathf.Clamp01(overflowResidualFlowFactor);
			overflowResidualLactationRefL = Mathf.Clamp(overflowResidualLactationRefL, 0.01f, 10f);
			overflowResidualInflammationBoost = Mathf.Clamp(overflowResidualInflammationBoost, 0f, 3f);
		}
		Scribe_Values.Look(ref enableLetdownReflex, "EM.EnableLetdownReflex", true);
		Scribe_Values.Look(ref letdownReflexDecayLambda, "EM.LetdownReflexDecayLambda", 0.03f);
		Scribe_Values.Look(ref letdownReflexStimulusDeltaR, "EM.LetdownReflexStimulusDeltaR", 0.45f);
		Scribe_Values.Look(ref letdownReflexBoostMultiplier, "EM.LetdownReflexBoostMultiplier", 2f);
		Scribe_Values.Look(ref enableInflammationModel, "EM.EnableInflammationModel", true);
		Scribe_Values.Look(ref inflammationAlpha, "EM.InflammationAlpha", 2.2f);
		Scribe_Values.Look(ref inflammationBeta, "EM.InflammationBeta", 0.15f);
		Scribe_Values.Look(ref inflammationGamma, "EM.InflammationGamma", 0.2f);
		Scribe_Values.Look(ref inflammationRho, "EM.InflammationRho", 0.05f);
		Scribe_Values.Look(ref inflammationCrit, "EM.InflammationCrit", 1f);
		Scribe_Values.Look(ref inflammationStasisFullnessThreshold, "EM.InflammationStasisFullnessThreshold", 0.85f);
		Scribe_Values.Look(ref inflammationStasisExponent, "EM.InflammationStasisExponent", 2f);
		Scribe_Values.Look(ref inflammationHygieneBaselineFactor, "EM.InflammationHygieneBaselineFactor", 0.2f);
		Scribe_Values.Look(ref inflammationDrainReliefScale, "EM.InflammationDrainReliefScale", 0.35f);
		Scribe_Values.Look(ref inflammationDrainReliefMaxPerEvent, "EM.InflammationDrainReliefMaxPerEvent", 0.14f);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			inflammationStasisFullnessThreshold = Mathf.Clamp(inflammationStasisFullnessThreshold, 0.5f, 0.99f);
			inflammationStasisExponent = Mathf.Clamp(inflammationStasisExponent, 1f, 4f);
			inflammationHygieneBaselineFactor = Mathf.Clamp01(inflammationHygieneBaselineFactor);
			inflammationDrainReliefScale = Mathf.Clamp(inflammationDrainReliefScale, 0f, 2f);
			inflammationDrainReliefMaxPerEvent = Mathf.Clamp(inflammationDrainReliefMaxPerEvent, 0f, 0.5f);
		}
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
		Scribe_Values.Look(ref fullPoolLetterCooldownDays, "EM.FullPoolLetterCooldownDays", 2f);
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

	/// <summary>确保 Scribe_Deep 反序列化得到的对象类型正确，避免类型不匹配或损坏数据造成 InvalidCastException。</summary>
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
		// 防止反序列化类型不一致而抛出 InvalidCastException。
		EnsureScribeDeepTypes();
		inRect.yMin += unitSize;
		// 从主菜单直接打开设置时 PostLoadInit 可能尚未执行，这里兜底初始化以避免 NRE。
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

		// 顶部主 Tab（专业级）：6 个常驻 + 1 个仅 DevMode。
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

		// 子 Tab 随主 Tab 变化。
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

		// 专业级：7 个 Tab 内容分发。
		bool useAdvancedSettings = mainTabIndex == (int)MainTabIndex.HealthRisk || mainTabIndex == (int)MainTabIndex.Permissions
			|| mainTabIndex == (int)MainTabIndex.Balance || mainTabIndex == (int)MainTabIndex.Integrations || mainTabIndex == (int)MainTabIndex.DevTools;
		if (useAdvancedSettings)
			advancedSettings ??= new Widget_AdvancedSettings();
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
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.HealthRisk, subTabIndex);
				break;
			case (int)MainTabIndex.Permissions:
				defaultSettingWidget ??= new Widget_DefaultSetting(colonistSetting, slaveSetting, prisonerSetting, animalSetting, mechSetting, entitySetting);
				if (subTabIndex == 0)
					advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Permissions, 0);
				else
					defaultSettingWidget.Draw(contentRect);
				break;
			case (int)MainTabIndex.Balance:
				advancedSettings.DrawSection(contentRect, (int)MainTabIndex.Balance, 0);
				break;
			case (int)MainTabIndex.Integrations:
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
					geneSetting.Draw(contentRect);
				break;
			case (int)MainTabIndex.DevTools:
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
		HediffDefOf.Lactating.maxSeverity = 100f; // 允许 severity 按公式自由提高到 100。
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
		// 人奶：默认开启「显示生产者」，产线有上限，但可以在标签里关掉以保持兼容。
		ThingDef humanMilkDef = DefDatabase<ThingDef>.GetNamedSilentFail("EM_HumanMilk");
		if (humanMilkDef != null && !productsToTags.ContainsKey("EM_HumanMilk"))
			productsToTags.Add("EM_HumanMilk", new MilkTag("EM_HumanMilk", true, false));
		// 7.10: rjw-genes cum milk etc. — ensure breast-sourced cum gets producer so allowedConsumers apply
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
		// 设计原则 3：不重复定义「该种族是否可泌乳」的底层规则，默认值来自 GetDefaultMilkProduct(Def)，开关由 namesToProducts + 设置控制。
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

	/// <summary>设计原则 3/4：默认泌乳开关与量级全部从 Def（Humanlike/畜牧类）推导，不在代码里手写底层规则；实际开放由 namesToProducts + 设置决定。</summary>
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
		if (pawn?.def == null) return 0f;

		// RJW/兼容层仍可能会调用 PawnMilkPoolExtensions.MilkAmount()。
		// 在统一数据源后，这里优先读取原版 CompProperties_Milkable 的 milkAmount（若定义了 milkDef），否则回退到体型公式。
		CompProperties_Milkable compMilkable = pawn.def.GetCompProperties<CompProperties_Milkable>();
		if (compMilkable?.milkDef != null && compMilkable.milkAmount > 0f)
			return compMilkable.milkAmount;

		if (pawn.def.race?.Humanlike == true)
			return Mathf.FloorToInt(3f * pawn.def.race.baseBodySize / ThingDefOf.Human.race.baseBodySize);

		return Mathf.FloorToInt(14f * pawn.def.race.baseBodySize / ThingDefOf.Cow.race.baseBodySize);
	}
	/// <summary>褰撳墠鍌钩绱犺€愬彈涓ラ噸搴?t 鈭?[0,1]锛涘畬鍏ㄧ敱娓告垙鍐?Hediff/Comp 鍐冲畾銆</summary>
	/// <summary>3.3：按物种对「泌乳药物 ΔS」做乘法修正，raceDrugDeltaSMultiplier；未配置时为 1。</summary>
	internal static float GetRaceDrugDeltaSMultiplier(Pawn pawn)
	{
		if (pawn?.def?.defName == null || raceDrugDeltaSMultiplierDefNames == null || raceDrugDeltaSMultiplierValues == null) return 1f;
		int i = raceDrugDeltaSMultiplierDefNames.IndexOf(pawn.def.defName);
		if (i < 0 || i >= raceDrugDeltaSMultiplierValues.Count) return 1f;
		return Mathf.Clamp(raceDrugDeltaSMultiplierValues[i], 0.1f, 3f);
	}

	internal static float GetProlactinTolerance(Pawn pawn)
		=> pawn?.health?.hediffSet?.GetFirstHediffOfDef(MilkCumDefOf.EM_Prolactin_Tolerance)?.Severity ?? 0f;

	/// <summary>统一耐受因子：E_tol(t) = max(1 − t, 0.05)；启用动态耐受时优先用 comp 的 E。</summary>
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

	/// <summary>统一耐受因子（按严重度 t）：E_tol(t) = [max(1 − t, 0.05)]^exponent；关闭耐受影响时恒为 1。</summary>
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
	/// <summary>医学贴近：卫生差+淤积/损伤时 MTB 再除以此值（感染风险）。</summary>
	public float mastitisInfectionRiskFactor = 1.2f;
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
		Scribe_Values.Look(ref mastitisInfectionRiskFactor, "EM.MastitisInfectionRiskFactor", 1.2f);
		Scribe_Values.Look(ref allowToleranceAffectMilk, "EM.AllowToleranceAffectMilk", true);
		Scribe_Values.Look(ref toleranceFlowImpactExponent, "EM.ToleranceFlowImpactExponent", 1f);
		Scribe_Values.Look(ref mastitisMtbDaysMultiplierHumanlike, "EM.MastitisMtbDaysMultiplierHumanlike", 1f);
		Scribe_Values.Look(ref mastitisMtbDaysMultiplierAnimal, "EM.MastitisMtbDaysMultiplierAnimal", 1f);
	}
}
