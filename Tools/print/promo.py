import base64, random, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2] / "Assets" / "_Main"
def b64(p): return "data:image/png;base64," + base64.b64encode((ROOT / p).read_bytes()).decode()
def font(p): return "data:font/ttf;base64," + base64.b64encode((ROOT / p).read_bytes()).decode()

bg, moon, hills, logo = b64("Art/UI/background.png"), b64("Art/UI/moon.png"), b64("Art/UI/hills.png"), b64("Art/UI/logo.png")
icons = {k: b64(f"Art/UI/icon_{k}.png") for k in ("scan", "collection", "trophy")}

rng = random.Random(31)
stars = "".join(f'<circle cx="{rng.uniform(0,1200):.0f}" cy="{rng.uniform(0,900):.0f}" r="{rng.uniform(1,2.6):.1f}" fill="#fff5e0" opacity="{rng.uniform(.3,.9):.2f}"/>' for _ in range(90))
SPARK = "M0 -1 Q0.15 -0.15 1 0 Q0.15 0.15 0 1 Q-0.15 0.15 -1 0 Q-0.15 -0.15 0 -1Z"
sparks = "".join(f'<path transform="translate({x} {y}) scale({s})" d="{SPARK}" fill="#fff1a8"/>' for x, y, s in
                 [(120, 300, 16), (1080, 360, 12), (90, 820, 10), (1110, 930, 18), (210, 1180, 9), (1000, 1210, 11), (600, 60, 8)])
BAT = "M0 0 C-8 -10 -22 -12 -34 -6 C-26 -4 -22 2 -22 8 C-16 4 -9 5 -6 10 C-4 5 -2 3 0 3 C2 3 4 5 6 10 C9 5 16 4 22 8 C22 2 26 -4 34 -6 C22 -12 8 -10 0 0Z"
bats = "".join(f'<g transform="translate({x} {y}) rotate({r}) scale({s})"><path d="{BAT}" fill="#1a0b33"/><circle cx="-3" cy="1" r="1.3" fill="#ffd23f"/><circle cx="3" cy="1" r="1.3" fill="#ffd23f"/></g>'
               for x, y, r, s in [(150, 190, -12, 1.9), (1040, 150, 10, 1.5), (960, 560, -6, 1.2), (250, 640, 14, 1.1)])

def card(kind, cid, name, tier, power, rot, dx, dy, scale=1.0, z=1):
    colors = {"common": ("#ff8a1f", "#ffb347", "#4a2a10"), "rare": ("#9b5cff", "#c9a2ff", "#2a1650"),
              "epic": ("#3ee86a", "#b6ff3d", "#123a24"), "legendary": ("#8fe9ff", "#7df9ff", "#0f1a3d")}
    frame, glow, deep = colors[tier]
    if kind == "open":
        art = f'<img class="art" src="{b64(f"Art/Characters/{cid}.png")}">'
        label = name
    else:
        sil = b64(f"Art/Characters/{cid}_silhouette.png")
        art = (f'<div class="sil" style="-webkit-mask-image:url({sil});mask-image:url({sil});background:{glow}"></div>'
               f'<div class="q" style="color:{glow}">?</div>')
        label = "???"
    return (f'<div class="card {tier} {kind}" style="--frame:{frame};--glow:{glow};--deep:{deep};z-index:{z};'
            f'transform:translate({dx}px,{dy}px) rotate({rot}deg) scale({scale})">'
            f'<div class="inner">{art}<div class="power">{power}</div><div class="name">{label}</div></div></div>')

cards = "".join([
    card("open", "pip", "Pip the Pumpkin", "common", 18, -15, -478, 60, 0.84, 1),
    card("open", "wanda", "Wanda the Witch", "rare", 52, -7, -262, 8, 0.93, 2),
    card("open", "fangsworth", "Count Fangsworth", "epic", 82, 7, 262, 8, 0.93, 2),
    card("lock", "gourdking", "", "epic", "?", 15, 478, 60, 0.84, 1),
    card("lock", "jenny", "", "legendary", "?", 0, 0, -20, 1.14, 3),
])

html = f'''<!doctype html><html><head><meta charset="utf-8"><style>
@font-face {{ font-family: Luckiest; src: url({font("Fonts/LuckiestGuy-Regular.ttf")}); }}
@font-face {{ font-family: Fredoka; src: url({font("Fonts/Fredoka-SemiBold.ttf")}); }}
* {{ box-sizing: border-box; }}
html, body {{ margin: 0; width: 1200px; height: 1500px; overflow: hidden; background: #0f0724; }}
.poster {{ position: relative; width: 1200px; height: 1500px; overflow: hidden; font-family: Fredoka; color: #fff5e0;
  background: url({bg}) center / cover; }}
.layer {{ position: absolute; inset: 0; }}
.moon {{ position: absolute; right: 70px; top: 70px; width: 230px; filter: drop-shadow(0 0 60px rgba(255, 240, 180, .45)); }}
.hills {{ position: absolute; left: 0; bottom: 0; width: 1200px; height: 520px; object-fit: cover; object-position: top; }}
.fog {{ position: absolute; left: 0; right: 0; bottom: 0; height: 520px; background: linear-gradient(transparent, rgba(15, 7, 36, .92) 70%); }}
.logo {{ position: absolute; left: 50%; top: 46px; width: 740px; transform: translateX(-50%) rotate(-2deg);
  filter: drop-shadow(0 14px 0 rgba(0,0,0,.25)) drop-shadow(0 0 40px rgba(155, 92, 255, .55)); }}
.ribbon {{ position: absolute; left: 50%; top: 336px; transform: translateX(-50%); white-space: nowrap;
  font-family: Luckiest; font-size: 44px; letter-spacing: 2px; color: #2a1240; padding: 16px 44px 10px;
  background: linear-gradient(#ffe36b, #ff9a1f); border-radius: 40px; box-shadow: 0 0 0 6px #2a1240, 0 10px 0 6px rgba(0,0,0,.3); }}
.tease {{ position: absolute; left: 0; right: 0; top: 428px; text-align: center; font-size: 33px; color: #e6dcff;
  text-shadow: 0 3px 0 #1a0b33; }}
.tease b {{ color: #b6ff3d; font-weight: 600; }}
.fan {{ position: absolute; left: 600px; top: 700px; width: 0; height: 0; }}
.rays {{ position: absolute; left: -430px; top: -300px; width: 860px; height: 860px; border-radius: 50%;
  background: repeating-conic-gradient(rgba(125, 249, 255, .26) 0 7deg, transparent 7deg 18deg);
  -webkit-mask-image: radial-gradient(circle, #000 20%, transparent 68%); mask-image: radial-gradient(circle, #000 20%, transparent 68%); }}
.card {{ position: absolute; left: -130px; top: -10px; width: 260px; height: 360px; border-radius: 26px; padding: 9px;
  background: var(--frame); box-shadow: 0 0 0 5px #2a1240, 0 24px 40px rgba(0,0,0,.45); transform-origin: 50% 100%; }}
.legendary {{ background: linear-gradient(135deg, #8fe9ff, #ff9df0 35%, #fff3a6 55%, #7df9ff 80%, #b18cff);
  box-shadow: 0 0 0 5px #2a1240, 0 0 70px 10px rgba(125, 249, 255, .55), 0 24px 40px rgba(0,0,0,.45); }}
.inner {{ position: relative; width: 100%; height: 100%; border-radius: 18px; overflow: hidden;
  background: radial-gradient(circle at 50% 38%, color-mix(in srgb, var(--glow) 45%, var(--deep)) 0%, var(--deep) 75%);
  box-shadow: inset 0 0 0 4px rgba(42, 18, 64, .8); }}
.lock .inner {{ background: radial-gradient(circle at 50% 38%, color-mix(in srgb, var(--glow) 16%, #120826) 0%, #0d0620 80%); }}
.legendary .inner::after {{ content: ""; position: absolute; inset: 0; opacity: .22;
  background: repeating-linear-gradient(115deg, transparent 0 22px, #7df9ff 22px 26px, transparent 26px 48px, #ff9df0 48px 52px); }}
.art {{ position: absolute; left: 50%; top: 22px; width: 250px; transform: translateX(-50%); }}
.sil {{ position: absolute; left: 50%; top: 26px; width: 236px; height: 236px; transform: translateX(-50%); opacity: .3;
  -webkit-mask-size: contain; mask-size: contain; -webkit-mask-repeat: no-repeat; mask-repeat: no-repeat; }}
.q {{ position: absolute; left: 0; right: 0; top: 54px; text-align: center; font-family: Luckiest; font-size: 150px; line-height: 1;
  -webkit-text-stroke: 7px #2a1240; paint-order: stroke fill; text-shadow: 0 0 30px var(--glow); }}
.power {{ position: absolute; right: 12px; top: 12px; width: 54px; height: 54px; border-radius: 50%; background: #2a1240;
  box-shadow: 0 0 0 4px var(--frame); font-family: Luckiest; font-size: 28px; line-height: 60px; text-align: center; color: var(--glow); z-index: 2; }}
.name {{ position: absolute; left: 14px; right: 14px; bottom: 14px; padding: 12px 6px 8px; border-radius: 16px; text-align: center;
  background: #2a1240; font-family: Luckiest; font-size: 22px; color: #fff5e0; white-space: nowrap; z-index: 2;
  box-shadow: 0 0 0 3px var(--frame); }}
.lock .name {{ font-size: 30px; letter-spacing: 4px; color: var(--glow); }}
.leg-tag {{ position: absolute; left: 50%; top: -158px; transform: translateX(-50%); white-space: nowrap; z-index: 5;
  font-family: Luckiest; font-size: 28px; letter-spacing: 3px; color: #0f1a3d; padding: 10px 26px 5px; border-radius: 26px;
  background: linear-gradient(90deg, #8fe9ff, #ff9df0, #fff3a6); box-shadow: 0 0 0 5px #2a1240, 0 0 30px rgba(125,249,255,.6); }}
.steps {{ position: absolute; left: 60px; right: 60px; top: 1150px; display: flex; gap: 26px; }}
.step {{ flex: 1; background: rgba(26, 11, 51, .82); border-radius: 30px; padding: 16px 16px 18px; text-align: center;
  box-shadow: 0 0 0 4px #2a1240, inset 0 0 0 3px rgba(201, 162, 255, .35); }}
.step img {{ width: 70px; height: 70px; }}
.step .t {{ font-family: Luckiest; font-size: 38px; color: #ffb52e; margin-top: 4px; letter-spacing: 1px; }}
.step .d {{ font-size: 23px; color: #e6dcff; margin-top: 2px; line-height: 1.25; }}
.n {{ display: inline-block; width: 40px; height: 40px; border-radius: 50%; background: #7ccf3a; color: #2a1240; font-family: Luckiest;
  font-size: 26px; line-height: 46px; margin-right: 8px; box-shadow: 0 0 0 3px #2a1240; vertical-align: 4px; }}
.cta {{ position: absolute; left: 50%; bottom: 40px; transform: translateX(-50%); white-space: nowrap; font-family: Luckiest;
  font-size: 46px; color: #2a1240; padding: 22px 56px 12px; border-radius: 50px; letter-spacing: 1px;
  background: linear-gradient(#c9ff8f, #7ccf3a); box-shadow: 0 0 0 7px #2a1240, 0 12px 0 7px rgba(0,0,0,.35), 0 0 60px rgba(182,255,61,.45); }}
</style></head><body><div class="poster">
<svg class="layer" viewBox="0 0 1200 1500">{stars}</svg>
<img class="moon" src="{moon}">
<img class="hills" src="{hills}"><div class="fog"></div>
<svg class="layer" viewBox="0 0 1200 1500">{bats}{sparks}</svg>
<img class="logo" src="{logo}">
<div class="ribbon">THE HALLOWEEN QR HUNT</div>
<div class="tease"><b>10 spooky friends</b> are hiding around us.<br>Can you catch them all?</div>
<div class="fan"><div class="rays"></div>{cards}<div class="leg-tag">&#9733; ONE IS LEGENDARY &#9733;</div></div>
<div class="steps">
  <div class="step"><img src="{icons["scan"]}"><div class="t"><span class="n">1</span>FIND</div><div class="d">a hidden QR tag</div></div>
  <div class="step"><img src="{icons["collection"]}"><div class="t"><span class="n">2</span>SCAN</div><div class="d">it with your phone<br>&amp; reveal who it is</div></div>
  <div class="step"><img src="{icons["trophy"]}"><div class="t"><span class="n">3</span>COLLECT</div><div class="d">them all. Rarer friends<br>score more points!</div></div>
</div>
<div class="cta">THE HUNT IS ON!</div>
</div></body></html>'''
Path(sys.argv[1]).write_text(html)
