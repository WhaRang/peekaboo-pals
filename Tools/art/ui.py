"""UI sprites, icons, particle atlas, logo and backgrounds (CC0, made for this project).
Each entry: name -> (svg_body, width, height, nine_slice_border_px or 0). White/grey sprites are tinted in Unity."""
import math
import random

from characters import INK, sparkle

W = "#ffffff"


def rounded(w, h, r, fill=W, extra=""):
    return f'<rect x="0" y="0" width="{w}" height="{h}" rx="{r}" fill="{fill}" {extra}/>'


def star_path(cx, cy, r_out, r_in, points=5, rot=-90):
    pts = []
    for i in range(points * 2):
        r = r_out if i % 2 == 0 else r_in
        a = math.radians(rot + i * 180 / points)
        pts.append(f"{cx + r * math.cos(a):.1f},{cy + r * math.sin(a):.1f}")
    return "M" + " L".join(pts) + " Z"


def bat_path(cx, cy, s):
    # Simple cute bat silhouette, s = half width.
    k = s / 100
    def p(x, y):
        return f"{cx + x * k:.1f} {cy + y * k:.1f}"
    return (f"M{p(0, -30)} C{p(10, -30)} {p(14, -22)} {p(16, -14)} L{p(22, -36)} L{p(26, -10)} "
            f"C{p(50, -40)} {p(84, -36)} {p(100, -14)} C{p(86, -10)} {p(80, 2)} {p(82, 14)} "
            f"C{p(70, 4)} {p(56, 6)} {p(50, 18)} C{p(40, 8)} {p(28, 10)} {p(20, 22)} C{p(12, 28)} {p(-12, 28)} {p(-20, 22)} "
            f"C{p(-28, 10)} {p(-40, 8)} {p(-50, 18)} C{p(-56, 6)} {p(-70, 4)} {p(-82, 14)} C{p(-80, 2)} {p(-86, -10)} {p(-100, -14)} "
            f"C{p(-84, -36)} {p(-50, -40)} {p(-26, -10)} L{p(-22, -36)} L{p(-16, -14)} C{p(-14, -22)} {p(-10, -30)} {p(0, -30)} Z")


def ghost_path(cx, cy, s):
    k = s / 100
    def p(x, y):
        return f"{cx + x * k:.1f} {cy + y * k:.1f}"
    return (f"M{p(-70, 10)} C{p(-70, -60)} {p(-38, -95)} {p(0, -95)} C{p(38, -95)} {p(70, -60)} {p(70, 10)} L{p(70, 80)} "
            f"C{p(58, 100)} {p(46, 74)} {p(35, 88)} C{p(22, 104)} {p(10, 76)} {p(0, 90)} C{p(-10, 104)} {p(-22, 76)} {p(-35, 88)} "
            f"C{p(-46, 100)} {p(-58, 74)} {p(-70, 80)} Z")


def defs_grad(id_, stops, x2="0", y2="1", radial=False):
    tag = "radialGradient" if radial else "linearGradient"
    attrs = 'cx="50%" cy="50%" r="50%"' if radial else f'x1="0" y1="0" x2="{x2}" y2="{y2}"'
    s = "".join(f'<stop offset="{o}" stop-color="{c}" stop-opacity="{a}"/>' for o, c, a in stops)
    return f'<{tag} id="{id_}" {attrs}>{s}</{tag}>'


# ---------------------------------------------------------------- basic shapes

def soft_glow(size=256):
    g = defs_grad("g", [(0, W, 1), (0.35, W, 0.55), (0.7, W, 0.15), (1, W, 0)], radial=True)
    return f'<defs>{g}</defs><circle cx="{size / 2}" cy="{size / 2}" r="{size / 2}" fill="url(#g)"/>'


def card_frame():
    g = defs_grad("f", [(0, "#ffffff", 1), (1, "#c9c9c9", 1)])
    return (f'<defs>{g}</defs>{rounded(160, 160, 44, "url(#f)")}'
            f'<rect x="9" y="9" width="142" height="142" rx="36" fill="none" stroke="#ffffff" stroke-opacity="0.7" stroke-width="4"/>'
            f'<rect x="16" y="16" width="128" height="128" rx="30" fill="none" stroke="#000000" stroke-opacity="0.18" stroke-width="4"/>')


def card_inner():
    g = defs_grad("i", [(0, "#ffffff", 1), (0.6, "#d0d0d0", 1), (1, "#8f8f8f", 1)], radial=True)
    random.seed(7)
    dots = "".join(f'<path d="{star_path(random.uniform(20, 280), random.uniform(20, 388), s, s * 0.45, 4)}" fill="#ffffff" opacity="{random.uniform(0.15, 0.4):.2f}"/>'
                   for s in [random.uniform(4, 10) for _ in range(28)])
    return f'<defs>{g}</defs>{rounded(300, 408, 28, "url(#i)")}{dots}'


def card_back():
    g = defs_grad("b", [(0, "#ffffff", 1), (1, "#bdbdbd", 1)])
    stripes = "".join(f'<path d="M{x} -20 L{x + 60} -20 L{x - 340} 600 L{x - 400} 600 Z" fill="#000" opacity="0.06"/>' for x in range(0, 900, 120))
    emblem = (f'<circle cx="200" cy="272" r="118" fill="#000" opacity="0.18"/>'
              f'<circle cx="200" cy="272" r="104" fill="none" stroke="#fff" stroke-width="10" opacity="0.85"/>'
              f'<text x="200" y="318" text-anchor="middle" font-family="Luckiest" font-size="150" fill="#fff" stroke="{INK}" stroke-width="10" paint-order="stroke">?</text>')
    corner = "".join(f'<path d="{star_path(x, y, 18, 8, 4)}" fill="#fff" opacity="0.8"/>' for x, y in [(52, 52), (348, 52), (52, 492), (348, 492)])
    return (f'<defs>{g}<clipPath id="c"><rect width="400" height="544" rx="40"/></clipPath></defs>'
            f'<g clip-path="url(#c)">{rounded(400, 544, 40, "url(#b)")}{stripes}</g>'
            f'<rect x="18" y="18" width="364" height="508" rx="28" fill="none" stroke="#fff" stroke-width="8" opacity="0.8"/>'
            f'{emblem}{corner}')


def shine():
    g = defs_grad("s", [(0, W, 0), (0.5, W, 1), (1, W, 0)], x2="1", y2="0")
    return f'<defs>{g}</defs><rect width="64" height="256" fill="url(#s)"/>'


def rays():
    n = 18
    wedges = []
    for i in range(n):
        a0 = math.radians(i * 360 / n)
        a1 = math.radians(i * 360 / n + 360 / n * 0.5)
        wedges.append(f"M512 512 L{512 + 760 * math.cos(a0):.1f} {512 + 760 * math.sin(a0):.1f} L{512 + 760 * math.cos(a1):.1f} {512 + 760 * math.sin(a1):.1f} Z")
    g = defs_grad("r", [(0, W, 1), (0.25, W, 0.8), (1, W, 0)], radial=True)
    return f'<defs>{g}<mask id="m"><rect width="1024" height="1024" fill="url(#r)"/></mask></defs><path d="{" ".join(wedges)}" fill="#fff" mask="url(#m)"/>'


def gradient_vertical():
    g = defs_grad("v", [(0, W, 1), (1, W, 0)])
    return f'<defs>{g}</defs><rect width="16" height="256" fill="url(#v)"/>'


def vignette():
    g = defs_grad("v", [(0, "#070214", 0), (0.62, "#070214", 0), (1, "#070214", 0.85)], radial=True)
    return f'<defs>{g}</defs><rect width="512" height="512" fill="url(#v)"/>'


def scan_corner():
    return f'<path d="M30 20 L30 140 Q30 150 40 150 L150 150" fill="none" stroke="#fff" stroke-width="34" stroke-linecap="round" stroke-linejoin="round"/>'


def banner():
    return (f'<path d="M20 70 L120 70 L100 130 L120 190 L20 190 L50 130 Z" fill="#b5b5b5"/>'
            f'<path d="M800 70 L700 70 L720 130 L700 190 L800 190 L770 130 Z" fill="#b5b5b5"/>'
            f'<path d="M110 168 L150 190 L150 150 Z M710 168 L670 190 L670 150 Z" fill="#7a7a7a"/>'
            f'<rect x="100" y="20" width="620" height="150" rx="30" fill="#fff"/>'
            f'<rect x="100" y="20" width="620" height="60" rx="30" fill="#fff" opacity="0.6"/>'
            f'<rect x="118" y="36" width="584" height="118" rx="22" fill="none" stroke="#000" stroke-opacity="0.15" stroke-width="6"/>')


def badge():
    return (f'<rect x="6" y="6" width="208" height="84" rx="42" fill="#fff"/>'
            f'<rect x="16" y="14" width="188" height="30" rx="15" fill="#fff" opacity="0.5"/>'
            f'<rect x="6" y="6" width="208" height="84" rx="42" fill="none" stroke="{INK}" stroke-width="8"/>')


# ---------------------------------------------------------------- icons (256, full color)

def ic(body):
    return body


def icon_scan():
    corners = "".join(f'<path d="{d}" fill="none" stroke="{INK}" stroke-width="40" stroke-linecap="round" stroke-linejoin="round"/>'
                      f'<path d="{d}" fill="none" stroke="#fff" stroke-width="20" stroke-linecap="round" stroke-linejoin="round"/>'
                      for d in ["M40 90 L40 40 L90 40", "M166 40 L216 40 L216 90", "M216 166 L216 216 L166 216", "M90 216 L40 216 L40 166"])
    blocks = "".join(f'<rect x="{x}" y="{y}" width="{s}" height="{s}" rx="6" fill="{INK}"/>' for x, y, s in
                     [(78, 78, 40), (138, 78, 40), (78, 138, 40), (146, 146, 18), (128, 128, 14), (166, 166, 14)])
    return (f'<rect x="64" y="64" width="128" height="128" rx="16" fill="#fff" stroke="{INK}" stroke-width="10"/>{blocks}{corners}'
            f'<rect x="28" y="122" width="200" height="12" rx="6" fill="#7cff4f" stroke="{INK}" stroke-width="5"/>')


def icon_collection():
    def card(x, y, rot, color):
        return (f'<g transform="rotate({rot} {x + 50} {y + 70})"><rect x="{x}" y="{y}" width="100" height="140" rx="16" fill="{color}" stroke="{INK}" stroke-width="10"/>'
                f'<rect x="{x + 14}" y="{y + 14}" width="72" height="84" rx="10" fill="#fff" opacity="0.35"/></g>')
    return (card(36, 70, -18, "#ff8a1f") + card(120, 70, 18, "#7cff4f") + card(78, 52, 0, "#9b6cff")
            + f'<text x="128" y="160" text-anchor="middle" font-family="Luckiest" font-size="84" fill="#fff" stroke="{INK}" stroke-width="10" paint-order="stroke">?</text>')


def icon_results():
    return (f'<path d="M84 24 L120 120 L92 134 L56 38 Z" fill="#ff5fa8" stroke="{INK}" stroke-width="9" stroke-linejoin="round"/>'
            f'<path d="M172 24 L136 120 L164 134 L200 38 Z" fill="#3fa9f5" stroke="{INK}" stroke-width="9" stroke-linejoin="round"/>'
            f'<circle cx="128" cy="160" r="70" fill="#ffd23f" stroke="{INK}" stroke-width="10"/>'
            f'<circle cx="128" cy="160" r="50" fill="#ffb52e"/>'
            f'<path d="{star_path(128, 162, 38, 16)}" fill="#fff3b0" stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>'
            f'<ellipse cx="104" cy="126" rx="14" ry="8" fill="#fff" opacity="0.6" transform="rotate(-30 104 126)"/>')


def speaker():
    return (f'<path d="M40 100 L80 100 L132 52 L132 204 L80 156 L40 156 Z" fill="#fff" stroke="{INK}" stroke-width="12" stroke-linejoin="round"/>')


def icon_sound_on():
    return speaker() + "".join(f'<path d="M{150 + i * 26} {94 - i * 22} Q{184 + i * 36} 128 {150 + i * 26} {162 + i * 22}" fill="none" stroke="{INK}" stroke-width="24" stroke-linecap="round"/>'
                               f'<path d="M{150 + i * 26} {94 - i * 22} Q{184 + i * 36} 128 {150 + i * 26} {162 + i * 22}" fill="none" stroke="#fff" stroke-width="10" stroke-linecap="round"/>' for i in range(2))


def icon_sound_off():
    x = "M156 92 L220 164 M220 92 L156 164"
    return speaker() + f'<path d="{x}" stroke="{INK}" stroke-width="30" stroke-linecap="round"/><path d="{x}" stroke="#ff5164" stroke-width="14" stroke-linecap="round"/>'


def icon_close():
    x = "M70 70 L186 186 M186 70 L70 186"
    return f'<path d="{x}" stroke="{INK}" stroke-width="54" stroke-linecap="round"/><path d="{x}" stroke="#fff" stroke-width="30" stroke-linecap="round"/>'


def icon_candy():
    stripes = "".join(f'<path d="M{x} 86 L{x + 26} 170" stroke="#fff" stroke-width="14" opacity="0.9"/>' for x in range(84, 172, 30))
    return (f'<path d="M80 128 L24 84 L36 128 L24 172 Z" fill="#ffd23f" stroke="{INK}" stroke-width="10" stroke-linejoin="round"/>'
            f'<path d="M176 128 L232 84 L220 128 L232 172 Z" fill="#ffd23f" stroke="{INK}" stroke-width="10" stroke-linejoin="round"/>'
            f'<defs><clipPath id="cc"><ellipse cx="128" cy="128" rx="58" ry="46"/></clipPath></defs>'
            f'<ellipse cx="128" cy="128" rx="58" ry="46" fill="#ff5fa8"/><g clip-path="url(#cc)">{stripes}</g>'
            f'<ellipse cx="128" cy="128" rx="58" ry="46" fill="none" stroke="{INK}" stroke-width="10"/>'
            f'<ellipse cx="110" cy="108" rx="16" ry="8" fill="#fff" opacity="0.7" transform="rotate(-20 110 108)"/>')


def icon_star():
    return (f'<path d="{star_path(128, 136, 108, 48)}" fill="#fff" stroke="{INK}" stroke-width="14" stroke-linejoin="round"/>'
            f'<ellipse cx="104" cy="104" rx="18" ry="10" fill="#fff" opacity="0.9" transform="rotate(-30 104 104)"/>')


def icon_clock():
    return (f'<circle cx="128" cy="134" r="96" fill="#fff" stroke="{INK}" stroke-width="12"/>'
            f'<path d="M128 134 L128 76 M128 134 L170 152" stroke="#7a49b8" stroke-width="16" stroke-linecap="round"/>'
            f'<circle cx="128" cy="134" r="12" fill="{INK}"/>'
            f'<rect x="104" y="18" width="48" height="26" rx="10" fill="#ff8a1f" stroke="{INK}" stroke-width="8"/>')


def icon_ghost():
    return (f'<path d="{ghost_path(128, 132, 100)}" fill="#fff" stroke="{INK}" stroke-width="12" stroke-linejoin="round"/>'
            f'<ellipse cx="98" cy="110" rx="14" ry="20" fill="{INK}"/><ellipse cx="158" cy="110" rx="14" ry="20" fill="{INK}"/>'
            f'<circle cx="94" cy="103" r="5" fill="#fff"/><circle cx="154" cy="103" r="5" fill="#fff"/>'
            f'<ellipse cx="128" cy="150" rx="12" ry="16" fill="{INK}"/>'
            f'<ellipse cx="80" cy="140" rx="12" ry="7" fill="#ff5fa8" opacity="0.6"/><ellipse cx="176" cy="140" rx="12" ry="7" fill="#ff5fa8" opacity="0.6"/>')


def icon_camera():
    return (f'<rect x="92" y="40" width="72" height="40" rx="12" fill="#5b2a9e" stroke="{INK}" stroke-width="10"/>'
            f'<rect x="24" y="64" width="208" height="150" rx="34" fill="#9b6cff" stroke="{INK}" stroke-width="12"/>'
            f'<circle cx="128" cy="140" r="54" fill="#2a1650" stroke="{INK}" stroke-width="10"/>'
            f'<circle cx="128" cy="140" r="32" fill="#7cff4f"/><circle cx="114" cy="126" r="11" fill="#fff"/>'
            f'<circle cx="198" cy="94" r="9" fill="#ffd23f"/>')


def icon_keyboard():
    keys = "".join(f'<rect x="{46 + c * 42}" y="{88 + r * 40}" width="32" height="30" rx="7" fill="#fff"/>' for r in range(2) for c in range(4))
    return (f'<rect x="24" y="64" width="208" height="140" rx="26" fill="#5b2a9e" stroke="{INK}" stroke-width="12"/>{keys}'
            f'<rect x="72" y="168" width="112" height="22" rx="8" fill="#fff"/>')


def icon_power():
    bolt = "M150 20 L64 146 L120 146 L100 236 L192 104 L134 104 Z"
    return f'<path d="{bolt}" fill="#fff" stroke="{INK}" stroke-width="14" stroke-linejoin="round"/>'


def icon_trophy():
    return (f'<path d="M70 48 C20 48 20 120 80 128 M186 48 C236 48 236 120 176 128" fill="none" stroke="{INK}" stroke-width="26" stroke-linecap="round"/>'
            f'<path d="M70 48 C20 48 20 120 80 128 M186 48 C236 48 236 120 176 128" fill="none" stroke="#ffd23f" stroke-width="12" stroke-linecap="round"/>'
            f'<path d="M64 32 L192 32 L184 110 C178 150 150 168 128 168 C106 168 78 150 72 110 Z" fill="#ffd23f" stroke="{INK}" stroke-width="12" stroke-linejoin="round"/>'
            f'<rect x="112" y="164" width="32" height="34" fill="#f0b81f" stroke="{INK}" stroke-width="10"/>'
            f'<rect x="74" y="196" width="108" height="36" rx="10" fill="#7a49b8" stroke="{INK}" stroke-width="12"/>'
            f'<path d="{star_path(128, 92, 30, 13)}" fill="#fff3b0"/>'
            f'<ellipse cx="94" cy="66" rx="10" ry="22" fill="#fff" opacity="0.6"/>')


# ---------------------------------------------------------------- particle atlas (512, 4x4 cells of 128)

def particle_atlas():
    cells = []

    def cell(i, body):
        x, y = (i % 4) * 128, (i // 4) * 128
        cells.append(f'<g transform="translate({x} {y})">{body}</g>')

    soft = defs_grad("pd", [(0, W, 1), (0.4, W, 0.6), (1, W, 0)], radial=True)
    ember = defs_grad("pe", [(0, W, 1), (0.18, W, 1), (0.4, W, 0.35), (1, W, 0)], radial=True)
    ring = defs_grad("pr", [(0, W, 0), (0.72, W, 0), (0.82, W, 1), (0.9, W, 0.6), (1, W, 0)], radial=True)
    streak = defs_grad("ps", [(0, W, 0), (0.5, W, 1), (1, W, 0)], x2="1", y2="0")
    defs = f'<defs>{soft}{ember}{ring}{streak}<filter id="blur"><feGaussianBlur stdDeviation="9"/></filter></defs>'

    cell(0, f'<path d="{star_path(64, 68, 56, 24)}" fill="#fff"/>')
    cell(1, f'<circle cx="64" cy="64" r="26" fill="url(#pd)"/>' + sparkle(64, 64, 58))
    cell(2, '<rect x="40" y="22" width="48" height="84" rx="10" fill="#fff"/><rect x="40" y="22" width="20" height="84" rx="8" fill="#000" opacity="0.15"/>')
    cell(3, '<circle cx="64" cy="64" r="62" fill="url(#pd)"/>')
    cell(4, '<circle cx="64" cy="64" r="62" fill="url(#pe)"/>')
    cell(5, '<g filter="url(#blur)" opacity="0.9"><circle cx="48" cy="70" r="32" fill="#fff"/><circle cx="78" cy="58" r="34" fill="#fff"/><circle cx="70" cy="82" r="24" fill="#fff"/></g>')
    cell(6, f'<path d="{bat_path(64, 66, 60)}" fill="#fff"/>')
    stripes = "".join(f'<path d="M{x} 40 L{x + 18} 88" stroke="#000" stroke-opacity="0.18" stroke-width="10"/>' for x in range(40, 90, 22))
    cell(7, f'<path d="M40 64 L8 42 L16 64 L8 86 Z M88 64 L120 42 L112 64 L120 86 Z" fill="#fff"/>'
            f'<defs><clipPath id="cnd"><ellipse cx="64" cy="64" rx="30" ry="24"/></clipPath></defs><ellipse cx="64" cy="64" rx="30" ry="24" fill="#fff"/><g clip-path="url(#cnd)">{stripes}</g>')
    cell(8, '<ellipse cx="44" cy="72" rx="28" ry="38" fill="#fff"/><ellipse cx="84" cy="72" rx="28" ry="38" fill="#fff"/><ellipse cx="64" cy="70" rx="30" ry="42" fill="#fff"/>'
            '<path d="M44 40 Q48 76 46 108 M84 40 Q80 76 82 108" stroke="#000" stroke-opacity="0.15" stroke-width="5" fill="none"/>'
            '<rect x="58" y="16" width="12" height="20" rx="4" fill="#fff"/>'
            '<path d="M44 64 L54 54 L56 68 Z M84 64 L74 54 L72 68 Z M44 84 Q64 100 84 84 Q64 92 44 84 Z" fill="#000" opacity="0.35"/>')
    cell(9, '<circle cx="64" cy="64" r="62" fill="url(#pr)"/>')
    cell(10, '<rect x="0" y="54" width="128" height="20" rx="10" fill="url(#ps)"/><rect x="24" y="60" width="80" height="8" rx="4" fill="#fff"/>')
    cell(11, '<rect x="30" y="30" width="68" height="68" rx="12" fill="#fff"/>')
    cell(12, '<path d="M64 108 C30 84 14 64 14 44 C14 28 26 18 40 18 C52 18 60 26 64 36 C68 26 76 18 88 18 C102 18 114 28 114 44 C114 64 98 84 64 108 Z" fill="#fff"/>')
    cell(13, f'<path d="{ghost_path(64, 66, 54)}" fill="#fff"/><ellipse cx="50" cy="56" rx="7" ry="10" fill="#000" opacity="0.5"/><ellipse cx="78" cy="56" rx="7" ry="10" fill="#000" opacity="0.5"/>')
    spiral = " ".join(f"{64 + (6 + t * 2.6) * math.cos(t * 0.5):.1f},{64 + (6 + t * 2.6) * math.sin(t * 0.5):.1f}" for t in range(0, 22))
    cell(14, f'<polyline points="{spiral}" fill="none" stroke="#fff" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>')
    cell(15, '<circle cx="64" cy="64" r="56" fill="#fff"/>')
    flame = defs_grad("pf", [(0, W, 1), (0.45, W, 0.85), (1, W, 0)], radial=True)
    cells.insert(0, f'<defs>{flame}<filter id="fb"><feGaussianBlur stdDeviation="3"/></filter><filter id="sb"><feGaussianBlur stdDeviation="8"/></filter></defs>')
    # 16 flame: teardrop with a hot core
    cell(16, '<path d="M64 8 C78 38 104 58 104 84 C104 108 86 122 64 122 C42 122 24 108 24 84 C24 60 50 40 64 8 Z" fill="url(#pf)" filter="url(#fb)"/>'
             '<ellipse cx="64" cy="92" rx="18" ry="24" fill="#fff"/>')
    # 17 smoke: soft lumpy puff
    cell(17, '<g filter="url(#sb)" opacity="0.8"><circle cx="50" cy="72" r="30" fill="#fff"/><circle cx="76" cy="62" r="32" fill="#fff"/><circle cx="66" cy="86" r="26" fill="#fff"/></g>')
    # 18 flame tongue: tall wavy flame
    cell(18, '<path d="M64 4 C72 28 88 40 84 66 C82 82 92 90 88 104 C84 118 74 124 64 124 C52 124 40 116 40 98 C40 84 50 78 48 62 C46 44 58 30 64 4 Z" fill="url(#pf)" filter="url(#fb)"/>'
             '<path d="M64 60 C70 74 76 82 74 96 C72 108 68 112 64 112 C58 112 54 106 54 98 C54 86 62 78 64 60 Z" fill="#fff"/>')
    # 19 spark: bright dot with a short trail
    cell(19, '<rect x="14" y="58" width="80" height="12" rx="6" fill="url(#ps)"/><circle cx="96" cy="64" r="14" fill="url(#pe)"/><circle cx="96" cy="64" r="6" fill="#fff"/>')
    return defs + "".join(cells)


def icon_edit():
    pencil = "M176 30 L226 80 L104 202 L40 218 L56 154 Z"
    return (f'<path d="{pencil}" fill="#ffd23f" stroke="{INK}" stroke-width="14" stroke-linejoin="round"/>'
            f'<path d="M152 54 L202 104" stroke="{INK}" stroke-width="12"/>'
            f'<path d="M56 154 L104 202 L40 218 Z" fill="#ffe9c4" stroke="{INK}" stroke-width="12" stroke-linejoin="round"/>'
            f'<path d="M176 30 L226 80 L206 100 L156 50 Z" fill="#ff5fa8" stroke="{INK}" stroke-width="12" stroke-linejoin="round"/>')


def icon_info():
    return (f'<rect x="100" y="100" width="56" height="124" rx="22" fill="#fff" stroke="{INK}" stroke-width="14"/>'
            f'<circle cx="128" cy="58" r="32" fill="#fff" stroke="{INK}" stroke-width="14"/>')


def icon_dice():
    pips = "".join(f'<circle cx="{x}" cy="{y}" r="15" fill="{INK}"/>' for x, y in [(86, 86), (170, 86), (128, 128), (86, 170), (170, 170)])
    return (f'<rect x="34" y="34" width="188" height="188" rx="40" fill="#fff" stroke="{INK}" stroke-width="14"/>'
            f'<rect x="50" y="46" width="156" height="50" rx="22" fill="#fff" opacity="0.5"/>{pips}')


def card_frame_metal():
    g = defs_grad("mt", [(0, "#ffffff", 1), (0.18, "#8c8c8c", 1), (0.32, "#ffffff", 1), (0.5, "#6e6e6e", 1), (0.68, "#f2f2f2", 1), (0.86, "#8a8a8a", 1), (1, "#ffffff", 1)], x2="1", y2="1")
    return (f'<defs>{g}<mask id="ring"><rect width="160" height="160" rx="44" fill="#fff"/><rect x="20" y="20" width="120" height="120" rx="28" fill="#000"/></mask></defs>'
            f'<rect width="160" height="160" rx="44" fill="url(#mt)" mask="url(#ring)"/>'
            f'<rect x="5" y="5" width="150" height="150" rx="40" fill="none" stroke="#fff" stroke-width="3"/>')


def scratches():
    random.seed(23)
    lines = []
    for _ in range(26):
        x, y = random.uniform(0, 300), random.uniform(0, 408)
        a = math.radians(random.uniform(-70, -20))
        l = random.uniform(30, 140)
        lines.append(f'<path d="M{x:.0f} {y:.0f} q{l * 0.5 * math.cos(a) + random.uniform(-8, 8):.0f} {l * 0.5 * math.sin(a):.0f} {l * math.cos(a):.0f} {l * math.sin(a):.0f}" '
                     f'stroke="#fff" stroke-width="{random.uniform(1.5, 4):.1f}" stroke-linecap="round" fill="none" opacity="{random.uniform(0.4, 1):.2f}"/>')
    specks = "".join(f'<circle cx="{random.uniform(0, 300):.0f}" cy="{random.uniform(0, 408):.0f}" r="{random.uniform(1, 3.5):.1f}" fill="#fff" opacity="{random.uniform(0.3, 0.9):.2f}"/>' for _ in range(60))
    return "".join(lines) + specks


def chip_metal():
    g = defs_grad("cm", [(0, "#ffffff", 1), (0.45, "#b8b8b8", 1), (0.55, "#8a8a8a", 1), (1, "#f0f0f0", 1)])
    return f'<defs>{g}</defs><rect width="160" height="80" rx="40" fill="url(#cm)"/><rect x="10" y="8" width="140" height="24" rx="12" fill="#fff" opacity="0.7"/>'


# ---------------------------------------------------------------- logo + scenery

def logo():
    g1 = defs_grad("lg1", [(0, "#ffe066", 1), (0.5, "#ffa12b", 1), (1, "#ff6a13", 1)])
    g2 = defs_grad("lg2", [(0, "#d9ffb8", 1), (1, "#5fd630", 1)])
    return (f'<defs>{g1}{g2}</defs>'
            f'<path d="{bat_path(470, 46, 70)}" fill="{INK}"/>'
            f'<circle cx="455" cy="40" r="5" fill="#ffd23f"/><circle cx="485" cy="40" r="5" fill="#ffd23f"/>'
            f'<text x="470" y="215" text-anchor="middle" font-family="Luckiest" font-size="196" letter-spacing="6" fill="{INK}" stroke="{INK}" stroke-width="44" stroke-linejoin="round" transform="translate(6 12)">SPOOKY</text>'
            f'<text x="470" y="215" text-anchor="middle" font-family="Luckiest" font-size="196" letter-spacing="6" fill="url(#lg1)" stroke="{INK}" stroke-width="22" stroke-linejoin="round" paint-order="stroke">SPOOKY</text>'
            f'<text x="470" y="318" text-anchor="middle" font-family="Luckiest" font-size="104" letter-spacing="10" fill="{INK}" stroke="{INK}" stroke-width="30" stroke-linejoin="round" transform="translate(4 8)">COLLECTOR</text>'
            f'<text x="470" y="318" text-anchor="middle" font-family="Luckiest" font-size="104" letter-spacing="10" fill="url(#lg2)" stroke="{INK}" stroke-width="16" stroke-linejoin="round" paint-order="stroke">COLLECTOR</text>'
            + sparkle(90, 90, 22, "#fff3b0") + sparkle(860, 120, 18, "#fff") + sparkle(830, 260, 12, "#ffd23f"))


def background():
    g = defs_grad("sky", [(0, "#090318", 1), (0.45, "#1e0d40", 1), (0.8, "#3a1a62", 1), (1, "#4b2270", 1)])
    haze = defs_grad("haze", [(0, "#8a4dd6", 0.45), (1, "#8a4dd6", 0)], radial=True)
    random.seed(11)
    stars = "".join(f'<circle cx="{random.uniform(0, 1080):.0f}" cy="{random.uniform(0, 1150):.0f}" r="{random.uniform(1, 3.2):.1f}" fill="#fff" opacity="{random.uniform(0.25, 0.8):.2f}"/>' for _ in range(90))
    clouds = "".join(f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="#b98cff" opacity="0.07"/>' for x, y, rx, ry in
                     [(200, 700, 320, 70), (820, 560, 300, 60), (600, 900, 380, 80), (100, 1100, 280, 60), (900, 1250, 340, 70)])
    return (f'<defs>{g}{haze}<filter id="cb"><feGaussianBlur stdDeviation="24"/></filter></defs>'
            f'<rect width="1080" height="1920" fill="url(#sky)"/>'
            f'<ellipse cx="540" cy="1500" rx="900" ry="600" fill="url(#haze)"/>'
            f'{stars}<g filter="url(#cb)">{clouds}</g>')


def moon():
    glow = defs_grad("mg", [(0, "#fff7d6", 1), (1, "#ffe08a", 1)], radial=True)
    return (f'<defs>{glow}</defs><circle cx="150" cy="150" r="120" fill="url(#mg)"/>'
            f'<circle cx="110" cy="120" r="22" fill="#f2cf73" opacity="0.6"/><circle cx="180" cy="180" r="30" fill="#f2cf73" opacity="0.5"/>'
            f'<circle cx="170" cy="96" r="12" fill="#f2cf73" opacity="0.6"/><circle cx="100" cy="196" r="14" fill="#f2cf73" opacity="0.5"/>'
            f'<circle cx="150" cy="150" r="120" fill="none" stroke="#fff" stroke-opacity="0.6" stroke-width="6"/>')


def hills():
    fade = defs_grad("hf", [(0, "#1a0b33", 0), (0.25, "#1a0b33", 1), (1, "#0d051c", 1)])
    back = "M0 220 C160 150 300 170 440 210 C600 255 760 140 1080 180 L1080 560 L0 560 Z"
    front = "M0 330 C200 270 380 300 560 330 C760 365 900 290 1080 310 L1080 560 L0 560 Z"
    tombs = "".join(
        f'<path d="M{x - 30} {y} L{x - 30} {y - 52} Q{x - 30} {y - 84} {x} {y - 84} Q{x + 30} {y - 84} {x + 30} {y - 52} L{x + 30} {y} Z" fill="#3a2366" stroke="{INK}" stroke-width="8" transform="rotate({r} {x} {y})"/>'
        f'<path d="M{x - 12} {y - 50} L{x + 12} {y - 50} M{x} {y - 62} L{x} {y - 30}" stroke="#2a1650" stroke-width="6" stroke-linecap="round" transform="rotate({r} {x} {y})"/>'
        for x, y, r in [(120, 330, -6), (310, 318, 4), (820, 330, -3), (980, 322, 7)])

    def pumpkin(x, y, s):
        return (f'<g transform="translate({x} {y}) scale({s})"><ellipse cx="-22" cy="0" rx="26" ry="30" fill="#e8650e" stroke="{INK}" stroke-width="6"/>'
                f'<ellipse cx="22" cy="0" rx="26" ry="30" fill="#e8650e" stroke="{INK}" stroke-width="6"/><ellipse cx="0" cy="0" rx="28" ry="34" fill="#ff8a1f" stroke="{INK}" stroke-width="6"/>'
                f'<path d="M-16 -6 L-6 -16 L-4 -2 Z M16 -6 L6 -16 L4 -2 Z M-16 10 Q0 24 16 10 Q0 16 -16 10 Z" fill="#ffe066"/>'
                f'<rect x="-4" y="-44" width="8" height="14" rx="3" fill="#6b9b2f"/></g>')

    tree = (f'<path d="M640 300 C650 230 640 170 610 120 M628 200 C600 180 570 182 548 160 M636 160 C660 130 690 128 712 104 M616 140 C600 110 604 90 590 70" '
            f'fill="none" stroke="#2a1650" stroke-width="22" stroke-linecap="round"/>')
    fence = "".join(f'<path d="M{x} 360 L{x} 300 L{x + 8} 290 L{x + 16} 300 L{x + 16} 360 Z" fill="#2a1650"/>' for x in range(420, 560, 34)) + \
        '<rect x="410" y="316" width="150" height="10" fill="#2a1650"/>'
    grass = "".join(f'<path d="M{x} {y} l6 -22 l6 22 l6 -16 l6 16" fill="none" stroke="#2c1a52" stroke-width="5"/>' for x, y in [(40, 420), (230, 410), (700, 430), (900, 400), (1040, 420)])
    return (f'<defs>{fade}</defs><path d="{back}" fill="#24124a"/>{tree}<path d="{front}" fill="url(#hf)"/>'
            f'{tombs}{fence}{pumpkin(200, 380, 1.1)}{pumpkin(760, 392, 0.9)}{pumpkin(930, 410, 0.7)}{grass}')


UI = {
    # name: (svg, w, h, border)
    "rounded_rect": (rounded(128, 128, 44), 128, 128, 50),
    "rounded_rect_small": (rounded(96, 96, 22), 96, 96, 26),
    "pill": (rounded(160, 80, 40), 160, 80, 40),
    "circle": ('<circle cx="128" cy="128" r="126" fill="#fff"/>', 256, 256, 0),
    "soft_glow": (soft_glow(), 256, 256, 0),
    "card_frame": (card_frame(), 160, 160, 56),
    "card_inner": (card_inner(), 300, 408, 0),
    "card_back": (card_back(), 400, 544, 0),
    "shine": (shine(), 64, 256, 0),
    "rays": (rays(), 1024, 1024, 0),
    "gradient_vertical": (gradient_vertical(), 16, 256, 0),
    "vignette": (vignette(), 512, 512, 0),
    "scan_corner": (scan_corner(), 170, 170, 0),
    "banner": (banner(), 820, 210, 0),
    "badge": (badge(), 220, 96, 0),
    "icon_scan": (icon_scan(), 256, 256, 0),
    "icon_collection": (icon_collection(), 256, 256, 0),
    "icon_results": (icon_results(), 256, 256, 0),
    "icon_sound_on": (icon_sound_on(), 256, 256, 0),
    "icon_sound_off": (icon_sound_off(), 256, 256, 0),
    "icon_close": (icon_close(), 256, 256, 0),
    "icon_candy": (icon_candy(), 256, 256, 0),
    "icon_star": (icon_star(), 256, 256, 0),
    "icon_clock": (icon_clock(), 256, 256, 0),
    "icon_ghost": (icon_ghost(), 256, 256, 0),
    "icon_camera": (icon_camera(), 256, 256, 0),
    "icon_keyboard": (icon_keyboard(), 256, 256, 0),
    "icon_power": (icon_power(), 256, 256, 0),
    "icon_trophy": (icon_trophy(), 256, 256, 0),
    "logo": (logo(), 940, 340, 0),
    "background": (background(), 1080, 1920, 0),
    "moon": (moon(), 300, 300, 0),
    "hills": (hills(), 1080, 560, 0),
    "particles": (particle_atlas(), 512, 640, 0),
    "icon_edit": (icon_edit(), 256, 256, 0),
    "icon_dice": (icon_dice(), 256, 256, 0),
    "icon_info": (icon_info(), 256, 256, 0),
    "card_frame_metal": (card_frame_metal(), 160, 160, 56),
    "scratches": (scratches(), 300, 408, 0),
    "chip_metal": (chip_metal(), 160, 80, 40),
}
