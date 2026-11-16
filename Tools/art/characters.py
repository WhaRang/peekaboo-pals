"""Original Spooky Collector character art (CC0, made for this project). Each function returns SVG body markup
for a 512x512 viewBox. Shared style: thick ink outline, flat fills, one soft highlight, big shiny eyes, blush."""

import re

INK = "#2a1240"
SW = 12  # outline width
BLUSH = "#ff5fa8"


def o(fill, extra=""):
    return f'fill="{fill}" stroke="{INK}" stroke-width="{SW}" stroke-linejoin="round" stroke-linecap="round" {extra}'


def shadow(rx=150, y=470):
    return f'<ellipse cx="256" cy="{y}" rx="{rx}" ry="20" fill="#000" opacity="0.28"/>'


def eye(cx, cy, r, ry=None, pupil=INK, look=(4, 4), white="#ffffff"):
    ry = ry or r
    return (f'<ellipse cx="{cx}" cy="{cy}" rx="{r}" ry="{ry}" fill="{white}" stroke="{INK}" stroke-width="8"/>'
            f'<ellipse cx="{cx + look[0]}" cy="{cy + look[1]}" rx="{r * 0.62:.1f}" ry="{ry * 0.66:.1f}" fill="{pupil}"/>'
            f'<circle cx="{cx + look[0] - r * 0.22:.1f}" cy="{cy + look[1] - ry * 0.28:.1f}" r="{r * 0.24:.1f}" fill="#fff"/>'
            f'<circle cx="{cx + look[0] + r * 0.2:.1f}" cy="{cy + look[1] + ry * 0.2:.1f}" r="{r * 0.1:.1f}" fill="#fff"/>')


def blush(x1, x2, y, rx=22, ry=12):
    return (f'<ellipse cx="{x1}" cy="{y}" rx="{rx}" ry="{ry}" fill="{BLUSH}" opacity="0.55"/>'
            f'<ellipse cx="{x2}" cy="{y}" rx="{rx}" ry="{ry}" fill="{BLUSH}" opacity="0.55"/>')


def highlight(cx, cy, rx, ry, rot=-25, opacity=0.35):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#fff" opacity="{opacity}" transform="rotate({rot} {cx} {cy})"/>'


def sparkle(cx, cy, s, fill="#fff"):
    return (f'<path d="M{cx} {cy - s} Q{cx + s * 0.18} {cy - s * 0.18} {cx + s} {cy} Q{cx + s * 0.18} {cy + s * 0.18} {cx} {cy + s} '
            f'Q{cx - s * 0.18} {cy + s * 0.18} {cx - s} {cy} Q{cx - s * 0.18} {cy - s * 0.18} {cx} {cy - s} Z" fill="{fill}"/>')


def mirror(path_d_left):
    """Mirror an absolute path made of 'M x y', 'L x y', 'C ...', 'Q ...' tokens around x=256."""
    out, tokens = [], re.findall(r"[A-Za-z]|-?\d*\.?\d+", path_d_left)
    expect_x = True
    for t in tokens:
        if t.isalpha():
            out.append(t)
            expect_x = True
            if t in "Zz":
                continue
            continue
        v = float(t)
        if expect_x:
            out.append(f"{512 - v:g}")
        else:
            out.append(f"{v:g}")
        expect_x = not expect_x
    return " ".join(out)


def pip():
    return f'''{shadow(150)}
<path d="M244 170 C238 134 246 104 270 86 L292 104 C276 116 270 140 274 172 Z" {o("#6b9b2f")}/>
<path d="M282 124 C306 92 358 90 384 116 C356 134 320 142 284 136 Z" {o("#8fd14f")}/>
<path d="M290 128 C318 120 340 118 360 118" fill="none" stroke="{INK}" stroke-width="6" stroke-linecap="round"/>
<path d="M268 112 C230 92 210 124 230 134" fill="none" stroke="#6b9b2f" stroke-width="10" stroke-linecap="round"/>
<ellipse cx="166" cy="312" rx="104" ry="138" {o("#e8650e")}/>
<ellipse cx="346" cy="312" rx="104" ry="138" {o("#e8650e")}/>
<ellipse cx="256" cy="306" rx="116" ry="150" {o("#ff8a1f")}/>
{highlight(214, 220, 26, 52)}
{highlight(118, 270, 12, 34, -10, 0.25)}
{eye(212, 292, 30, 38)}
{eye(300, 292, 30, 38)}
<path d="M198 350 Q256 414 314 350 Q256 376 198 350 Z" {o(INK, 'stroke-width="8"')}/>
<rect x="244" y="362" width="22" height="18" rx="5" fill="#fff"/>
{blush(172, 340, 344)}
'''


def skelly():
    bone = "#f6f1e6"
    joint = o(bone, 'stroke-width="8"')

    def limb(x1, y1, x2, y2):
        return (f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{INK}" stroke-width="40" stroke-linecap="round"/>'
                f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{bone}" stroke-width="20" stroke-linecap="round"/>'
                f'<circle cx="{x2}" cy="{y2}" r="17" {joint}/>')
    return f'''{shadow(120)}
{limb(226, 372, 196, 452)}{limb(286, 372, 316, 452)}
{limb(214, 344, 140, 286)}{limb(298, 344, 368, 372)}
<rect x="200" y="320" width="112" height="86" rx="38" {o(bone)}/>
<path d="M222 352 Q256 342 290 352 M224 376 Q256 366 288 376" fill="none" stroke="{INK}" stroke-width="7" stroke-linecap="round"/>
<path d="M140 200 C140 120 196 74 256 74 C316 74 372 120 372 200 C372 250 352 276 332 290 L332 324 C332 340 320 348 304 348 L208 348 C192 348 180 340 180 324 L180 290 C160 276 140 250 140 200 Z" {o(bone)}/>
<path d="M340 150 C356 190 352 250 326 284 L326 300 C346 280 362 250 360 200 C358 180 352 164 340 150 Z" fill="#d9d0bf"/>
{highlight(196, 130, 30, 18, -30, 0.6)}
<ellipse cx="206" cy="212" rx="42" ry="48" fill="#3a1e5c"/>
<ellipse cx="306" cy="212" rx="42" ry="48" fill="#3a1e5c"/>
<circle cx="216" cy="204" r="15" fill="#fff"/><circle cx="198" cy="226" r="6" fill="#fff"/>
<circle cx="316" cy="204" r="15" fill="#fff"/><circle cx="298" cy="226" r="6" fill="#fff"/>
<path d="M256 252 L242 276 Q256 284 270 276 Z" fill="{INK}"/>
<path d="M210 308 L302 308" stroke="{INK}" stroke-width="8" stroke-linecap="round"/>
<path d="M228 298 L228 318 M246 298 L246 320 M266 298 L266 320 M284 298 L284 318" stroke="{INK}" stroke-width="6" stroke-linecap="round"/>
<path d="M220 334 L256 350 L220 366 Z" {o("#ff8a1f", 'stroke-width="7"')}/>
<path d="M292 334 L256 350 L292 366 Z" {o("#ff8a1f", 'stroke-width="7"')}/>
<circle cx="256" cy="350" r="11" {o("#ffb547", 'stroke-width="6"')}/>
{blush(166, 346, 262, 18, 10)}
'''


def batsy():
    wing = "M200 250 C150 186 82 176 38 212 C70 224 78 250 70 274 C98 260 120 272 124 298 C144 278 170 282 190 302 Z"
    ear = "M180 214 L166 112 L236 182 Z"
    ear_in = "M188 196 L182 140 L220 180 Z"
    return f'''{shadow(120)}
<path d="{wing}" {o("#3d2066")}/>
<path d="{mirror(wing)}" {o("#3d2066")}/>
<path d="M76 222 C110 214 150 226 176 262 M84 262 C112 252 140 262 156 284" fill="none" stroke="#5b3592" stroke-width="7" stroke-linecap="round"/>
<path d="{mirror("M76 222 C110 214 150 226 176 262 M84 262 C112 252 140 262 156 284")}" fill="none" stroke="#5b3592" stroke-width="7" stroke-linecap="round"/>
<path d="{ear}" {o("#7a49b8")}/><path d="{mirror(ear)}" {o("#7a49b8")}/>
<path d="{ear_in}" fill="#ff9ec8"/><path d="{mirror(ear_in)}" fill="#ff9ec8"/>
<ellipse cx="226" cy="430" rx="22" ry="16" {o("#5b3592", 'stroke-width="8"')}/>
<ellipse cx="286" cy="430" rx="22" ry="16" {o("#5b3592", 'stroke-width="8"')}/>
<circle cx="256" cy="290" r="116" {o("#7a49b8")}/>
<path d="M232 178 L244 152 L256 176 L268 150 L280 178" {o("#7a49b8", 'stroke-width="8"')}/>
<ellipse cx="256" cy="344" rx="62" ry="48" fill="#a57fe0" opacity="0.8"/>
{highlight(200, 216, 30, 18, -30, 0.35)}
{eye(214, 270, 34)}
{eye(298, 270, 34)}
<path d="M232 326 Q256 344 280 326" fill="none" stroke="{INK}" stroke-width="8" stroke-linecap="round"/>
<path d="M240 330 L246 350 L252 334 Z M260 334 L266 350 L272 330 Z" fill="#fff" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>
{blush(178, 334, 320)}
'''


def sheety():
    body = "M136 250 C136 148 190 82 256 82 C322 82 376 148 376 250 L376 404 C356 440 336 398 316 418 C296 446 276 402 256 424 C236 446 216 402 196 418 C176 440 156 398 136 404 Z"
    arm = "M140 280 C106 284 88 312 96 336 C116 330 128 322 142 312"
    return f'''{shadow(120, 474)}
<defs><clipPath id="sheetClip"><path d="{body}"/></clipPath></defs>
<path d="{arm}" {o("#f4f0ff")}/>
<path d="{mirror(arm)}" {o("#f4f0ff")}/>
<path d="{body}" {o("#f7f4ff")}/>
<g clip-path="url(#sheetClip)">
<ellipse cx="380" cy="300" rx="70" ry="220" fill="#d9cff8" opacity="0.8"/>
<path d="M150 380 C190 360 230 400 256 380 C290 360 330 400 370 380 L380 440 L130 440 Z" fill="#e6defc"/>
</g>
{highlight(200, 140, 34, 20, -30, 0.9)}
<ellipse cx="212" cy="234" rx="26" ry="36" fill="{INK}"/>
<ellipse cx="300" cy="234" rx="26" ry="36" fill="{INK}"/>
<circle cx="204" cy="222" r="10" fill="#fff"/><circle cx="292" cy="222" r="10" fill="#fff"/>
<circle cx="218" cy="248" r="4" fill="#fff"/><circle cx="306" cy="248" r="4" fill="#fff"/>
<ellipse cx="256" cy="302" rx="22" ry="28" fill="{INK}"/>
<ellipse cx="256" cy="316" rx="12" ry="9" fill="#ff7fb5"/>
{blush(176, 336, 282)}
'''


def wanda():
    hair_back = "M150 260 C126 322 150 392 116 436 C168 428 190 384 198 340 L314 340 C322 384 344 428 396 436 C362 392 386 322 362 260 Z"
    bangs = "M164 266 C186 218 230 210 256 234 C282 210 326 218 348 266 C322 254 300 256 282 268 C270 252 242 252 230 268 C212 256 190 254 164 266 Z"
    return f'''{shadow(130)}
<path d="{hair_back}" {o("#5b2a9e")}/>
<path d="M196 368 C170 400 150 440 140 466 L372 466 C362 440 342 400 316 368 Z" {o("#3b1d63")}/>
<path d="M214 372 L256 410 L298 372 Z" {o("#ff8a1f", 'stroke-width="8"')}/>
<circle cx="256" cy="408" r="10" {o("#ffd23f", 'stroke-width="6"')}/>
<circle cx="256" cy="292" r="98" {o("#b9e58a")}/>
<path d="M330 236 C352 270 354 320 330 356 C344 320 344 276 330 236 Z" fill="#9dcf6c"/>
<path d="{bangs}" {o("#5b2a9e", 'stroke-width="8"')}/>
<ellipse cx="256" cy="208" rx="170" ry="34" {o("#3b1d63")}/>
<path d="M170 204 C186 152 206 104 226 70 C242 42 284 26 326 42 C300 52 290 72 288 98 C302 134 322 170 342 204 Z" {o("#4a2580")}/>
<path d="M180 180 L334 180 L342 204 L170 204 Z" {o("#ff8a1f", 'stroke-width="8"')}/>
<rect x="238" y="176" width="36" height="32" rx="6" fill="none" stroke="#ffd23f" stroke-width="8"/>
{sparkle(262, 120, 18, "#ffd23f")}
{highlight(208, 120, 10, 30, 20, 0.25)}
{eye(222, 296, 26)}
{eye(290, 296, 26)}
<path d="M196 274 L206 266 M316 274 L306 266" stroke="{INK}" stroke-width="6" stroke-linecap="round"/>
<path d="M226 338 Q256 362 286 338" fill="none" stroke="{INK}" stroke-width="8" stroke-linecap="round"/>
<ellipse cx="256" cy="318" rx="8" ry="6" fill="#8fc45c"/>
<circle cx="206" cy="330" r="3.5" fill="#6e9f3d"/><circle cx="196" cy="322" r="3" fill="#6e9f3d"/><circle cx="214" cy="322" r="3" fill="#6e9f3d"/>
<circle cx="306" cy="330" r="3.5" fill="#6e9f3d"/><circle cx="316" cy="322" r="3" fill="#6e9f3d"/><circle cx="298" cy="322" r="3" fill="#6e9f3d"/>
{blush(196, 316, 340, 18, 10)}
'''


def mumbles():
    body = "M156 200 C156 118 204 82 256 82 C308 82 356 118 356 200 L356 398 C356 430 332 450 302 450 L210 450 C180 450 156 430 156 398 Z"
    arm = "M162 262 L88 280 C72 284 72 310 90 312 L162 302 Z"
    strips = "".join(
        f'<path d="M140 {y} L372 {y + s}" stroke="#cdb994" stroke-width="5"/><path d="M140 {y + 3} L372 {y + s + 3}" stroke="{INK}" stroke-width="3" opacity="0.25"/>'
        for y, s in [(120, 18), (156, -14), (290, 16), (326, -12), (362, 14), (398, -16), (430, 10)])
    return f'''{shadow(130)}
<defs><clipPath id="mumClip"><path d="{body}"/></clipPath></defs>
<path d="{arm}" {o("#ecdfc2")}/>
<path d="{mirror(arm)}" {o("#ecdfc2")}/>
<path d="M92 300 C80 330 96 350 84 380" fill="none" stroke="{INK}" stroke-width="18" stroke-linecap="round"/>
<path d="M92 300 C80 330 96 350 84 380" fill="none" stroke="#ecdfc2" stroke-width="8" stroke-linecap="round"/>
<path d="{body}" {o("#ecdfc2")}/>
<g clip-path="url(#mumClip)">
{strips}
<path d="M150 208 C206 188 306 188 362 208 L362 262 C306 244 206 244 150 262 Z" fill="#3b2850"/>
<ellipse cx="342" cy="300" rx="40" ry="190" fill="#d6c6a2" opacity="0.6"/>
</g>
<path d="M150 208 C206 188 306 188 362 208 M150 262 C206 244 306 244 362 262" fill="none" stroke="{INK}" stroke-width="8"/>
{eye(220, 228, 30, 30, INK, (3, 2))}
{eye(298, 232, 20, 16, INK, (2, 1))}
{highlight(198, 140, 30, 16, -25, 0.5)}
<path d="M234 296 Q256 312 278 296" fill="none" stroke="{INK}" stroke-width="8" stroke-linecap="round"/>
{blush(188, 324, 292, 18, 10)}
'''


def howler():
    head = "M130 262 C130 170 190 122 256 122 C322 122 382 170 382 262 C382 302 370 330 350 350 L380 362 L342 374 C322 394 292 406 256 406 C220 406 190 394 170 374 L132 362 L162 350 C142 330 130 302 130 262 Z"
    ear = "M168 206 L150 70 L242 160 Z"
    ear_in = "M174 186 L164 106 L222 162 Z"
    brow = "M180 204 C196 192 222 192 236 202"
    return f'''{shadow(140)}
<defs><mask id="moonMask"><rect width="512" height="512" fill="#fff"/><circle cx="410" cy="100" r="64" fill="#000"/></mask></defs>
<circle cx="380" cy="120" r="74" fill="#ffe9a8" stroke="{INK}" stroke-width="8" mask="url(#moonMask)"/>
<ellipse cx="256" cy="430" rx="112" ry="46" {o("#6e5242")}/>
<path d="{ear}" {o("#8b6a56")}/><path d="{mirror(ear)}" {o("#8b6a56")}/>
<path d="{ear_in}" fill="#e9a3b5"/><path d="{mirror(ear_in)}" fill="#e9a3b5"/>
<path d="{head}" {o("#8b6a56")}/>
<path d="M236 130 L250 104 L258 128 L272 100 L280 132" {o("#8b6a56", 'stroke-width="8"')}/>
<path d="M340 190 C364 230 366 290 344 330 C356 290 356 236 340 190 Z" fill="#735645"/>
<path d="M190 330 C190 280 214 262 256 262 C298 262 322 280 322 330 C322 366 296 384 256 384 C216 384 190 366 190 330 Z" {o("#e2c9ab", 'stroke-width="8"')}/>
<path d="M232 284 Q256 270 280 284 Q272 304 256 308 Q240 304 232 284 Z" fill="{INK}"/>
<ellipse cx="248" cy="284" rx="8" ry="4" fill="#fff" opacity="0.7"/>
<path d="M256 308 L256 324 M222 332 Q240 350 256 330 Q272 350 290 332" fill="none" stroke="{INK}" stroke-width="7" stroke-linecap="round"/>
<path d="M232 336 L238 354 L246 340 Z M266 340 L274 354 L280 336 Z" fill="#fff" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>
{eye(206, 238, 28, 28, INK, (3, 3), "#ffd23f")}
{eye(306, 238, 28, 28, INK, (-3, 3), "#ffd23f")}
<path d="{brow}" fill="none" stroke="{INK}" stroke-width="12" stroke-linecap="round"/>
<path d="{mirror(brow)}" fill="none" stroke="{INK}" stroke-width="12" stroke-linecap="round"/>
{highlight(200, 160, 26, 14, -25, 0.3)}
{blush(176, 336, 300, 16, 9)}
'''


def bat(cx, cy, s, fill=INK):
    """Tiny flying bat silhouette, wingspan ~4s."""
    return (f'<path d="M{cx} {cy - s * 0.3} C{cx - s * 0.6} {cy - s * 1.1} {cx - s * 1.5} {cy - s * 0.9} {cx - s * 2} {cy - s * 0.4} '
            f'C{cx - s * 1.5} {cy - s * 0.3} {cx - s * 1.3} {cy + s * 0.1} {cx - s} {cy + s * 0.3} C{cx - s * 0.8} {cy} {cx - s * 0.4} {cy} {cx} {cy + s * 0.4} '
            f'C{cx + s * 0.4} {cy} {cx + s * 0.8} {cy} {cx + s} {cy + s * 0.3} C{cx + s * 1.3} {cy + s * 0.1} {cx + s * 1.5} {cy - s * 0.3} {cx + s * 2} {cy - s * 0.4} '
            f'C{cx + s * 1.5} {cy - s * 0.9} {cx + s * 0.6} {cy - s * 1.1} {cx} {cy - s * 0.3} Z" fill="{fill}"/>')


def fangsworth():
    # Lord of the midnight ball: bat-wing cape spread wide, gold trim, ruby medallion, a goblet of tomato juice.
    wing = ("M256 262 C206 254 140 236 70 170 C58 214 50 260 48 300 C64 290 82 292 92 306 C100 290 120 290 130 306 "
            "C138 292 156 294 162 310 L176 446 L256 446 Z")
    lining = ("M256 280 C210 274 150 258 92 212 C86 244 82 270 82 286 C94 282 104 288 110 296 C118 284 132 286 140 298 "
              "C148 288 160 292 166 306 L184 446 L256 446 Z")
    collar = "M176 300 L112 150 L150 170 L222 254 Z"
    fang = "M162 228 L140 196 L170 208 Z"
    return f'''{shadow(175)}
<circle cx="384" cy="120" r="74" fill="#fff3c4" opacity="0.28"/>
<circle cx="384" cy="120" r="52" fill="#fff3c4" opacity="0.35"/>
{bat(396, 104, 16)}{bat(110, 92, 12)}{bat(446, 196, 9)}
<path d="{wing}" {o("#1d0f33")}/>
<path d="{mirror(wing)}" {o("#1d0f33")}/>
<path d="{lining}" fill="#b81d33"/>
<path d="{mirror(lining)}" fill="#b81d33"/>
<path d="M70 170 C140 236 206 254 256 262 C306 254 372 236 442 170" fill="none" stroke="#ffd23f" stroke-width="7" stroke-linecap="round"/>
<path d="M200 300 L312 300 L328 446 L184 446 Z" {o("#2b1a45", 'stroke-width="8"')}/>
<path d="M232 300 L256 360 L280 300 Z" fill="#f7f3ff" stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>
<path d="M214 300 L208 446 M298 300 L304 446" stroke="#ffd23f" stroke-width="5" stroke-linecap="round"/>
<path d="{collar}" {o("#d7263d")}/>
<path d="{mirror(collar)}" {o("#d7263d")}/>
<path d="M116 158 L176 296 M396 158 L336 296" stroke="#ffd23f" stroke-width="6" stroke-linecap="round"/>
<path d="{fang}" {o("#ece0f8", 'stroke-width="7"')}/>
<path d="{mirror(fang)}" {o("#ece0f8", 'stroke-width="7"')}/>
<ellipse cx="256" cy="236" rx="96" ry="110" {o("#ece0f8")}/>
<path d="M326 170 C350 210 352 270 324 312 C340 270 340 214 326 170 Z" fill="#d6c4ee"/>
<path d="M160 214 C158 142 206 112 256 112 C306 112 354 142 352 214 C332 182 302 168 272 170 L256 206 L240 170 C210 168 180 182 160 214 Z" {o("#1d0f33", 'stroke-width="8"')}/>
<path d="M200 140 C222 128 246 126 262 128" fill="none" stroke="#5a4b8a" stroke-width="8" stroke-linecap="round"/>
{eye(218, 244, 24, 24, "#b0122c")}
{eye(294, 244, 24, 24, "#b0122c")}
<path d="M188 212 L240 228 M324 212 L272 228" stroke="{INK}" stroke-width="10" stroke-linecap="round"/>
<circle cx="294" cy="244" r="36" fill="#ffffff" fill-opacity="0.15" stroke="#ffd23f" stroke-width="8"/>
<path d="M326 262 C340 290 336 310 318 330" fill="none" stroke="#ffd23f" stroke-width="5" stroke-dasharray="6 6"/>
<path d="M218 292 Q256 322 294 292" fill="none" stroke="{INK}" stroke-width="8" stroke-linecap="round"/>
<path d="M234 300 L241 326 L250 305 Z M262 305 L271 326 L278 300 Z" fill="#fff" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>
<path d="M214 352 Q256 378 298 352" fill="none" stroke="#ffd23f" stroke-width="6" stroke-linecap="round"/>
<path d="M256 362 L282 388 L256 420 L230 388 Z" {o("#ffd23f", 'stroke-width="7"')}/>
<path d="M256 374 L270 388 L256 406 L242 388 Z" fill="#d7263d"/>
{highlight(250, 384, 3, 8, 20, 0.7)}
<path d="M376 300 C368 300 362 322 380 330 L396 330 C414 322 408 300 400 300 Z" {o("#ffd23f", 'stroke-width="7"')}/>
<path d="M370 304 L406 304" stroke="#b0122c" stroke-width="9" stroke-linecap="round"/>
<path d="M388 330 L388 356 M374 360 L402 360" stroke="{INK}" stroke-width="16" stroke-linecap="round"/>
<path d="M388 330 L388 356 M376 360 L400 360" stroke="#ffd23f" stroke-width="7" stroke-linecap="round"/>
<ellipse cx="388" cy="344" rx="19" ry="14" {o("#ece0f8", 'stroke-width="7"')}/>
{blush(196, 316, 282, 18, 10)}
{sparkle(64, 340, 14, "#ffd23f")}{sparkle(450, 300, 10, "#fff3b0")}{sparkle(300, 70, 9, "#fff")}
'''


def gourd_king():
    spots = "".join(f'<path d="M{x} {y} q4 8 0 14 q-4 -6 0 -14 z" fill="{INK}"/>' for x, y in
                    [(186, 428), (220, 436), (256, 438), (292, 436), (326, 428)])
    return f'''{shadow(180)}
<path d="M66 452 C86 330 128 258 200 238 L312 238 C384 258 426 330 446 452 Z" {o("#c81d3a")}/>
<path d="M100 452 C116 360 146 300 196 278" fill="none" stroke="#9e1530" stroke-width="10" stroke-linecap="round"/>
<line x1="388" y1="446" x2="444" y2="232" stroke="{INK}" stroke-width="26" stroke-linecap="round"/>
<line x1="388" y1="446" x2="444" y2="232" stroke="#ffd23f" stroke-width="12" stroke-linecap="round"/>
<path d="M444 186 L456 216 L488 218 L462 236 L472 266 L444 248 L418 266 L428 236 L402 218 L434 216 Z" {o("#ffd23f", 'stroke-width="7"')}/>
<ellipse cx="176" cy="318" rx="96" ry="118" {o("#f09a16")}/>
<ellipse cx="336" cy="318" rx="96" ry="118" {o("#f09a16")}/>
<ellipse cx="256" cy="312" rx="108" ry="130" {o("#ffb52e")}/>
{highlight(214, 236, 24, 46, -20, 0.4)}

<path d="M168 176 L180 92 L222 136 L256 72 L290 136 L332 92 L344 176 Z" {o("#ffd23f")}/>
<rect x="166" y="160" width="180" height="34" rx="10" {o("#f0b81f", 'stroke-width="8"')}/>
<circle cx="180" cy="90" r="11" {o("#fff3b0", 'stroke-width="6"')}/><circle cx="256" cy="70" r="12" {o("#fff3b0", 'stroke-width="6"')}/><circle cx="332" cy="90" r="11" {o("#fff3b0", 'stroke-width="6"')}/>
<circle cx="210" cy="177" r="9" fill="#d7263d"/><circle cx="256" cy="177" r="11" fill="#3fa9f5"/><circle cx="302" cy="177" r="9" fill="#7cff4f"/>
{highlight(206, 120, 8, 26, 15, 0.5)}
<ellipse cx="216" cy="296" rx="28" ry="30" fill="#fff" stroke="{INK}" stroke-width="8"/>
<ellipse cx="296" cy="296" rx="28" ry="30" fill="#fff" stroke="{INK}" stroke-width="8"/>
<circle cx="220" cy="302" r="16" fill="{INK}"/><circle cx="300" cy="302" r="16" fill="{INK}"/>
<circle cx="214" cy="296" r="5" fill="#fff"/><circle cx="294" cy="296" r="5" fill="#fff"/>
<path d="M186 284 Q216 268 246 282 M266 282 Q296 268 326 284" fill="none" stroke="{INK}" stroke-width="8" stroke-linecap="round"/>
<path d="M256 340 C236 328 206 328 194 348 C188 360 200 372 212 364 C206 354 218 346 230 350 C242 354 250 350 256 344 C262 350 270 354 282 350 C294 346 306 354 300 364 C312 372 324 360 318 348 C306 328 276 328 256 340 Z" fill="#7a3b12" stroke="{INK}" stroke-width="6" stroke-linejoin="round"/>
<path d="M232 378 Q256 396 280 378" fill="none" stroke="{INK}" stroke-width="7" stroke-linecap="round"/>
<path d="M150 408 C200 436 312 436 362 408 C368 430 360 452 340 458 L172 458 C152 452 144 430 150 408 Z" {o("#fff7ea", 'stroke-width="8"')}/>
{spots}
{blush(184, 328, 344, 18, 10)}
{sparkle(96, 150, 18, "#fff3b0")}{sparkle(420, 120, 14, "#fff")}{sparkle(70, 300, 10, "#ffd23f")}
'''


def jenny():
    # Legendary easter egg: a cheerful teenage robot girl on trick-or-treat duty. Original drawing; the nods are the
    # white-and-blue robot body, the navy "hair" helmet with lightning-bolt pigtails, the flared skirt and the jet boots.
    WHITE, PANEL, BLUE, NAVY, TEAL = "#f6f8ff", "#cfe3ff", "#3d7bff", "#22306e", "#5ff6ff"
    # Her signature pigtails, drawn as zigzag lightning bolts sticking up and out.
    bolt = "M60 0 L8 92 L46 92 L30 168 L94 62 L56 62 L78 0 Z"
    bolt_style = o(NAVY, 'stroke-width="10"')
    pigtail = (f'<g transform="translate(188 150) rotate(124) scale(0.78) translate(-69 -6)"><path d="{bolt}" {bolt_style}/>'
               '<path d="M60 14 L30 70" stroke="#5a6bb0" stroke-width="7" stroke-linecap="round"/></g>')
    flame = "M{x} 446 C{l} 470 {l2} 492 {x} 512 C{r2} 492 {r} 470 {x} 446 Z"
    # Cartoon eyes (not doll-like): tall whites, big two-tone blue irises, a heavy upper lid with a flick.
    ex, ey, icx, icy = 222, 204, 224, 209
    toon_eye = (f'<ellipse cx="{ex}" cy="{ey}" rx="21" ry="28" fill="#fff" stroke="{INK}" stroke-width="4"/>'
                f'<ellipse cx="{icx}" cy="{icy}" rx="15" ry="20" fill="#2f86ff"/>'
                f'<path d="M{icx - 15} {icy} A15 20 0 0 1 {icx + 15} {icy} Q{icx} {icy - 7} {icx - 15} {icy} Z" fill="#1a47b8" opacity="0.75"/>'
                f'<ellipse cx="{icx}" cy="{icy + 2}" rx="7" ry="10" fill="{INK}"/>'
                f'<circle cx="{icx - 6}" cy="{icy - 8}" r="5.5" fill="#fff"/><circle cx="{icx + 6}" cy="{icy + 9}" r="2.5" fill="#fff"/>'
                f'<path d="M{ex - 24} {ey - 2} Q{ex - 6} {ey - 40} {ex + 21} {ey - 19}" fill="none" stroke="{INK}" stroke-width="9" stroke-linecap="round"/>'
                f'<path d="M{ex - 23} {ey - 4} L{ex - 34} {ey - 10}" stroke="{INK}" stroke-width="7" stroke-linecap="round"/>')
    return f'''<ellipse cx="256" cy="498" rx="96" ry="12" fill="#000" opacity="0.2"/>
<path d="{flame.format(x=222, l=200, l2=208, r=244, r2=236)}" fill="{TEAL}" opacity="0.9"/>
<path d="{flame.format(x=290, l=268, l2=276, r=312, r2=304)}" fill="{TEAL}" opacity="0.9"/>
<path d="{flame.format(x=222, l=212, l2=216, r=232, r2=228).replace(" 512", " 488")}" fill="#fff"/>
<path d="{flame.format(x=290, l=280, l2=284, r=300, r2=296).replace(" 512", " 488")}" fill="#fff"/>
{pigtail}
<g transform="translate(512 0) scale(-1 1)">{pigtail}</g>
<path d="M214 392 L206 432 M298 392 L306 432" stroke="{INK}" stroke-width="30" stroke-linecap="round"/>
<path d="M214 392 L206 432 M298 392 L306 432" stroke="{WHITE}" stroke-width="16" stroke-linecap="round"/>
<circle cx="210" cy="410" r="11" {o(BLUE, 'stroke-width="6"')}/><circle cx="302" cy="410" r="11" {o(BLUE, 'stroke-width="6"')}/>
<path d="M188 430 C188 420 228 420 228 430 L232 452 C220 458 196 458 184 452 Z" {o(BLUE, 'stroke-width="8"')}/>
<path d="M284 430 C284 420 324 420 324 430 L328 452 C316 458 292 458 280 452 Z" {o(BLUE, 'stroke-width="8"')}/>
<path d="M222 334 L290 334 L338 398 C300 412 212 412 174 398 Z" {o(WHITE)}/>
<path d="M186 384 C222 396 290 396 326 384 L338 398 C300 412 212 412 174 398 Z" fill="{BLUE}" stroke="{INK}" stroke-width="8" stroke-linejoin="round"/>
<path d="M206 352 L196 372 M306 352 L316 372" stroke="{PANEL}" stroke-width="7" stroke-linecap="round"/>
<path d="M208 288 C208 278 304 278 304 288 L294 340 C270 346 242 346 218 340 Z" {o(WHITE)}/>
<circle cx="256" cy="312" r="24" fill="{TEAL}" opacity="0.35"/>
<circle cx="256" cy="312" r="18" fill="#ffb52e" stroke="{BLUE}" stroke-width="5"/>
<path d="M247 305 L252 310 L245 312 Z M265 305 L260 310 L267 312 Z" fill="{INK}"/>
<path d="M246 317 Q256 324 266 317" fill="none" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>
<path d="M256 293 L256 299" stroke="#6b9b2f" stroke-width="4" stroke-linecap="round"/>
<path d="M212 298 L172 304 L146 268" fill="none" stroke="{INK}" stroke-width="30" stroke-linecap="round" stroke-linejoin="round"/>
<path d="M212 298 L172 304 L146 268" fill="none" stroke="{WHITE}" stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/>
<circle cx="172" cy="304" r="11" {o(BLUE, 'stroke-width="6"')}/>
<path d="M132 250 L120 228 M144 246 L142 222" stroke="{INK}" stroke-width="14" stroke-linecap="round"/>
<path d="M132 250 L120 228 M144 246 L142 222" stroke="{BLUE}" stroke-width="6" stroke-linecap="round"/>
<circle cx="142" cy="262" r="20" {o(BLUE, 'stroke-width="8"')}/>
<path d="M104 262 C98 254 98 244 104 238 M90 270 C80 256 80 238 90 226" fill="none" stroke="{TEAL}" stroke-width="5" stroke-linecap="round"/>
<path d="M300 296 L334 330 L350 350" fill="none" stroke="{INK}" stroke-width="30" stroke-linecap="round" stroke-linejoin="round"/>
<path d="M300 296 L334 330 L350 350" fill="none" stroke="{WHITE}" stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/>
<circle cx="334" cy="330" r="11" {o(BLUE, 'stroke-width="6"')}/>
<path d="M330 372 C330 354 344 352 356 352 C368 352 382 354 382 372" fill="none" stroke="{INK}" stroke-width="6"/>
<path d="M322 378 C320 404 334 420 356 420 C378 420 392 404 390 378 Z" {o("#ff8a1f", 'stroke-width="8"')}/>
<path d="M340 388 L348 380 L352 392 Z M362 392 L366 380 L374 388 Z" fill="{INK}"/>
<path d="M340 402 L346 408 L352 402 L358 408 L364 402 L370 408" fill="none" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>
<path d="M338 376 L346 366 L356 376 Z" fill="#ff5fa8" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>
<path d="M360 374 L368 362 L376 376 Z" fill="#7cff4f" stroke="{INK}" stroke-width="4" stroke-linejoin="round"/>
<circle cx="356" cy="352" r="12" {o(BLUE, 'stroke-width="7"')}/>
<rect x="244" y="262" width="24" height="26" rx="6" fill="#4a5a8a" stroke="{INK}" stroke-width="6"/>
<ellipse cx="256" cy="196" rx="86" ry="80" {o(WHITE)}/>
<path d="M176 196 C168 134 204 104 256 104 C308 104 344 134 336 196 C328 176 318 160 302 152 C292 166 270 170 256 150 C242 170 220 166 210 152 C194 160 184 176 176 196 Z" {o(NAVY, 'stroke-width="9"')}/>
<path d="M214 122 C230 112 250 110 266 112" fill="none" stroke="#5a6bb0" stroke-width="8" stroke-linecap="round"/>
{toon_eye}
<g transform="translate(512 0) scale(-1 1)">{toon_eye}</g>
<path d="M202 160 Q218 150 236 152 M276 152 Q294 150 310 160" fill="none" stroke="{INK}" stroke-width="5" stroke-linecap="round"/>
<path d="M240 236 Q256 255 272 236 Q256 242 240 236 Z" fill="{INK}" stroke="{INK}" stroke-width="5" stroke-linejoin="round"/>
<path d="M249 245 Q256 250 263 245" fill="none" stroke="#ff7aa8" stroke-width="3" stroke-linecap="round"/>
{highlight(208, 150, 8, 22, -30, 0.5)}
<path d="M176 196 C176 214 180 228 188 240" fill="none" stroke="{PANEL}" stroke-width="6" stroke-linecap="round"/>
{sparkle(76, 120, 16, TEAL)}{sparkle(440, 150, 12, "#fff")}{sparkle(60, 330, 10, "#ff5fa8")}{sparkle(452, 320, 14, TEAL)}
{sparkle(410, 440, 9, "#fff")}{sparkle(110, 440, 8, TEAL)}
'''


CHARACTERS = {
    "pip": pip, "skelly": skelly, "batsy": batsy, "sheety": sheety,
    "wanda": wanda, "mumbles": mumbles, "howler": howler,
    "fangsworth": fangsworth, "gourdking": gourd_king, "jenny": jenny,
}
