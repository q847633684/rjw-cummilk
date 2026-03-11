# -*- coding: utf-8 -*-
"""每天吃一个药，30 天模拟：时间与进水量。
公式依据：记忆库/docs/泌乳系统逻辑图、耐受水池模拟结果、吃药泌乳模拟。
"""
# 常数
BASELINE_DAYS = 5.0
K = 0.01  # NegativeFeedbackK
TOL_PER_DOSE = 0.044   # Prolactin 每次耐受
TOL_DECAY_PER_DAY = 0.015  # 耐受日衰减（severityPerDay，取绝对值）
RAW_SEVERITY = 0.5     # Prolactin rawSeverity
C_DOSE = 1.0

def E_tol(t):
    return max(1.0 - t, 0.05)

def B_T(baseline):
    denom = 0.5 / baseline - K * 0.5
    if denom <= 0.01:
        return 100.0
    return 1.0 / denom

def D(L, E, bT):
    return 1.0 / (bT * E) + K * L

def main():
    bT = B_T(BASELINE_DAYS)
    t = 0.0
    L = 0.0
    total_inflow = 0.0   # 累计进水量（加入 L 的总量）

    for day in range(1, 31):
        # 1. 日衰减（耐受、L）
        t = max(0.0, t - TOL_DECAY_PER_DAY)
        E = E_tol(t)
        decay = D(L, E, bT)
        L = max(0.0, L - decay)

        # 2. 吃一粒药：t_before 用于 E_tol，然后 t += 0.044，L += Δs × C_dose
        t_before = t
        delta_s = RAW_SEVERITY * E_tol(t_before) * 1.0  # 种族倍率 1
        delta_L = delta_s * C_DOSE
        t += TOL_PER_DOSE
        L += delta_L
        total_inflow += delta_L

    # 30 天末状态（与游戏 RemainingDays 一致：解析解 (1/k)*ln(1 + k*L/a)，a=1/(B_T*eff)）
    E = E_tol(t)
    bT_eff = bT  # 仅药物分量，L_birth=0
    a = 1.0 / (bT_eff * E)
    import math
    arg = 1.0 + K * L / a
    remaining_days = (1.0 / K) * math.log(arg) if arg > 1.0 else 0.0
    daily_decay = D(L, E, bT)
    linear_approx = L / daily_decay if daily_decay > 0 else 0  # 线性近似 L/D，仅对比用

    print("=== 每天 1 粒 Prolactin，连续 30 天（默认设置 baseline=5）===")
    print(f"游戏内经过时间：30 天")
    print(f"30 天末 耐受 t：{t:.4f}")
    print(f"30 天末 水池 L：{L:.4f}")
    print(f"累计进水量（加入 L 的总量）：{total_inflow:.4f}")
    print(f"当日衰减 D：{daily_decay:.4f} / 天")
    print(f"剩余泌乳（游戏公式 (1/k)*ln(1+k*L/a)）：{remaining_days:.2f} 天")
    print(f"  （线性近似 L/D = {linear_approx:.2f} 天，仅参考）")
    print()
    print("说明：进水量 = 30 次吃药对 L 的累计增加量（ΔL = Δs×C_dose，Δs=0.5×E_tol）。")
    print("      时间 = 30 游戏日；30 天后 L 仍>0 时可继续泌乳约 remaining_days 天。")

if __name__ == "__main__":
    main()
