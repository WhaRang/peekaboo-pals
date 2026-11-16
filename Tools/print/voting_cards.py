import base64, random, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Assets" / "_Main"
OUT = Path(sys.argv[1])

def b64(p):
    return base64.b64encode(Path(p).read_bytes()).decode()

ROSTER = [
    ("pip", "Pip the Pumpkin", "common"), ("skelly", "Skelly Bones", "common"),
    ("batsy", "Batsy", "common"), ("sheety", "Sheety the Ghost", "common"),
    ("wanda", "Wanda the Witch", "rare"), ("mumbles", "Mumbles the Mummy", "rare"),
    ("howler", "Howler the Werewolf", "rare"), ("fangsworth", "Count Fangsworth", "epic"),
    ("gourdking", "The Great Gourd King", "epic"), ("jenny", "Jenny XJ-Boo", "legendary"),
]
TIERS = {  # frame, glow, label (from the game's rarity tiers)
    "common": ("#ff8a1f", "#ffb347", "Common"),
    "rare": ("#9b5cff", "#c9a2ff", "Rare"),
    "epic": ("#3ee86a", "#b6ff3d", "Epic"),
    "legendary": ("#8fe9ff", "#7df9ff", "Legendary"),
}

img = {cid: b64(ROOT / f"Art/Characters/{cid}.png") for cid, _, _ in ROSTER}
moon = b64(ROOT / "Art/UI/moon.png")
hills = b64(ROOT / "Art/UI/hills.png")
title_font = b64(ROOT / "Fonts/LuckiestGuy-Regular.ttf")
body_font = b64(ROOT / "Fonts/Fredoka-SemiBold.ttf")
logo = b64(ROOT / "Art/UI/logo.png")
trophy = b64(ROOT / "Art/UI/icon_trophy.png")

def stars(seed):
    rng = random.Random(seed)
    out = []
    for _ in range(26):
        x, y, r = rng.uniform(0, 100), rng.uniform(0, 60), rng.uniform(0.25, 0.7)
        out.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{r:.2f}" fill="#fff5e0" opacity="{rng.uniform(0.35, 0.9):.2f}"/>')
    for _ in range(3):
        x, y, s = rng.uniform(5, 60), rng.uniform(4, 40), rng.uniform(1.4, 2.4)
        out.append(f'<path transform="translate({x:.1f} {y:.1f}) scale({s:.2f})" d="M0 -1 Q0.15 -0.15 1 0 Q0.15 0.15 0 1 Q-0.15 0.15 -1 0 Q-0.15 -0.15 0 -1Z" fill="#fff1a8"/>')
    return f'<svg class="stars" viewBox="0 0 100 60" preserveAspectRatio="xMidYMid slice">{"".join(out)}</svg>'

def glow(uid, color, opacity):
    return (f'<svg class="glow" viewBox="0 0 100 100" preserveAspectRatio="none"><defs><radialGradient id="g{uid}">'
            f'<stop offset="0" stop-color="{color}" stop-opacity="{opacity}"/><stop offset="1" stop-color="{color}" stop-opacity="0"/>'
            f'</radialGradient></defs><circle cx="50" cy="50" r="50" fill="url(#g{uid})"/></svg>')

def title(uid, text, width, height, size, stroke, stops):
    grad = "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in stops)
    return (f'<svg class="title-svg" width="{width}mm" height="{height}mm" viewBox="0 0 {width * 10} {height * 10}">'
            f'<defs><linearGradient id="t{uid}" x1="0" y1="0" x2="0" y2="1">{grad}</linearGradient></defs>'
            f'<text x="{width * 5}" y="{height * 10 - stroke}" text-anchor="middle" font-family="Luckiest Guy" font-size="{size * 10}" '
            f'fill="url(#t{uid})" stroke="#2a1240" stroke-width="{stroke * 2}" stroke-linejoin="round" paint-order="stroke">{text}</text></svg>')

VOTE_STOPS = [(0, "#ffe36b"), (0.7, "#ff9a1f"), (1, "#ff7a12")]
COSTUME_STOPS = [(0, "#d9ffb0"), (0.6, "#9cf060"), (1, "#6fd63a")]

cards = []
for i in range(30):
    cid, name, tier = ROSTER[i % len(ROSTER)]
    frame, glow_color, label = TIERS[tier]
    shimmer = '<div class="holo"></div>' if tier == "legendary" else ""
    cards.append(f'''
<div class="card {tier}" style="--frame:{frame}">
  <div class="scene">
    {stars(i * 7 + 3)}
    {shimmer}
    <img class="moon" src="data:image/png;base64,{moon}">
    <img class="hills" src="data:image/png;base64,{hills}">
    <div class="halo">{glow(f"h{i}", glow_color, 0.55)}</div>
    <img class="mascot" src="data:image/png;base64,{img[cid]}">
    <div class="title">{title(f"v{i}", "VOTE!", 50, 19, 17, 13, VOTE_STOPS)}</div>
    <div class="sub">for the <b>Best Costume</b></div>
    <div class="who"><span class="rarity">&#9733; {label}</span>{name}</div>
    <div class="num">#{i + 1:02d}</div>
  </div>
  <div class="panel">
    <div class="label">My vote goes to&hellip;</div>
    <div class="line"></div>
    <div class="line"></div>
  </div>
</div>''')

pages = []
for p in range(0, 30, 6):
    pages.append('<section class="page">' + "".join(cards[p:p + 6]) + "</section>")

SPARK = '<svg viewBox="-1 -1 2 2"><path d="M0 -1 Q0.15 -0.15 1 0 Q0.15 0.15 0 1 Q-0.15 0.15 -1 0 Q-0.15 -0.15 0 -1Z" fill="#fff1a8"/></svg>'
def back(b):
    return f'''
<div class="card back">
  <div class="back-inner">
    {stars(999)}
    <div class="stripes"></div>
    <div class="ring"></div>
    <i class="spark tl">{SPARK}</i><i class="spark tr">{SPARK}</i><i class="spark bl">{SPARK}</i><i class="spark br">{SPARK}</i>
    <div class="logo-wrap"><div class="logo-glow">{glow(f"l{b}", "#c9a2ff", 0.5)}</div><img class="logo" src="data:image/png;base64,{logo}"></div>
    <div class="medal-glow">{glow(f"m{b}", "#ffd23f", 0.6)}</div><div class="medal"><img src="data:image/png;base64,{trophy}"></div>
    <div class="headline">{title(f"c{b}", "Best Costume", 76, 12, 8.6, 9, COSTUME_STOPS)}</div>
    <div class="contest">&#9733; Contest &#9733;</div>
  </div>
</div>'''
# One sheet of backs: print it on the reverse of every card sheet. The grid is symmetric, so it lines up either way.
pages.append('<section class="page">' + "".join(back(b) for b in range(6)) + "</section>")

html = f'''<!doctype html><html><head><meta charset="utf-8"><title>Spooky Collector Voting Cards</title>
<style>
@font-face {{ font-family: "Luckiest Guy"; src: url(data:font/ttf;base64,{title_font}); }}
@font-face {{ font-family: "Fredoka"; src: url(data:font/ttf;base64,{body_font}); }}
@page {{ size: A4; margin: 0; }}
* {{ box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }}
html, body {{ margin: 0; background: #fff; }}
.page {{ width: 210mm; height: 297mm; padding: 11mm 10mm; display: grid;
  grid-template-columns: 92mm 92mm; grid-template-rows: repeat(3, 88mm); gap: 5.5mm 6mm;
  page-break-after: always; break-after: page; overflow: hidden; }}
.card {{ position: relative; border-radius: 5mm; background: var(--frame); padding: 1.6mm;
  box-shadow: inset 0 0 0 0.5mm #2a1240; display: flex; flex-direction: column; overflow: hidden;
  font-family: "Fredoka", sans-serif; color: #2a1240; }}
.legendary {{ background: linear-gradient(135deg, #8fe9ff, #ff9df0 35%, #fff3a6 55%, #7df9ff 80%, #b18cff); }}
.scene {{ position: relative; flex: 0 0 52mm; border-radius: 3.6mm 3.6mm 1.2mm 1.2mm; overflow: hidden;
  background: radial-gradient(120% 90% at 80% 20%, #3b1d78 0%, #26124d 45%, #0f0824 100%);
  box-shadow: inset 0 0 0 0.45mm #2a1240; }}
.legendary .scene {{ background: radial-gradient(120% 90% at 80% 20%, #1d3a7a 0%, #141f4d 45%, #0a0f26 100%); }}
.stars {{ position: absolute; inset: 0; width: 100%; height: 100%; }}
.holo {{ position: absolute; inset: 0; opacity: 0.22;
  background: repeating-linear-gradient(115deg, transparent 0 6mm, #7df9ff 6mm 7mm, transparent 7mm 13mm, #ff9df0 13mm 14mm); }}
.moon {{ position: absolute; width: 9mm; left: 3.5mm; top: 1.8mm; opacity: 0.95; }}
.hills {{ position: absolute; left: -4mm; right: -4mm; bottom: -2mm; width: calc(100% + 8mm); height: 20mm; object-fit: cover; object-position: top; }}
.halo {{ position: absolute; right: 1mm; top: 4mm; width: 44mm; height: 44mm; border-radius: 50%;
  }}
.mascot {{ position: absolute; right: 0mm; top: 1mm; width: 47mm; height: 47mm; object-fit: contain; }}
.title {{ position: absolute; left: 1mm; top: 9.5mm; }}
.title-svg {{ display: block; overflow: visible; }}
.glow {{ display: block; width: 100%; height: 100%; }}
.sub {{ position: absolute; left: 4.5mm; top: 31mm; font-size: 3.6mm; color: #fff5e0; letter-spacing: 0.1mm;
  background: #2a1240; padding: 0.8mm 2.6mm 1mm; border-radius: 3mm; box-shadow: 0 0 0 0.5mm var(--frame); }}
.sub b {{ color: #b6ff3d; font-weight: 600; }}
.who {{ position: absolute; left: 3mm; bottom: 2.4mm; font-size: 2.7mm; color: #fff5e0; display: flex; align-items: center; gap: 1.4mm; }}
.rarity {{ background: var(--frame); color: #2a1240; border-radius: 2mm; padding: 0.3mm 1.6mm 0.4mm; font-size: 2.3mm; text-transform: uppercase; letter-spacing: 0.15mm; }}
.legendary .rarity {{ background: linear-gradient(90deg, #8fe9ff, #ff9df0, #fff3a6); }}
.num {{ position: absolute; right: 2.6mm; bottom: 2.2mm; font-size: 2.5mm; color: #c9b8ee; opacity: 0.85; }}
.panel {{ flex: 1; margin-top: 1.3mm; border-radius: 1.2mm 1.2mm 3.6mm 3.6mm; background: #fff5e0;
  box-shadow: inset 0 0 0 0.45mm #2a1240; padding: 3.2mm 5mm 0; position: relative; }}
.label {{ font-family: "Luckiest Guy"; font-size: 5.2mm; color: #7a49b8; letter-spacing: 0.2mm; }}
.line {{ height: 9.5mm; border-bottom: 0.5mm dashed #b4a8cc; }}
.back {{ background: linear-gradient(135deg, #ffe36b, #ff9a1f 50%, #ffd23f); }}
.back-inner {{ position: relative; flex: 1; border-radius: 3.6mm; overflow: hidden;
  background: radial-gradient(110% 80% at 50% 45%, #3b1d78 0%, #26124d 50%, #0f0824 100%);
  box-shadow: inset 0 0 0 0.45mm #2a1240; display: flex; flex-direction: column; align-items: center; }}
.stripes {{ position: absolute; inset: 0; opacity: 0.08;
  background: repeating-linear-gradient(120deg, #fff5e0 0 7mm, transparent 7mm 14mm); }}
.ring {{ position: absolute; inset: 2.6mm; border: 0.6mm solid rgba(255, 210, 63, 0.55); border-radius: 2.6mm; }}
.spark {{ position: absolute; width: 3.6mm; height: 3.6mm; }}
.spark svg {{ width: 100%; height: 100%; display: block; }}
.tl {{ left: 4.6mm; top: 4.6mm; }} .tr {{ right: 4.6mm; top: 4.6mm; }}
.bl {{ left: 4.6mm; bottom: 4.6mm; }} .br {{ right: 4.6mm; bottom: 4.6mm; }}
.logo-wrap {{ position: relative; margin-top: 5mm; }}
.logo {{ position: relative; display: block; width: 52mm; }}
.logo-glow {{ position: absolute; left: -6mm; right: -6mm; top: -5mm; bottom: -5mm; }}
.logo-glow .glow {{ width: 100%; height: 100%; }}
.medal-glow {{ position: absolute; left: 50%; top: 15mm; width: 50mm; height: 50mm; margin-left: -25mm; }}
.medal {{ position: relative; width: 31mm; height: 31mm; margin-top: 0.5mm; border-radius: 50%;
  background: radial-gradient(circle closest-side, #6a3fc0 0%, #3b1d78 70%, #2a1240 100%);
  box-shadow: 0 0 0 1mm #ffd23f, 0 0 0 1.5mm #2a1240;
  display: flex; align-items: center; justify-content: center; }}
.medal img {{ width: 22mm; }}
.headline {{ position: relative; margin-top: 2.6mm; }}
.contest {{ position: relative; margin-top: 1.4mm; font-family: "Luckiest Guy"; font-size: 4.4mm; letter-spacing: 0.6mm;
  color: #2a1240; background: linear-gradient(90deg, #ffe36b, #ff9a1f); padding: 1.1mm 4mm 0.5mm; border-radius: 3mm;
  box-shadow: 0 0 0 0.5mm #2a1240; }}
</style></head><body>{"".join(pages)}</body></html>'''
OUT.write_text(html)
print(OUT, len(html) // 1024, "KB")
