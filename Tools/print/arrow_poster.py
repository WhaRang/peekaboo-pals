# A5 sign (top half of an A4 sheet, cut along the dashed line) with a big arrow pointing left (Belarusian text, so Rubik instead of the game's Latin-only fonts).
#   python arrow_poster.py <out.html>   then print to PDF with Chrome.
import base64, random, sys
from pathlib import Path

HERE = Path(__file__).parent
ROOT = Path(__file__).resolve().parents[2] / "Assets" / "_Main"
def b64(path, mime="image/png"):
    return f"data:{mime};base64," + base64.b64encode(Path(path).read_bytes()).decode()

HEADLINE = ("ПЕРШЫ", "МОНСТР!")
ARROW_LINES = ("Лаві яго", "кожныя <tspan fill=\"#5b2a9e\">3 хвіліны!</tspan>")

rubik = b64(HERE / "fonts/Rubik[wght].ttf", "font/ttf")
moon = b64(ROOT / "Art/UI/moon.png")
mascot = b64(ROOT / "Art/Characters/sheety.png")

rng = random.Random(5)
stars = "".join(f'<circle cx="{rng.uniform(0, 297):.1f}" cy="{rng.uniform(0, 210):.1f}" r="{rng.uniform(.3, .7):.2f}" '
                f'fill="#fff5e0" opacity="{rng.uniform(.25, .75):.2f}"/>' for _ in range(70))
SPARK = "M0 -1 Q0.15 -0.15 1 0 Q0.15 0.15 0 1 Q-0.15 0.15 -1 0 Q-0.15 -0.15 0 -1Z"
sparks = "".join(f'<path transform="translate({x} {y}) scale({s})" d="{SPARK}" fill="#fff1a8"/>'
                 for x, y, s in [(22, 30, 3.2), (276, 70, 2.4), (150, 200, 2), (60, 200, 2.6)])

# Arrow pointing left: tip at x=10, head 72 mm long, body 70 mm tall, head 128 mm tall. Rounded by a thick round stroke.
cy = 110
ARROW = f"M10 {cy} L82 {cy - 64} L82 {cy - 35} L284 {cy - 35} L284 {cy + 35} L82 {cy + 35} L82 {cy + 64} Z"

html = f'''<!doctype html><html><head><meta charset="utf-8"><title>Першы монстр</title><style>
@font-face {{ font-family: Rubik; src: url({rubik}); font-weight: 300 900; }}
@page {{ size: A4; margin: 0; }}
* {{ box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }}
html, body {{ margin: 0; width: 210mm; overflow: hidden; }}
.sheet {{ position: relative; width: 210mm; height: 297mm; }}
/* The top half of the A4 sheet holds the sign (A5 minus a margin printers can reach); cut along the dashed line. */
.slot {{ position: relative; width: 210mm; height: 148.5mm; overflow: hidden; }}
.slot {{ border-bottom: 0.3mm dashed #b4a8cc; }}
.fit {{ position: absolute; left: 8.475mm; top: 6mm; width: 193.05mm; height: 136.5mm; border-radius: 6mm; overflow: hidden; }}
.fit svg {{ display: block; width: 100%; height: 100%; }}
</style></head><body>{{SHEET}}</body></html>'''

# The whole sign is one SVG laid out in a 297 x 210 box and scaled by its viewBox: CSS transforms flatten the
# gradients in Chrome's PDF output.
# Two-row headline, centred over the arrow body so it stays clear of the arrow head; the second row overlaps the
# arrow's top edge.
HEAD_Y = cy - 72

def headline(k):
    rows = []
    for i, row in enumerate(HEADLINE):
        y = HEAD_Y + 20 + i * 26
        common = f'x="165" y="{y}" text-anchor="middle" font-family="Rubik" font-weight="900" font-size="28" letter-spacing=".6"'
        rows.append(f'<text {common} fill="#2a1240" stroke="#2a1240" stroke-width="7" stroke-linejoin="round" transform="translate(0 1.6)">{row}</text>'
                    f'<text {common} fill="url(#head{k})" stroke="#2a1240" stroke-width="5" stroke-linejoin="round" paint-order="stroke">{row}</text>')
    return "".join(rows)

def sign(k):
    return f'''<svg viewBox="0 0 297 210">
  <defs>
    <radialGradient id="sky{k}" gradientUnits="userSpaceOnUse" cx="134" cy="126" r="230">
      <stop offset="0" stop-color="#4a2390"/><stop offset=".5" stop-color="#2a1458"/><stop offset="1" stop-color="#120828"/></radialGradient>
    <linearGradient id="head{k}" gradientUnits="userSpaceOnUse" x1="0" y1="{HEAD_Y}" x2="0" y2="{HEAD_Y + 50}">
      <stop offset="0" stop-color="#ffe36b"/><stop offset=".7" stop-color="#ff9a1f"/><stop offset="1" stop-color="#ff7a12"/></linearGradient>
    <linearGradient id="slime{k}" gradientUnits="userSpaceOnUse" x1="0" y1="{cy - 64}" x2="0" y2="{cy + 64}">
      <stop offset="0" stop-color="#d9ffb0"/><stop offset=".55" stop-color="#8fe05a"/><stop offset="1" stop-color="#5fc030"/></linearGradient>
  </defs>
  <rect width="297" height="210" fill="url(#sky{k})"/>
  {stars}{sparks}
  <image href="{moon}" x="255" y="9" width="30" height="30"/>
  <path d="{ARROW}" transform="translate(0 3.2)" fill="#120828" opacity=".55" stroke="#120828" stroke-width="10" stroke-linejoin="round"/>
  <path d="{ARROW}" fill="url(#slime{k})" stroke="#2a1240" stroke-width="10" stroke-linejoin="round"/>
  <path d="M30 {cy - 3} L80 {cy - 50}" stroke="#f4ffe6" stroke-width="3.4" stroke-linecap="round" opacity=".7"/>
  <path d="M96 {cy - 26} L226 {cy - 26}" stroke="#f4ffe6" stroke-width="3" stroke-linecap="round" opacity=".55"/>
  <g transform="rotate(-3 165 {HEAD_Y + 25})">{headline(k)}</g>
  <text x="181" y="{cy + 3}" text-anchor="middle" font-family="Rubik" font-weight="900" font-size="17" fill="#2a1240">{ARROW_LINES[0]}</text>
  <text x="181" y="{cy + 25}" text-anchor="middle" font-family="Rubik" font-weight="900" font-size="17" fill="#2a1240">{ARROW_LINES[1]}</text>
  <!-- The ghost sits on the arrow's top-right corner, peeking out over the edge. -->
  <image href="{mascot}" x="232" y="{cy - 62}" width="62" height="62" transform="rotate(9 263 {cy - 31})"/>
</svg>'''
slots = f'<div class="slot"><div class="fit">{sign(0)}</div></div>'
html = html.replace("{SHEET}", f'<div class="sheet">{slots}</div>')
Path(sys.argv[1]).write_text(html)
