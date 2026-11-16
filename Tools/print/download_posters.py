# Two A4 posters with download QR codes (Android APK and the iOS web build). Needs segno.
#   python download_posters.py <out_dir>   -> poster_android.html, poster_ios.html (print each to PDF with Chrome)
import base64, random, sys
from pathlib import Path
import segno

ROOT = Path(__file__).resolve().parents[2] / "Assets" / "_Main"
OUT = Path(sys.argv[1])

def b64(path, mime="image/png"):
    return f"data:{mime};base64," + base64.b64encode((ROOT / path).read_bytes()).decode()

POSTERS = {
    "android": dict(
        url="https://drive.google.com/file/d/1dwfys3Au5WMLffiVGgA2EKsDQS6CJ9TQ/view",
        platform="ANDROID", accent="#7ccf3a", accent_light="#c9ff8f", mascot="pip",
        speech="Psst! Phones with Android, this way!",
        steps=["Scan", "Install the app", "Start hunting!"],
        note="Your phone may ask to allow installs from this source. That's expected.",
        link="Google Drive &middot; SpookyCollector.apk"),
    "ios": dict(
        url="https://guileless-dusk-fc1546.netlify.app",
        platform="iPHONE", accent="#8fe9ff", accent_light="#e3fbff", mascot="sheety",
        speech="Boo! iPhones, this one's yours!",
        steps=["Scan", "Open in Safari", "Allow camera &amp; hunt!"],
        note="Tip: Share &rarr; Add to Home Screen keeps your hunt one tap away.",
        link="guileless-dusk-fc1546.netlify.app"),
}

title_font = b64("Fonts/LuckiestGuy-Regular.ttf", "font/ttf")
body_font = b64("Fonts/Fredoka-SemiBold.ttf", "font/ttf")
logo, moon = b64("Art/UI/logo.png"), b64("Art/UI/moon.png")

rng = random.Random(13)
STARS = "".join(f'<circle cx="{rng.uniform(0, 210):.1f}" cy="{rng.uniform(0, 297):.1f}" r="{rng.uniform(.25, .6):.2f}" '
                f'fill="#fff5e0" opacity="{rng.uniform(.25, .75):.2f}"/>' for _ in range(70))

for key, p in POSTERS.items():
    qr = segno.make(p["url"], error="q")
    qr_svg = qr.svg_inline(scale=1, border=0, dark="#2a1240", light=None, omitsize=True)
    steps = "".join(f'<div class="step"><span class="n">{i + 1}</span>{s}</div>' for i, s in enumerate(p["steps"]))
    html = f'''<!doctype html><html><head><meta charset="utf-8"><title>Spooky Collector for {p["platform"]}</title><style>
@font-face {{ font-family: Luckiest; src: url({title_font}); }}
@font-face {{ font-family: Fredoka; src: url({body_font}); }}
@page {{ size: A4; margin: 0; }}
* {{ box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }}
html, body {{ margin: 0; }}
.page {{ position: relative; width: 210mm; height: 297mm; overflow: hidden; font-family: Fredoka; color: #fff5e0;
  background: radial-gradient(120% 70% at 50% 55%, #4a2390 0%, #2a1458 45%, #120828 100%); }}
.stars {{ position: absolute; inset: 0; width: 100%; height: 100%; }}
.moon {{ position: absolute; right: 12mm; top: 10mm; width: 26mm; }}
.logo {{ position: absolute; left: 50%; top: 9mm; width: 92mm; transform: translateX(-50%); }}
.platform {{ position: absolute; left: 50%; top: 45mm; transform: translateX(-50%); white-space: nowrap;
  font-family: Luckiest; font-size: 17mm; line-height: 1; color: #2a1240; letter-spacing: .6mm;
  padding: 4.6mm 11mm 2.4mm; border-radius: 12mm; background: linear-gradient({p["accent_light"]}, {p["accent"]});
  box-shadow: 0 0 0 1.6mm #2a1240, 0 2mm 0 1.6mm rgba(0,0,0,.35); }}
.platform small {{ font-size: 8mm; letter-spacing: .3mm; vertical-align: 3.4mm; margin-right: 2.5mm; }}
.card {{ position: absolute; left: 50%; top: 86mm; width: 160mm; height: 160mm; transform: translateX(-50%);
  background: #fff; border-radius: 9mm; box-shadow: 0 0 0 3mm {p["accent"]}, 0 0 0 4.4mm #2a1240; }}
.qr {{ position: absolute; inset: 12mm; }}
.qr svg {{ width: 100%; height: 100%; display: block; shape-rendering: crispEdges; }}
.mascot {{ position: absolute; right: 3mm; top: 49mm; width: 40mm; transform: rotate(8deg); }}
.speech {{ position: absolute; left: 26mm; top: 70mm; font-size: 5mm; color: #2a1240; background: #fff5e0;
  padding: 1.6mm 4mm 1.8mm; border-radius: 4mm; box-shadow: 0 0 0 .8mm #2a1240; transform: rotate(-2deg); }}
.steps {{ position: absolute; left: 10mm; right: 10mm; top: 259mm; display: flex; justify-content: center; gap: 4mm; }}
.step {{ font-family: Luckiest; font-size: 6mm; letter-spacing: .2mm; white-space: nowrap; color: #fff5e0; }}
.step + .step::before {{ content: "\\2192"; font-family: Fredoka; color: {p["accent"]}; margin-right: 4mm; }}
.n {{ display: inline-block; width: 7.6mm; height: 7.6mm; border-radius: 50%; background: {p["accent"]}; color: #2a1240;
  text-align: center; line-height: 8.6mm; font-size: 5mm; margin-right: 1.8mm; box-shadow: 0 0 0 .6mm #2a1240; vertical-align: .4mm; }}
.note {{ position: absolute; left: 0; right: 0; top: 271mm; text-align: center; font-size: 4mm; color: #d8ccf5; }}
.link {{ position: absolute; left: 0; right: 0; top: 278.5mm; text-align: center; font-size: 3.4mm; color: #9d8cc9; letter-spacing: .1mm; }}
</style></head><body><div class="page">
<svg class="stars" viewBox="0 0 210 297">{STARS}</svg>
<img class="moon" src="{moon}">
<img class="logo" src="{logo}">
<div class="platform"><small>FOR</small>{p["platform"]}</div>
<div class="speech">{p["speech"]}</div>
<div class="card"><div class="qr">{qr_svg}</div></div>
<img class="mascot" src="{b64(f"Art/Characters/{p['mascot']}.png")}">
<div class="steps">{steps}</div>
<div class="note">{p["note"]}</div>
<div class="link">{p["link"]}</div>
</div></body></html>'''
    (OUT / f"poster_{key}.html").write_text(html)
    print(key, "QR version", qr.version, qr.designator)
