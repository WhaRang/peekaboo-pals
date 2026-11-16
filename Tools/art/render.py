"""Renders SVG markup to transparent PNGs with headless Chrome (fonts from Assets/_Main/Fonts are available as
'Luckiest' and 'Fredoka'). Usage from other scripts: render_many([(name, svg_markup, w, h, out_path), ...])."""
import os
import subprocess
import tempfile
from concurrent.futures import ThreadPoolExecutor

CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FONTS = os.path.join(ROOT, "Assets", "_Main", "Fonts")

HTML = """<!doctype html><html><head><meta charset="utf-8"><style>
@font-face {{ font-family: 'Luckiest'; src: url('file://{fonts}/LuckiestGuy-Regular.ttf'); }}
@font-face {{ font-family: 'Fredoka'; src: url('file://{fonts}/Fredoka-SemiBold.ttf'); }}
html, body {{ margin: 0; padding: 0; background: transparent; overflow: hidden; }}
svg {{ display: block; }}
</style></head><body>
<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {vw} {vh}">{body}</svg>
</body></html>"""


def render(svg_body, w, h, out_path, view=None):
    vw, vh = view or (w, h)
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False) as f:
        f.write(HTML.format(fonts=FONTS, w=w, h=h, vw=vw, vh=vh, body=svg_body))
        html_path = f.name
    try:
        subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                        "--default-background-color=00000000", "--allow-file-access-from-files",
                        "--virtual-time-budget=1500", f"--window-size={w},{h}", f"--screenshot={out_path}",
                        "file://" + html_path], check=True, capture_output=True)
    finally:
        os.unlink(html_path)
    return out_path


def render_many(jobs, workers=6):
    with ThreadPoolExecutor(workers) as pool:
        return list(pool.map(lambda j: render(*j), jobs))
