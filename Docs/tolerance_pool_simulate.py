# 耐受+水池模拟（当前代码逻辑）
# 进水：ΔL = Δs × C_dose，其中 Δs = rawSeverity × E_tol(t_before)（只乘一次 E）
# 数据来源：ChemicalDefs.xml, HediffDefs.xml, LactatingItems.xml, PoolModelConstants

# 常量（与当前代码一致）
TOLERANCE_PER_DOSE_PROLACTIN = 0.044
TOLERANCE_PER_DOSE_LUCILACTIN = 0.176
TOLERANCE_PER_DAY = -0.015
E_MIN = 0.05
BASE_T = 3.0
K = 0.01
L_EPSILON = 1e-5
DOSE_TO_L = 1.0

def E_tol(t):
    return max(1.0 - t, E_MIN)

def daily_decay(L, t):
    e = E_tol(t)
    if e <= 0:
        return 0.0
    return 1.0 / (BASE_T * e) + K * L

def run_day_decay_only(t, L):
    """只做日衰减（不再吃药），返回 (t_end, L_end)。"""
    t = max(0.0, t + TOLERANCE_PER_DAY)
    D = daily_decay(L, t)
    L = max(0.0, L - D)
    if L < L_EPSILON:
        L = 0.0
    return t, L

def run_day(t, L, doses_prolactin=0, doses_lucilactin=0):
    """一天内：先日衰减，再吃 doses 次药。Prolactin raw=0.5, Lucilactin raw=2.0；进水 ΔL = Δs（Δs=raw×E_tol）。"""
    t = max(0.0, t + TOLERANCE_PER_DAY)
    D = daily_decay(L, t)
    L = max(0.0, L - D)
    if L < L_EPSILON:
        L = 0.0
    for _ in range(doses_prolactin):
        t_before = t
        eff = E_tol(t_before)
        delta_s = 0.5 * eff
        L += delta_s * DOSE_TO_L
        t += TOLERANCE_PER_DOSE_PROLACTIN
        t = min(1.0, t)
    for _ in range(doses_lucilactin):
        t_before = t
        eff = E_tol(t_before)
        delta_s = 2.0 * eff
        L += delta_s * DOSE_TO_L
        t += TOLERANCE_PER_DOSE_LUCILACTIN
        t = min(1.0, t)
    return t, L

def simulate_one_dose(raw_severity, tolerance_per_dose, label):
    """只打一针后不再吃药：返回 (初始L, 每日 (day, t, L) 直到 L<=0)。"""
    t = 0.0
    L = raw_severity * E_tol(0.0) * DOSE_TO_L  # 一针：Δs = raw × E_tol(0), ΔL = Δs
    t += tolerance_per_dose
    day = 0
    history = [(day, t, L)]
    while L > L_EPSILON and day < 365:
        day += 1
        t, L = run_day_decay_only(t, L)
        history.append((day, t, L))
    return history

def main():
    print("=" * 70)
    print("只打一针 模拟：泌乳持续时间（当前公式 ΔL = Δs×C_dose，Δs = raw×E_tol）")
    print("=" * 70)

    for raw, tol_per_dose, name in [
        (0.5, TOLERANCE_PER_DOSE_PROLACTIN, "Prolactin (0.5)"),
        (2.0, TOLERANCE_PER_DOSE_LUCILACTIN, "Lucilactin (2.0)"),
    ]:
        hist = simulate_one_dose(raw, tol_per_dose, name)
        L0 = hist[0][2]
        days_until_end = len(hist) - 1
        print(f"\n【{name}】 一针后")
        print(f"  初始 L = {L0:.3f}，耐受 t = {hist[0][1]:.3f}")
        print(f"  泌乳持续约 {days_until_end} 天（L 降至 0 为止）")
        print(f"  前几日 L 变化：")
        for day, t, L in hist[: min(8, len(hist))]:
            print(f"    第 {day} 天末  t={t:.3f}  L={L:.3f}")
        if len(hist) > 8:
            print(f"    ...")
            print(f"    第 {days_until_end} 天末  L≈0，泌乳结束")

    print("\n" + "=" * 70)
    print("说明：L=1 约等于「满池」一池的等效容量；L 每日衰减 D = 1/(3×E_tol) + 0.01×L")
    print("=" * 70)

if __name__ == "__main__":
    main()
