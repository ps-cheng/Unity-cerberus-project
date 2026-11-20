"""
暫用音效合成器：用程式產生 demo 需要的 12 個音效（44.1 kHz、16-bit、單聲道 wav）。
只用 Python 標準函式庫，不需要下載任何素材。

用法：python Tools/Audio/synth_sfx.py [輸出資料夾]
預設輸出到 Assets/Audio/SFX。每次產生的結果相同（固定亂數種子）。
"""
import math
import os
import random
import struct
import sys
import wave

RATE = 44100
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Audio", "SFX")


def n(seconds):
    return int(seconds * RATE)


def noise(rng, count):
    return [rng.uniform(-1.0, 1.0) for _ in range(count)]


def lowpass(samples, cutoff):
    """一階低通。cutoff 可以是固定值或依樣本位置變化的函式（Hz）。"""
    out, y = [], 0.0
    for i, x in enumerate(samples):
        c = cutoff(i) if callable(cutoff) else cutoff
        a = 1.0 - math.exp(-2.0 * math.pi * c / RATE)
        y += a * (x - y)
        out.append(y)
    return out


def highpass(samples, cutoff):
    low = lowpass(samples, cutoff)
    return [x - l for x, l in zip(samples, low)]


def sweep(f0, f1, seconds, shape="sine", curve=1.0):
    """頻率從 f0 滑到 f1 的振盪器。curve > 1 = 前段變化慢。"""
    count, phase, out = n(seconds), 0.0, []
    for i in range(count):
        t = (i / count) ** curve
        f = f0 + (f1 - f0) * t
        phase += 2.0 * math.pi * f / RATE
        if shape == "sine":
            out.append(math.sin(phase))
        elif shape == "square":
            out.append(1.0 if math.sin(phase) >= 0 else -1.0)
        else:  # saw
            out.append(((phase / math.pi) % 2.0) - 1.0)
    return out


def env(count, attack, decay_power=1.0, hold=0.0):
    """attack 秒線性上升，之後以 (1-t)^decay_power 衰減。"""
    a, h, out = n(attack), n(hold), []
    for i in range(count):
        if i < a:
            out.append(i / max(a, 1))
        elif i < a + h:
            out.append(1.0)
        else:
            t = (i - a - h) / max(count - a - h, 1)
            out.append(max(0.0, 1.0 - t) ** decay_power)
    return out


def expdecay(count, rate):
    return [math.exp(-rate * i / RATE) for i in range(count)]


def mul(a, b):
    return [x * y for x, y in zip(a, b)]


def mix(*tracks):
    length = max(len(t) for t, _ in tracks)
    out = [0.0] * length
    for track, gain in tracks:
        for i, x in enumerate(track):
            out[i] += x * gain
    return out


def delay(samples, seconds):
    return [0.0] * n(seconds) + samples


def save(name, samples, peak=0.89):
    m = max(1e-9, max(abs(x) for x in samples))
    # 頭尾 3 ms 淡入淡出，避免爆音
    fade = n(0.003)
    data = bytearray()
    for i, x in enumerate(samples):
        g = min(1.0, i / fade, (len(samples) - 1 - i) / fade) if fade else 1.0
        v = int(max(-1.0, min(1.0, x / m * peak * g)) * 32767)
        data += struct.pack("<h", v)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(bytes(data))
    print(f"{name:14s} {len(samples) / RATE:.2f}s")


def main():
    os.makedirs(OUT, exist_ok=True)
    rng = random.Random(7)

    # 拐杖揮擊：濾波雜訊的「咻」，截止頻率先升後降
    c = n(0.24)
    whoosh = lowpass(noise(rng, c), lambda i: 400 + 3200 * math.sin(math.pi * i / c))
    save("sfx_cane_swing", mul(whoosh, env(c, 0.07, 2.0)))

    # 槍聲：尖銳的雜訊爆裂 + 低頻衝擊 + 短殘響
    c = n(0.35)
    crack = mul(highpass(noise(rng, c), 900), expdecay(c, 32))
    thump = mul(sweep(150, 45, 0.35), expdecay(c, 14))
    tail = mul(lowpass(noise(rng, c), 1200), expdecay(c, 9))
    save("sfx_gunshot", mix((crack, 1.0), (thump, 0.9), (tail, 0.35)))

    # 爪擊：兩道撕裂聲（高通雜訊 + 粗糙的振幅抖動），第二道稍晚
    def scratch(length, start_cut):
        cc = n(length)
        s = highpass(noise(rng, cc), start_cut)
        jag = [0.6 + 0.4 * math.sin(i / RATE * 2 * math.pi * 55) * (1 if rng.random() > 0.15 else 0.3) for i in range(cc)]
        return mul(mul(s, jag), env(cc, 0.012, 1.6))
    save("sfx_claw", mix((scratch(0.16, 1800), 1.0), (delay(scratch(0.2, 1300), 0.06), 0.9),
                         (mul(sweep(220, 90, 0.18), expdecay(n(0.18), 18)), 0.5)))

    # 擊中：短促的低頻撞擊加一點喀聲
    c = n(0.16)
    save("sfx_hit", mix((mul(sweep(200, 60, 0.16), expdecay(c, 22)), 1.0),
                        (mul(lowpass(noise(rng, n(0.04)), 3000), expdecay(n(0.04), 90)), 0.8)))

    # 敵人爆炸：低頻轟聲 + 低通雜訊 + 劈啪碎裂聲
    c = n(0.9)
    boom = mul(sweep(90, 28, 0.9, curve=0.6), expdecay(c, 5))
    rumble = mul(lowpass(noise(rng, c), lambda i: 2500 * math.exp(-4 * i / c) + 200), expdecay(c, 4.5))
    crackle = [0.0] * c
    for _ in range(40):
        p = rng.randrange(0, int(c * 0.6))
        for k in range(n(0.004)):
            if p + k < c:
                crackle[p + k] += rng.uniform(-1, 1) * math.exp(-k / 40)
    save("sfx_enemy_explode", mix((boom, 1.0), (rumble, 0.9), (crackle, 0.45)))

    # 切頭：上升的音調加顫音 + 一陣煙霧般的氣聲 + 高頻閃爍
    c = n(0.42)
    rise = [math.sin(2 * math.pi * (300 * t / RATE + 450 * (t / RATE) ** 2 / 0.42) + 0.4 * math.sin(2 * math.pi * 18 * t / RATE)) for t in range(c)]
    puff = lowpass(noise(rng, c), lambda i: 600 + 2400 * (i / c))
    shimmer = mul(sweep(2400, 3600, 0.42), [0.5 + 0.5 * math.sin(2 * math.pi * 30 * i / RATE) for i in range(c)])
    save("sfx_head_switch", mul(mix((rise, 0.7), (puff, 0.6), (shimmer, 0.18)), env(c, 0.05, 1.4)))

    # 雷射蓄力：越來越高、越來越快的嗡嗡聲（約 0.9 秒）
    c = n(0.9)
    charge = sweep(180, 1100, 0.9, shape="saw", curve=1.6)
    trem = [0.6 + 0.4 * math.sin(2 * math.pi * (6 + 30 * (i / c)) * i / RATE) for i in range(c)]
    charge = lowpass(mul(charge, trem), 2500)
    save("sfx_laser_charge", mul(charge, [min(1.0, 0.15 + 0.85 * (i / c) ** 1.5) for i in range(c)]))

    # 雷射發射：往下掉的方波 + 嘶聲
    c = n(0.45)
    zap = lowpass(sweep(1100, 260, 0.45, shape="square", curve=0.5), 4000)
    hiss = mul(highpass(noise(rng, c), 2500), expdecay(c, 7))
    save("sfx_laser_fire", mul(mix((zap, 0.8), (hiss, 0.4)), env(c, 0.005, 1.3)))

    # 格擋成功：金屬撞擊（不成倍數的幾個泛音，各自衰減）
    c = n(0.6)
    partials = [(860, 1.0, 7), (1370, 0.7, 9), (2230, 0.5, 12), (3180, 0.35, 15), (4410, 0.2, 20)]
    clang = mix(*[(mul(sweep(f, f * 0.995, 0.6), expdecay(c, d)), g) for f, g, d in partials])
    click = mul(highpass(noise(rng, n(0.02)), 3000), expdecay(n(0.02), 200))
    save("sfx_parry", mix((clang, 1.0), (click, 0.6)))

    # 受傷：低沉粗糙的悶擊
    c = n(0.24)
    grunt = lowpass(sweep(150, 80, 0.24, shape="square"), 900)
    save("sfx_hurt", mul(mix((grunt, 0.8), (lowpass(noise(rng, c), 1500), 0.5)), env(c, 0.004, 1.8)))

    # 拿到硬幣：兩個音（B5 → E6）的鈴聲，帶一點泛音
    def bell(f, length):
        cc = n(length)
        tone = mix((sweep(f, f, length), 1.0), (sweep(f * 2, f * 2, length), 0.35), (sweep(f * 3, f * 3, length), 0.12))
        return mul(tone, expdecay(cc, 5))
    save("sfx_coin", mix((bell(987.8, 0.25), 0.8), (delay(bell(1318.5, 0.75), 0.09), 1.0)))

    # 介面按鍵：短短的「嗒」
    c = n(0.06)
    save("sfx_ui_click", mix((mul(sweep(1900, 1500, 0.06), expdecay(c, 70)), 1.0),
                             (mul(highpass(noise(rng, c), 4000), expdecay(c, 160)), 0.3)))


if __name__ == "__main__":
    main()
