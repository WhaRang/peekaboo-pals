"""Peekaboo Pals brand art (CC0, made for this project): the mascot "Peeky", the app logo, the app icon, the
generic "found a pal" and "albums" icons, the default party hills, and the Halloween 2026 pack logo."""
import random

from characters import INK, SW, blush, eye, highlight, o, shadow, sparkle
from ui import bat_path, defs_grad, star_path

MINT = "#5fe0b5"
MINT_DARK = "#2fb68b"
MINT_LIGHT = "#c4f7e3"
PINK = "#ff5fa8"
GOLD = "#ffd23f"
PURPLE = "#9b5cff"
MOUTH = 'stroke-width="8"'


def peeky(hat=True, hands=True, with_shadow=True):
    """The mascot in a 512x512 box: a minty gumdrop pal in a party hat, peeking over whatever is below it."""
    body = "M112 440 C92 304 140 154 256 154 C372 154 420 304 400 440 C372 470 140 470 112 440 Z"
    parts = [shadow(150) if with_shadow else ""]
    # little horns
    parts.append(f'<path d="M176 182 C160 150 166 118 190 104 C200 130 210 152 214 170 Z" {o(PURPLE)}/>')
    parts.append(f'<path d="M336 182 C352 150 346 118 322 104 C312 130 302 152 298 170 Z" {o(PURPLE)}/>')
    parts.append(f'<path d="{body}" {o(MINT)}/>')
    parts.append(f'<path d="M140 420 C150 330 190 300 256 300 C322 300 362 330 372 420 C340 444 172 444 140 420 Z" fill="{MINT_LIGHT}" opacity="0.65"/>')
    parts.append(highlight(190, 220, 28, 50, -30, 0.4))
    parts.append(eye(206, 286, 36, 44, look=(5, 4)))
    parts.append(eye(308, 286, 36, 44, look=(5, 4)))
    parts.append(f'<path d="M212 352 Q258 404 304 352 Q258 372 212 352 Z" {o(INK, MOUTH)}/>')
    parts.append(f'<path d="M232 360 L242 360 L237 372 Z" fill="#fff"/>')
    parts.append(f'<ellipse cx="258" cy="378" rx="16" ry="8" fill="{PINK}" opacity="0.9"/>')
    parts.append(blush(162, 352, 340, 24, 13))
    if hat:
        parts.append('<g transform="rotate(14 300 150)">'
                     f'<path d="M238 176 L300 26 L362 176 Q300 196 238 176 Z" {o(PINK)}/>'
                     f'<path d="M262 118 L338 118 L350 148 L250 148 Z" fill="{GOLD}" opacity="0.95"/>'
                     f'<path d="M282 70 L318 70 L326 92 L274 92 Z" fill="{GOLD}" opacity="0.95"/>'
                     f'<path d="M238 176 L300 26 L362 176 Q300 196 238 176 Z" fill="none" stroke="{INK}" stroke-width="{SW}" stroke-linejoin="round"/>'
                     f'<circle cx="300" cy="24" r="22" {o("#ffffff")}/>'
                     '</g>')
    if hands:
        for cx in (168, 344):
            parts.append(f'<ellipse cx="{cx}" cy="452" rx="44" ry="30" {o(MINT)}/>'
                         f'<path d="M{cx - 14} 440 L{cx - 14} 462 M{cx + 12} 440 L{cx + 12} 462" stroke="{INK}" stroke-width="6" stroke-linecap="round"/>')
    return "".join(parts)


def logo():
    """940x400: PEEKABOO / PALS with Peeky peeking over the end of the word."""
    g1 = defs_grad("pk1", [(0, "#ffe36b", 1), (0.45, "#ff9ad0", 1), (1, "#ff4f9a", 1)])
    g2 = defs_grad("pk2", [(0, "#d6fff0", 1), (1, "#3fd39f", 1)])

    def word(text, y, size, spacing, fill, stroke, shadow_stroke):
        common = f'x="470" y="{y}" text-anchor="middle" font-family="Luckiest" font-size="{size}" letter-spacing="{spacing}"'
        return (f'<text {common} fill="{INK}" stroke="{INK}" stroke-width="{shadow_stroke}" stroke-linejoin="round" transform="translate(5 10)">{text}</text>'
                f'<text {common} fill="{fill}" stroke="{INK}" stroke-width="{stroke}" stroke-linejoin="round" paint-order="stroke">{text}</text>')

    mascot = f'<g transform="translate(598 0) scale(0.37)">{peeky(hat=True, hands=False, with_shadow=False)}</g>'
    hands = "".join(
        f'<g transform="translate({x} 150) rotate({r})"><ellipse cx="0" cy="0" rx="22" ry="15" fill="{MINT}" stroke="{INK}" stroke-width="7"/>'
        f'<path d="M-7 -6 L-7 6 M7 -6 L7 6" stroke="{INK}" stroke-width="4" stroke-linecap="round"/></g>'
        for x, r in [(650, -10), (742, 10)])
    return (f'<defs>{g1}{g2}</defs>{mascot}'
            + word("PEEKABOO", 258, 150, 4, "url(#pk1)", 18, 36)
            + word("PALS", 378, 118, 16, "url(#pk2)", 14, 28)
            + hands
            + sparkle(70, 120, 22, "#fff3b0") + sparkle(120, 330, 14, "#fff") + sparkle(880, 300, 18, GOLD) + sparkle(890, 120, 12, "#fff"))


def app_icon():
    """1024x1024 launcher/home-screen icon."""
    bg = defs_grad("ib", [(0, "#7a3fd0", 1), (0.6, "#3a1a72", 1), (1, "#1a0b3a", 1)], radial=True)
    random.seed(3)
    confetti = "".join(
        f'<rect x="{random.uniform(40, 980):.0f}" y="{random.uniform(40, 980):.0f}" width="{random.uniform(14, 26):.0f}" height="{random.uniform(8, 14):.0f}" rx="4" '
        f'fill="{random.choice([PINK, GOLD, MINT, "#c9a2ff", "#ffffff"])}" opacity="{random.uniform(0.5, 0.9):.2f}" '
        f'transform="rotate({random.uniform(0, 180):.0f} 512 512)"/>' for _ in range(36))
    glow = defs_grad("ig", [(0, "#ffffff", 0.35), (1, "#ffffff", 0)], radial=True)
    return (f'<defs>{bg}{glow}</defs><rect width="1024" height="1024" fill="url(#ib)"/>{confetti}'
            f'<circle cx="512" cy="560" r="400" fill="url(#ig)"/>'
            f'<g transform="translate(112 132) scale(1.56)">{peeky()}</g>')


def icon_pal():
    return f'<g transform="translate(0 4) scale(0.5)">{peeky(hat=True, hands=False, with_shadow=False)}</g>'


def icon_albums():
    def book(x, y, w, h, rot, fill, star):
        return (f'<g transform="rotate({rot} {x + w / 2} {y + h})">'
                f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="12" fill="{fill}" stroke="{INK}" stroke-width="10"/>'
                f'<rect x="{x + 10}" y="{y + 14}" width="{w - 20}" height="10" rx="5" fill="#fff" opacity="0.6"/>'
                f'<rect x="{x + 10}" y="{y + h - 26}" width="{w - 20}" height="10" rx="5" fill="#fff" opacity="0.6"/>'
                f'<path d="{star_path(x + w / 2, y + h / 2, w * 0.22, w * 0.1)}" fill="{star}" stroke="{INK}" stroke-width="5" stroke-linejoin="round"/></g>')

    return (book(38, 70, 62, 150, -12, PINK, GOLD) + book(98, 46, 68, 174, 0, MINT, "#fff") + book(166, 64, 58, 156, 10, GOLD, PINK)
            + f'<rect x="20" y="214" width="216" height="22" rx="11" fill="{PURPLE}" stroke="{INK}" stroke-width="8"/>')


def hills():
    """1080x560: soft night hills with a string of party lights. The default scenery; packs bring their own."""
    fade = defs_grad("hf", [(0, "#1a0b33", 0), (0.25, "#1a0b33", 1), (1, "#0d051c", 1)])
    back = "M0 230 C180 160 330 190 470 222 C640 262 800 150 1080 196 L1080 560 L0 560 Z"
    front = "M0 334 C210 280 390 306 570 336 C770 370 910 296 1080 318 L1080 560 L0 560 Z"
    bushes = "".join(f'<ellipse cx="{x}" cy="{y}" rx="{r}" ry="{r * 0.72:.0f}" fill="#2c1758" stroke="{INK}" stroke-width="7"/>'
                     for x, y, r in [(120, 342, 44), (176, 352, 30), (860, 336, 40), (912, 346, 26), (520, 352, 22)])
    poles = "".join(f'<rect x="{x - 6}" y="{y}" width="12" height="{370 - y}" rx="5" fill="#2a1650"/>' for x, y in [(60, 120), (1020, 110)])
    wire = "M60 124 Q300 270 540 196 Q780 124 1020 114"
    random.seed(9)
    bulbs = []
    colors = [PINK, GOLD, MINT, "#c9a2ff", "#ff9a3c", "#7df9ff"]
    for i in range(1, 18):
        t = i / 18
        # sample the two quadratic halves of the wire
        if t < 0.5:
            u = t * 2
            x = (1 - u) ** 2 * 60 + 2 * (1 - u) * u * 300 + u ** 2 * 540
            y = (1 - u) ** 2 * 124 + 2 * (1 - u) * u * 270 + u ** 2 * 196
        else:
            u = t * 2 - 1
            x = (1 - u) ** 2 * 540 + 2 * (1 - u) * u * 780 + u ** 2 * 1020
            y = (1 - u) ** 2 * 196 + 2 * (1 - u) * u * 124 + u ** 2 * 114
        c = colors[i % len(colors)]
        bulbs.append(f'<circle cx="{x:.0f}" cy="{y + 14:.0f}" r="22" fill="{c}" opacity="0.25"/>'
                     f'<rect x="{x - 5:.0f}" y="{y - 2:.0f}" width="10" height="10" rx="2" fill="#2a1650"/>'
                     f'<ellipse cx="{x:.0f}" cy="{y + 16:.0f}" rx="9" ry="12" fill="{c}" stroke="{INK}" stroke-width="4"/>'
                     f'<circle cx="{x - 3:.0f}" cy="{y + 11:.0f}" r="3" fill="#fff" opacity="0.8"/>')
    grass = "".join(f'<path d="M{x} {y} l6 -22 l6 22 l6 -16 l6 16" fill="none" stroke="#2c1a52" stroke-width="5"/>'
                    for x, y in [(40, 420), (300, 430), (700, 430), (980, 410)])
    return (f'<defs>{fade}</defs><path d="{back}" fill="#24124a"/>{poles}'
            f'<path d="{wire}" fill="none" stroke="#2a1650" stroke-width="5"/>{"".join(bulbs)}'
            f'<path d="{front}" fill="url(#hf)"/>{bushes}{grass}')


def halloween_logo():
    """940x340 logo for the Halloween 2026 pack (the original look, new words)."""
    g1 = defs_grad("hl1", [(0, "#ffe066", 1), (0.5, "#ffa12b", 1), (1, "#ff6a13", 1)])
    g2 = defs_grad("hl2", [(0, "#d9ffb8", 1), (1, "#5fd630", 1)])
    return (f'<defs>{g1}{g2}</defs>'
            f'<path d="{bat_path(470, 46, 70)}" fill="{INK}"/>'
            f'<circle cx="455" cy="40" r="5" fill="#ffd23f"/><circle cx="485" cy="40" r="5" fill="#ffd23f"/>'
            f'<text x="470" y="212" text-anchor="middle" font-family="Luckiest" font-size="150" letter-spacing="4" fill="{INK}" stroke="{INK}" stroke-width="40" stroke-linejoin="round" transform="translate(6 12)">HALLOWEEN</text>'
            f'<text x="470" y="212" text-anchor="middle" font-family="Luckiest" font-size="150" letter-spacing="4" fill="url(#hl1)" stroke="{INK}" stroke-width="20" stroke-linejoin="round" paint-order="stroke">HALLOWEEN</text>'
            f'<text x="470" y="318" text-anchor="middle" font-family="Luckiest" font-size="104" letter-spacing="14" fill="{INK}" stroke="{INK}" stroke-width="30" stroke-linejoin="round" transform="translate(4 8)">2026</text>'
            f'<text x="470" y="318" text-anchor="middle" font-family="Luckiest" font-size="104" letter-spacing="14" fill="url(#hl2)" stroke="{INK}" stroke-width="16" stroke-linejoin="round" paint-order="stroke">2026</text>'
            + sparkle(90, 90, 22, "#fff3b0") + sparkle(860, 120, 18, "#fff") + sparkle(830, 270, 12, "#ffd23f"))
