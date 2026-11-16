"""Regenerates every PNG under Assets/_Main/Art (app UI + brand) and the Halloween 2026 pack art in
Packs/halloween-2026 from the SVG sources in this folder.
Run:  python3 Tools/art/build.py   (needs Google Chrome + Pillow)"""
import os
import shutil
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
import brand  # noqa: E402
import characters  # noqa: E402
import render  # noqa: E402
import ui  # noqa: E402

ART = os.path.join(render.ROOT, "Assets", "_Main", "Art")
HALLOWEEN = os.path.join(render.ROOT, "Packs", "halloween-2026")

# The app itself is generic (Peekaboo Pals); the Halloween look moved into its pack.
APP_UI = dict(ui.UI)
APP_UI.pop("icon_ghost", None)
APP_UI.update({
    "logo": (brand.logo(), 940, 400, 0),
    "hills": (brand.hills(), 1080, 560, 0),
    "icon_pal": (brand.icon_pal(), 256, 256, 0),
    "icon_albums": (brand.icon_albums(), 256, 256, 0),
    "app_icon": (brand.app_icon(), 1024, 1024, 0),
})


def silhouette(src, dst):
    image = Image.open(src).convert("RGBA")
    alpha = image.getchannel("A")
    # Drop the soft ground shadow (semi-transparent) so the silhouette is just the character.
    alpha = alpha.point(lambda a: 255 if a > 200 else (0 if a < 90 else a))
    white = Image.new("RGBA", image.size, (255, 255, 255, 0))
    white.putalpha(alpha)
    white.save(dst)


def main():
    jobs = []
    for key, fn in characters.CHARACTERS.items():
        jobs.append((fn(), 512, 512, os.path.join(ART, "Characters", f"{key}.png")))
    for name, (svg, w, h, _border) in APP_UI.items():
        jobs.append((svg, w, h, os.path.join(ART, "UI", f"{name}.png")))
    jobs.append((brand.halloween_logo(), 940, 340, os.path.join(HALLOWEEN, "logo.png")))
    jobs.append((ui.hills(), 1080, 560, os.path.join(HALLOWEEN, "scenery.png")))

    render.render_many(jobs)

    os.makedirs(os.path.join(HALLOWEEN, "art"), exist_ok=True)
    for key in characters.CHARACTERS:
        shutil.copyfile(os.path.join(ART, "Characters", f"{key}.png"), os.path.join(HALLOWEEN, "art", f"{key}.png"))

    for key in characters.CHARACTERS:
        silhouette(os.path.join(ART, "Characters", f"{key}.png"), os.path.join(ART, "Characters", f"{key}_silhouette.png"))

    print(f"Rendered {len(jobs)} images + {len(characters.CHARACTERS)} silhouettes into {ART}")


if __name__ == "__main__":
    main()
