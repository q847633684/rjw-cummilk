# 吃药 n 次、不同时间 —— 药效剩余时间与泌乳量 L 模拟
# 与当前代码一致：吸收延迟 0.25 天、L 每 200 tick 衰减、ΔL = raw×E_tol(t_before)
# 常量：PoolModelConstants、LactatingItems.xml、HediffDefs（耐受）

from __future__ import print_function

# 常量（与 tolerance_pool_simulate + 代码一致）
ABSORPTION_DELAY_DAYS = 15000 / 60000.0   # 0.25 天
TICKS_PER_DAY = 60000
STEP_TICKS = 200
STEP_DAYS = STEP_TICKS / float(TICKS_PER_DAY)  # 1/300 天

TOLERANCE_PER_DOSE_PROLACTIN = 0.044
TOLERANCE_PER_DOSE_LUCILACTIN = 0.176
TOLERANCE_PER_DAY = -0.015
E_MIN = 0.05
BASE_T = 3.0
K = 0.01
L_EPSILON = 1e-5
DOSE_TO_L = 1.0

# 药物预设
PROLACTIN = ("Prolactin", 0.5, TOLERANCE_PER_DOSE_PROLACTIN)
LUCILACTIN = ("Lucilactin", 2.0, TOLERANCE_PER_DOSE_LUCILACTIN)


def E_tol(t):
    return max(1.0 - t, E_MIN)


def D(L, t):
    """日衰减量 D = 1/(B_T*E) + k*L"""
    e = E_tol(t)
    return (1.0 / (BASE_T * e)) + K * L


def remaining_days_approx(L, t):
    """瞬时估算：按当前 L、t 的 D 算 L/D 得到约等于剩余有效天数（天）"""
    if L <= L_EPSILON:
        return 0.0
    d = D(L, t)
    if d <= 0:
        return 999.0
    return L / d


def step_decay(L, t):
    """单步 200 tick：先耐受自然恢复（按步长比例），再 L 衰减。返回 (t_new, L_new)。"""
    t_next = max(0.0, t + TOLERANCE_PER_DAY * STEP_DAYS)
    d = D(L, t)
    L_next = max(0.0, L - d * STEP_DAYS)
    if L_next < L_EPSILON:
        L_next = 0.0
    return t_next, L_next


def apply_dose_at(L, t, raw_severity, tolerance_per_dose):
    """在时刻 (吸收后) 施加一剂：用当前 t 算 E_tol，L += raw*E_tol；t += tolerance_per_dose。返回 (t_new, L_new)。"""
    e = E_tol(t)
    delta_L = raw_severity * e * DOSE_TO_L
    L_new = L + delta_L
    t_new = min(1.0, t + tolerance_per_dose)
    return t_new, L_new


def simulate(dose_times, drug_preset, absorption_delay=ABSORPTION_DELAY_DAYS, max_days=30.0, sample_interval_days=0.5):
    """
    dose_times: 服药时刻列表（游戏日），如 [0, 0.5, 1, 2]
    每剂在 dose_times[i] 吃下，实际进水在 dose_times[i] + absorption_delay
    drug_preset: (name, raw_severity, tolerance_per_dose)
    返回: (rows, name), rows = [(day, L, t, E_tol, remaining_days, label)]
    """
    name, raw_severity, tolerance_per_dose = drug_preset
    events = []
    for d in dose_times:
        events.append((d, "服药"))
        events.append((d + absorption_delay, "吸收生效", raw_severity, tolerance_per_dose))
    events.sort(key=lambda x: (x[0], 0 if x[1] == "服药" else 1))

    L = 0.0
    t = 0.0
    day = 0.0
    next_event_idx = 0
    next_sample_day = 0.0
    out = []
    steps_per_sample = max(1, int(round(sample_interval_days / STEP_DAYS)))
    step_count = 0

    while day <= max_days:
        while next_event_idx < len(events) and events[next_event_idx][0] <= day + STEP_DAYS * 0.5:
            ev = events[next_event_idx]
            if len(ev) >= 4 and ev[1] == "吸收生效":
                _, _, raw, tol_per = ev
                t, L = apply_dose_at(L, t, raw, tol_per)
                out.append((ev[0], L, t, E_tol(t), remaining_days_approx(L, t), "吸收生效"))
            next_event_idx += 1

        t, L = step_decay(L, t)

        step_count += 1
        if step_count % steps_per_sample == 0 and day >= next_sample_day - 1e-6:
            out.append((day, L, t, E_tol(t), remaining_days_approx(L, t), "采样"))
            next_sample_day += sample_interval_days

        day += STEP_DAYS
        if L <= 0.0 and next_event_idx >= len(events):
            break

    return out, name


def print_table(rows, drug_name, dose_times):
    print("\n【{}】 服药时刻(天) = {}".format(drug_name, dose_times))
    print("-" * 85)
    print("{:>10} | {:>8} | {:>8} | {:>8} | {:>14} | {}".format(
        "时间(天)", "L", "耐受t", "E_tol", "剩余有效天数(约)", "备注"))
    print("-" * 85)
    for day, L, tol, e, rem, label in rows:
        print("{:>10.2f} | {:>8.3f} | {:>8.3f} | {:>8.3f} | {:>14.2f} | {}".format(
            day, L, tol, e, rem, label))
    print("-" * 85)


def main():
    print("=" * 90)
    print("吃药 n 次、不同时间 —— 药效剩余时间与泌乳量 L 模拟")
    print("公式：吸收延迟 0.25 天；ΔL = raw×E_tol(t_before)；L 每 200 tick 衰减 D = 1/(3×E)+0.01×L")
    print("=" * 90)

    # 场景 1：只吃 1 次（第 0 天）
    for drug in [PROLACTIN, LUCILACTIN]:
        dose_times = [0.0]
        rows, name = simulate(dose_times, drug, max_days=10.0, sample_interval_days=0.5)
        print_table(rows, name, dose_times)

    # 场景 2：吃 3 次，间隔 0.5 天（0, 0.5, 1）
    dose_times = [0.0, 0.5, 1.0]
    rows, name = simulate(dose_times, PROLACTIN, max_days=8.0, sample_interval_days=0.5)
    print_table(rows, PROLACTIN[0], dose_times)

    # 场景 3：吃 5 次，每天 1 次（0,1,2,3,4）
    dose_times = [0.0, 1.0, 2.0, 3.0, 4.0]
    rows, name = simulate(dose_times, PROLACTIN, max_days=12.0, sample_interval_days=1.0)
    print_table(rows, PROLACTIN[0], dose_times)

    # 场景 4：同一天吃 3 次（0, 0.25, 0.5）
    dose_times = [0.0, 0.25, 0.5]
    rows, name = simulate(dose_times, PROLACTIN, max_days=6.0, sample_interval_days=0.5)
    print_table(rows, PROLACTIN[0], dose_times)

    # 场景 5：Lucilactin 吃 2 次（0, 2）
    dose_times = [0.0, 2.0]
    rows, name = simulate(dose_times, LUCILACTIN, max_days=14.0, sample_interval_days=1.0)
    print_table(rows, LUCILACTIN[0], dose_times)

    print("\n说明：")
    print("  - 剩余有效天数(约) = L / D(L,E)，即按当前衰减率估算的「还能维持多少天」；L 降为 0 后泌乳结束。")
    print("  - 吸收生效 = 服药后 0.25 天 L 增加、耐受增加；之后每 200 tick 做一次 L 衰减与耐受自然恢复。")
    print("  - 采样 = 按间隔输出的 (L, 耐受, 剩余天数) 快照。")


if __name__ == "__main__":
    main()
