"""Procedurally synthesized music + SFX for Spooky Collector (CC0, made for this project).
Run:  python3 Tools/audio/synth.py   (needs numpy). Writes 16-bit mono WAVs to Assets/_Main/Audio/Generated."""
import os
import wave

import numpy as np

SR = 44100
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Main", "Audio", "Generated")
rng = np.random.default_rng(1031)


# ------------------------------------------------------------------ primitives

def t_of(seconds):
    return np.arange(int(seconds * SR)) / SR


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def env_adsr(n, a=0.005, d=0.1, s=0.6, r=0.1, hold=None):
    total = n / SR
    hold = total - a - d - r if hold is None else hold
    hold = max(0.0, hold)
    parts = [np.linspace(0, 1, max(1, int(a * SR))), np.linspace(1, s, max(1, int(d * SR))),
             np.full(max(0, int(hold * SR)), s), np.linspace(s, 0, max(1, int(r * SR)))]
    e = np.concatenate(parts)
    return np.pad(e, (0, max(0, n - len(e))))[:n]


def env_exp(n, decay):
    return np.exp(-np.arange(n) / SR / decay)


def osc(freq, seconds, kind="sine", phase_mod=None):
    t = t_of(seconds)
    f = np.broadcast_to(np.asarray(freq, dtype=float), t.shape)
    ph = 2 * np.pi * np.cumsum(f) / SR
    if phase_mod is not None:
        ph = ph + phase_mod
    if kind == "sine":
        return np.sin(ph)
    if kind == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    if kind == "square":
        return np.tanh(np.sin(ph) * 4)
    if kind == "saw":
        return 2 * ((ph / (2 * np.pi)) % 1) - 1
    raise ValueError(kind)


def lowpass(x, cutoff):
    # One-pole low-pass, cutoff in Hz (scalar).
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def noise(seconds):
    return rng.uniform(-1, 1, int(seconds * SR))


def place(track, clip, at, gain=1.0):
    start = int(at * SR)
    end = min(len(track), start + len(clip))
    if end > start:
        track[start:end] += clip[:end - start] * gain
    return track


def normalize(x, peak=0.89):
    m = np.max(np.abs(x)) or 1.0
    return x / m * peak


def save(name, x, peak=0.89):
    os.makedirs(OUT, exist_ok=True)
    x = normalize(x, peak)
    fade = min(len(x), int(0.004 * SR))
    x[-fade:] *= np.linspace(1, 0, fade)
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


# ------------------------------------------------------------------ instruments

def bell(freq, seconds=1.2, bright=1.0):
    t = t_of(seconds)
    partials = [(1, 1.0, 1.0), (2.76, 0.45 * bright, 0.5), (5.4, 0.25 * bright, 0.3), (8.93, 0.12 * bright, 0.2)]
    x = sum(a * np.sin(2 * np.pi * freq * m * t) * np.exp(-t / (seconds * d)) for m, a, d in partials)
    return x * env_adsr(len(t), 0.002, 0.05, 1, 0.05)


def pluck(freq, seconds=0.5, bright=0.6):
    t = t_of(seconds)
    x = osc(freq, seconds, "tri") * 0.7 + osc(freq * 2, seconds, "sine") * 0.3 * bright
    return x * env_exp(len(t), seconds * 0.25)


def celesta(freq, seconds=0.9):
    t = t_of(seconds)
    x = np.sin(2 * np.pi * freq * t) + 0.35 * np.sin(2 * np.pi * freq * 4 * t) * np.exp(-t / 0.08)
    return x * env_exp(len(t), 0.32) * env_adsr(len(t), 0.003, 0.02, 1, 0.05)


def bass(freq, seconds=0.45):
    t = t_of(seconds)
    x = osc(freq, seconds, "tri") + 0.4 * osc(freq * 2, seconds, "sine") * np.exp(-t / 0.05)
    return lowpass(x, 900) * env_exp(len(t), 0.18) * env_adsr(len(t), 0.004, 0.02, 1, 0.04)


def brass(freq, seconds=0.5, vibrato=True):
    t = t_of(seconds)
    vib = 1 + (0.006 * np.sin(2 * np.pi * 5.5 * t) * np.clip(t / 0.25, 0, 1) if vibrato else 0)
    x = osc(freq * vib, seconds, "saw") * 0.6 + osc(freq * vib, seconds, "square") * 0.25
    x = lowpass(x, 2400)
    return x * env_adsr(len(t), 0.03, 0.08, 0.8, 0.12)


def organ(freq, seconds=0.6):
    t = t_of(seconds)
    x = sum(a * np.sin(2 * np.pi * freq * m * t) for m, a in [(0.5, 0.5), (1, 1), (2, 0.6), (3, 0.3), (4, 0.25)])
    return x * env_adsr(len(t), 0.01, 0.05, 0.85, 0.12)


def kick(seconds=0.35):
    t = t_of(seconds)
    f = 50 + 110 * np.exp(-t / 0.04)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * env_exp(len(t), 0.12)


def hat(seconds=0.05, gain=1.0):
    return highpass(noise(seconds), 6000) * env_exp(int(seconds * SR), 0.015) * gain


def snap(seconds=0.12):
    x = highpass(noise(seconds), 1500) * env_exp(int(seconds * SR), 0.025)
    return x + 0.4 * np.sin(2 * np.pi * 1800 * t_of(seconds)) * env_exp(int(seconds * SR), 0.01)


def timpani(freq, seconds=1.2):
    t = t_of(seconds)
    f = freq * (1 + 0.15 * np.exp(-t / 0.03))
    return (np.sin(2 * np.pi * np.cumsum(f) / SR) * env_exp(len(t), 0.5) + 0.3 * lowpass(noise(seconds), 400) * env_exp(len(t), 0.08))


def whoosh(seconds=0.5, up=True):
    n = int(seconds * SR)
    x = noise(seconds)
    cut = np.linspace(400, 5000, n) if up else np.linspace(5000, 400, n)
    y = np.empty(n)
    acc = 0.0
    for i in range(n):
        a = np.exp(-2 * np.pi * cut[i] / SR)
        acc = (1 - a) * x[i] + a * acc
        y[i] = acc
    return y * np.sin(np.linspace(0, np.pi, n)) ** 1.5


# ------------------------------------------------------------------ music: 16-bar spooky-bouncy loop

def music_loop():
    bpm = 118
    beat = 60 / bpm
    bars = 16
    length = bars * 4 * beat
    track = np.zeros(int(length * SR) + SR * 3)

    # i - VI - iv - V in A minor, twice with a turnaround.
    chords = [("A", [57, 60, 64]), ("F", [53, 57, 60]), ("D", [50, 53, 57]), ("E", [52, 56, 59])] * 4
    roots = {"A": 45, "F": 41, "D": 38, "E": 40}

    melody_a = [(69, 0, 1), (72, 1, 0.5), (76, 1.5, 0.5), (75, 2, 0.5), (76, 2.5, 1), (72, 3.5, 0.5),
                (65, 4, 1), (69, 5, 0.5), (72, 5.5, 0.5), (71, 6, 0.5), (72, 6.5, 1), (69, 7.5, 0.5),
                (74, 8, 1), (77, 9, 0.5), (81, 9.5, 0.5), (80, 10, 0.5), (81, 10.5, 1), (77, 11.5, 0.5),
                (68, 12, 0.5), (71, 12.5, 0.5), (76, 13, 0.5), (74, 13.5, 0.5), (71, 14, 0.5), (68, 14.5, 0.5), (64, 15, 1)]
    melody_b = [(76, 0, 0.5), (74, 0.5, 0.5), (72, 1, 0.5), (71, 1.5, 0.5), (72, 2, 1.5), (69, 3.5, 0.5),
                (77, 4, 0.5), (76, 4.5, 0.5), (74, 5, 0.5), (72, 5.5, 0.5), (74, 6, 1.5), (72, 7.5, 0.5),
                (81, 8, 0.75), (80, 8.75, 0.25), (81, 9, 0.5), (77, 9.5, 0.5), (74, 10, 1), (77, 11, 1),
                (76, 12, 0.5), (80, 12.5, 0.5), (83, 13, 0.5), (80, 13.5, 0.5), (76, 14, 1), (71, 15, 0.5), (68, 15.5, 0.5)]

    for bar, (name, triad) in enumerate(chords):
        t0 = bar * 4 * beat
        r = roots[name]
        for b, note in [(0, r), (1, r + 12), (2, r + 7), (3, r + 12)]:
            place(track, bass(midi(note), beat * 0.9), t0 + b * beat, 0.55)
        for b in (1, 3):
            for n in triad:
                place(track, pluck(midi(n), beat * 0.8, 0.5), t0 + b * beat + 0.01, 0.16)
        place(track, kick(), t0, 0.5)
        place(track, kick(), t0 + 2 * beat, 0.35)
        for e in range(8):
            place(track, hat(0.05, 0.5 if e % 2 else 0.25), t0 + e * beat / 2, 0.22)
        place(track, snap(), t0 + 3 * beat, 0.25)

    for section, mel in enumerate([melody_a, melody_b, melody_a, melody_b]):
        base = section * 16 * beat
        for note, start, dur in mel:
            place(track, celesta(midi(note), dur * beat + 0.5), base + start * beat, 0.42)
            if section >= 2:
                place(track, bell(midi(note + 12), 0.6, 0.6), base + start * beat, 0.07)

    # Seamless loop: fold the tail that rings past the end back onto the start.
    n = int(length * SR)
    tail = track[n:]
    loop = track[:n].copy()
    loop[:len(tail)] += tail
    return lowpass(loop, 9000)


# ------------------------------------------------------------------ stingers & fanfares

def arpeggio(notes, step, inst=celesta, gain=0.5, total=None, **kw):
    total = total or step * len(notes) + 1.2
    x = np.zeros(int(total * SR))
    for i, n in enumerate(notes):
        place(x, inst(midi(n), **kw) if kw else inst(midi(n)), i * step, gain)
    return x


def stinger_common():
    x = arpeggio([72, 76, 79, 84], 0.07, pluck, 0.6, 0.9)
    return x + arpeggio([84, 88, 91], 0.06, bell, 0.25, 0.9)


def stinger_rare():
    x = arpeggio([67, 71, 74, 79, 83, 86], 0.06, celesta, 0.5, 1.4)
    shimmer = arpeggio([91, 95, 98, 103, 98, 103], 0.05, bell, 0.18, 1.4)
    return x + shimmer + np.pad(whoosh(0.4), (0, int(1.0 * SR)))[:len(x)] * 0.15


def stinger_epic():
    total = 2.0
    x = np.zeros(int(total * SR))
    for n in [50, 57, 62, 66, 69]:
        place(x, organ(midi(n), 1.6), 0.0, 0.18)
    place(x, timpani(midi(38), 1.6), 0.0, 0.6)
    place(x, arpeggio([74, 78, 81, 86, 90, 93, 98], 0.05, bell, 0.25, 1.6), 0.15)
    place(x, whoosh(0.6, False), 0.0, 0.25)
    return x


def fanfare_legendary():
    beat = 0.16
    total = 3.6
    x = np.zeros(int(total * SR))
    # Timpani roll into the hit.
    for i in range(10):
        place(x, timpani(midi(45), 0.4), i * 0.035, 0.12 + i * 0.03)
    seq = [(60, 0, 1), (60, 1, 1), (60, 2, 1), (67, 3, 3), (65, 6, 1), (67, 7, 1), (72, 8, 6)]
    for note, start, dur in seq:
        for harm in (0, 4, 7):
            place(x, brass(midi(note + harm), dur * beat + 0.25), 0.4 + start * beat, 0.25 if harm == 0 else 0.12)
    place(x, timpani(midi(36), 1.5), 0.4 + 8 * beat, 0.7)
    place(x, arpeggio([84, 88, 91, 96, 100, 103, 108], 0.06, bell, 0.25, 1.8), 0.4 + 8 * beat)
    crash = highpass(noise(1.8), 3000) * env_exp(int(1.8 * SR), 0.6)
    place(x, crash, 0.4 + 8 * beat, 0.25)
    # Robot flavour: a power-up sweep into the first note and a square-wave arpeggio over the final chord.
    n = int(0.45 * SR)
    sweep = lowpass(osc(np.geomspace(110, 1800, n), 0.45, "saw"), 3200) * np.linspace(0, 1, n) ** 2
    place(x, sweep, 0.0, 0.16)
    for i, note in enumerate([72, 76, 79, 84, 88, 91, 96, 100]):
        m = int(0.07 * SR)
        place(x, osc(midi(note), 0.07, "square") * env_adsr(m, 0.002, 0.02, 0.5, 0.03), 0.4 + 8 * beat + 0.1 + i * 0.05, 0.08)
    return x


def fanfare_completion():
    beat = 0.15
    total = 4.6
    x = np.zeros(int(total * SR))
    melody = [(67, 0, 1), (72, 1, 1), (76, 2, 1), (79, 3, 2), (76, 5, 1), (79, 6, 4), (77, 10, 1), (76, 11, 1), (74, 12, 1), (84, 13, 8)]
    for note, start, dur in melody:
        place(x, brass(midi(note), dur * beat + 0.3), start * beat, 0.32)
        place(x, celesta(midi(note + 12), dur * beat + 0.6), start * beat, 0.16)
    for i, (n, at) in enumerate([(48, 0), (55, 3), (52, 6), (48, 13)]):
        place(x, timpani(midi(n), 1.2), at * beat, 0.6)
        for harm in (0, 4, 7, 12):
            place(x, organ(midi(n + 12 + harm), 0.7 if i < 3 else 1.6), at * beat, 0.07)
    place(x, arpeggio([84, 88, 91, 96, 100, 103, 108, 112], 0.055, bell, 0.22, 2.0), 13 * beat)
    crash = highpass(noise(2.4), 3000) * env_exp(int(2.4 * SR), 0.8)
    place(x, crash, 13 * beat, 0.25)
    return x


# ------------------------------------------------------------------ game-feel SFX

def scan_beep():
    x = np.zeros(int(0.32 * SR))
    place(x, osc(1568, 0.09, "square") * env_adsr(int(0.09 * SR), 0.002, 0.02, 0.7, 0.03), 0.0, 0.35)
    place(x, osc(2093, 0.14, "square") * env_adsr(int(0.14 * SR), 0.002, 0.02, 0.7, 0.08), 0.1, 0.35)
    return lowpass(x, 6000)


def points_tick():
    return osc(2400, 0.035, "sine") * env_exp(int(0.035 * SR), 0.01)


def new_badge():
    return bell(midi(88), 0.7) * 0.6 + bell(midi(95), 0.7) * 0.4


def slot_fill():
    x = np.zeros(int(0.8 * SR))
    pop = osc(np.linspace(300, 900, int(0.08 * SR)), 0.08, "sine") * env_exp(int(0.08 * SR), 0.03)
    place(x, pop, 0, 0.8)
    place(x, arpeggio([84, 91, 96], 0.05, bell, 0.3, 0.7), 0.04)
    return x


def sparkle_sfx():
    notes = [96, 100, 103, 108, 103, 108, 112]
    return arpeggio(notes, 0.04, bell, 0.25, 0.9, seconds=0.5, bright=1.2)


def rumble():
    seconds = 1.8
    n = int(seconds * SR)
    sub = osc(np.linspace(38, 52, n), seconds, "sine")
    grit = lowpass(noise(seconds), 180) * 3
    return (sub * 0.7 + grit * 0.6) * env_adsr(n, 0.3, 0.2, 0.9, 0.6)


def build_up():
    seconds = 2.2
    n = int(seconds * SR)
    rise = osc(np.geomspace(180, 1400, n), seconds, "saw")
    rise = lowpass(rise, 3000) * np.linspace(0.1, 1, n) ** 2
    return rise * 0.5 + whoosh(seconds, True) * 0.7


def firework():
    seconds = 1.0
    n = int(seconds * SR)
    boom = lowpass(noise(seconds), 600) * env_exp(n, 0.12) * 1.4
    crackle = np.zeros(n)
    for _ in range(40):
        at = rng.uniform(0.08, 0.8)
        place(crackle, highpass(noise(0.01), 3000) * env_exp(int(0.01 * SR), 0.002), at, rng.uniform(0.2, 0.6))
    return boom + crackle


def whoosh_sfx():
    return whoosh(0.45, True)


def card_appear():
    x = whoosh(0.3, True) * 0.6
    return x + np.pad(pluck(midi(79), 0.3), (0, len(x)))[:len(x)] * 0.4


def float_fire():
    seconds = 1.6
    n = int(seconds * SR)
    roar = lowpass(noise(seconds), 700) * env_adsr(n, 0.15, 0.2, 0.7, 0.8) * 2.2
    crackle = np.zeros(n)
    for _ in range(70):
        at = rng.uniform(0.05, 1.4)
        place(crackle, highpass(noise(0.006), 2500) * env_exp(int(0.006 * SR), 0.0015), at, rng.uniform(0.3, 1.0))
    return roar + crackle * 0.8 + whoosh(seconds, True) * 0.4


def float_jackpot():
    x = arpeggio([84, 88, 91, 96, 100, 103, 108, 112, 115], 0.045, bell, 0.3, 1.9, seconds=0.9, bright=1.3)
    x += np.pad(brass(midi(72), 0.8), (0, len(x)))[:len(x)] * 0.25
    return x


# ------------------------------------------------------------------ character voices (one per character)

def voice_pip():  # bouncy "boing-boing"
    x = np.zeros(int(0.8 * SR))
    for i, at in enumerate([0, 0.22]):
        n = int(0.3 * SR)
        f = 180 * (1 + 0.8 * np.sin(np.linspace(0, np.pi, n))) * (1.2 if i else 1)
        place(x, osc(f, 0.3, "tri") * env_exp(n, 0.12), at, 0.8)
    return x


def voice_skelly():  # xylophone bone rattle
    return arpeggio([79, 76, 79, 83, 79, 86], 0.06, pluck, 0.5, 0.8, seconds=0.25, bright=1.0)


def voice_batsy():  # three squeaks
    x = np.zeros(int(0.6 * SR))
    for i, at in enumerate([0, 0.12, 0.24]):
        n = int(0.09 * SR)
        f = np.linspace(2200, 3600, n) * (1 + 0.1 * i)
        place(x, osc(f, 0.09, "sine") * env_adsr(n, 0.005, 0.02, 0.6, 0.04), at, 0.6)
    return x


def voice_sheety():  # "wooOOoo"
    seconds = 1.1
    t = t_of(seconds)
    f = 420 + 160 * np.sin(np.linspace(0, np.pi, len(t))) + 12 * np.sin(2 * np.pi * 6 * t)
    return (osc(f, seconds, "sine") + 0.2 * osc(f * 2, seconds, "sine")) * env_adsr(len(t), 0.12, 0.1, 0.8, 0.35)


def voice_wanda():  # magic sparkle + cackle-ish hops
    x = arpeggio([84, 88, 91, 96, 100], 0.05, bell, 0.3, 1.0)
    for i in range(3):
        n = int(0.08 * SR)
        place(x, osc(np.linspace(900, 700, n), 0.08, "square") * env_adsr(n, 0.005, 0.02, 0.5, 0.03) * 0.3, 0.35 + i * 0.11)
    return lowpass(x, 7000)


def voice_mumbles():  # muffled "mm-hmm"
    x = np.zeros(int(0.8 * SR))
    for f0, at, d in [(150, 0, 0.28), (190, 0.34, 0.35)]:
        n = int(d * SR)
        place(x, lowpass(osc(np.linspace(f0, f0 * 1.15, n), d, "saw"), 500) * env_adsr(n, 0.03, 0.05, 0.8, 0.1), at, 1.0)
    return x


def voice_howler():  # "a-wooo!"
    seconds = 1.3
    t = t_of(seconds)
    f = 300 + 300 * np.clip(t / 0.35, 0, 1) - 120 * np.clip((t - 0.8) / 0.5, 0, 1) + 9 * np.sin(2 * np.pi * 5 * t)
    x = osc(f, seconds, "saw")
    return lowpass(x, 1600) * env_adsr(len(t), 0.06, 0.1, 0.8, 0.4)


def voice_fangsworth():  # grand organ "dun dun DUNNN", a timpani hit and a flutter of bats
    x = np.zeros(int(1.9 * SR))
    for n, at, d in [(62, 0, 0.18), (61, 0.22, 0.18), (50, 0.44, 1.2)]:
        for harm in (0, 3, 7, 12):
            place(x, organ(midi(n + harm), d), at, 0.22 if harm < 12 else 0.1)
    place(x, timpani(midi(38), 1.4), 0.44, 0.7)
    place(x, lowpass(noise(1.2), 260) * env_adsr(int(1.2 * SR), 0.05, 0.2, 0.6, 0.8), 0.44, 0.9)
    for i, at in enumerate([0.9, 1.0, 1.08]):
        n = int(0.07 * SR)
        place(x, osc(np.linspace(2600, 3800, n) * (1 + 0.08 * i), 0.07, "sine") * env_adsr(n, 0.004, 0.02, 0.6, 0.03), at, 0.22)
    return x


def voice_gourdking():  # royal mini-fanfare
    x = np.zeros(int(1.3 * SR))
    for note, at, d in [(67, 0, 0.14), (72, 0.16, 0.14), (76, 0.32, 0.14), (79, 0.48, 0.6)]:
        place(x, brass(midi(note), d + 0.15), at, 0.5)
        place(x, brass(midi(note - 12), d + 0.15), at, 0.2)
    return x


# ------------------------------------------------------------------ robot voice (no words: chirps and burbles)

def chirp(f_from, f_to, seconds, wobble=0.0, kind="sine"):
    """One robot blip: a quick pitch glide with an optional fast warble, soft attack and release."""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    f = np.geomspace(f_from, f_to, n) * (1 + wobble * np.sin(2 * np.pi * 38 * t))
    return osc(f, seconds, kind) * env_adsr(n, 0.004, 0.02, 0.75, min(0.03, seconds * 0.4))


def voice_jenny():  # servo whirr, a happy burble of robot chirps, a rising "bee-dee!" and a twinkle
    x = np.zeros(int(1.9 * SR))
    n = int(0.2 * SR)
    whirr = lowpass(osc(np.linspace(240, 720, n) * (1 + 0.03 * np.sin(np.linspace(0, 40, n))), 0.2, "saw"), 1800)
    place(x, whirr * env_adsr(n, 0.02, 0.05, 0.8, 0.06), 0.0, 0.22)
    place(x, highpass(noise(0.012), 3000) * env_exp(int(0.012 * SR), 0.003), 0.0, 0.3)
    # Data burble: a fixed (seeded) run of short glides, like a cheerful little droid talking.
    burble = [(1500, 2300, 0.06), (2300, 1700, 0.05), (1250, 1250, 0.04), (1900, 2900, 0.07),
              (2600, 1500, 0.06), (1700, 2100, 0.04), (2100, 1300, 0.08)]
    at = 0.2
    for i, (f_from, f_to, d) in enumerate(burble):
        place(x, chirp(f_from, f_to, d, wobble=0.04 if i % 3 == 1 else 0.0), at, 0.32)
        place(x, chirp(f_from / 2, f_to / 2, d, kind="tri"), at, 0.1)  # a softer octave below for body
        at += d + 0.018
    # The happy "bee-dee!" sign-off: two rising tones, the second warbling with joy.
    place(x, chirp(1320, 1480, 0.09), at + 0.05, 0.38)
    place(x, chirp(1760, 2640, 0.22, wobble=0.05), at + 0.16, 0.4)
    place(x, chirp(880, 1320, 0.22, kind="tri"), at + 0.16, 0.12)
    place(x, arpeggio([96, 100, 103, 108], 0.045, bell, 0.16, 0.7), at + 0.42)
    return x


def main():
    out = {
        "music_lobby_loop": (music_loop, 0.7),
        "stinger_common": (stinger_common, 0.85), "stinger_rare": (stinger_rare, 0.85),
        "stinger_epic": (stinger_epic, 0.89), "fanfare_legendary": (fanfare_legendary, 0.89),
        "fanfare_completion": (fanfare_completion, 0.89),
        "scan_beep": (scan_beep, 0.7), "points_tick": (points_tick, 0.5), "new_badge": (new_badge, 0.8),
        "slot_fill": (slot_fill, 0.8), "sparkle": (sparkle_sfx, 0.7), "rumble": (rumble, 0.85),
        "build_up": (build_up, 0.75), "firework": (firework, 0.8), "whoosh": (whoosh_sfx, 0.7),
        "card_appear": (card_appear, 0.75),
        "float_fire": (float_fire, 0.8), "float_jackpot": (float_jackpot, 0.85),
        "voice_pip": (voice_pip, 0.8), "voice_skelly": (voice_skelly, 0.8), "voice_batsy": (voice_batsy, 0.7),
        "voice_sheety": (voice_sheety, 0.75), "voice_wanda": (voice_wanda, 0.8), "voice_mumbles": (voice_mumbles, 0.85),
        "voice_howler": (voice_howler, 0.8), "voice_fangsworth": (voice_fangsworth, 0.85),
        "voice_gourdking": (voice_gourdking, 0.85), "voice_jenny": (voice_jenny, 0.85),
    }
    for name, (fn, peak) in out.items():
        save(name, fn(), peak)
        print("wrote", name)


if __name__ == "__main__":
    main()
