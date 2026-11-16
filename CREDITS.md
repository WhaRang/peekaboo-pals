# Credits

Every third-party asset in the game, with its source and license. Nothing with an unclear license is used.

## Fonts (bundled in `Assets/_Main/Fonts`)

| Asset | Author | License | Source |
|---|---|---|---|
| Luckiest Guy (`LuckiestGuy-Regular.ttf`) | Astigmatic (Brian J. Bonislawsky) | Apache License 2.0 (`LuckiestGuy-LICENSE.txt`) | https://fonts.google.com/specimen/Luckiest+Guy · https://github.com/google/fonts/tree/main/apache/luckiestguy |
| Fredoka SemiBold (`Fredoka-SemiBold.ttf`) | The Fredoka Project Authors | SIL Open Font License 1.1 (`Fredoka-OFL.txt`) | https://fonts.google.com/specimen/Fredoka · https://github.com/google/fonts/tree/main/ofl/fredoka |

The Fredoka file is a static weight-600 instance cut from the official variable font with fontTools (`varLib.instancer`). The OFL allows this, and Fredoka declares no Reserved Font Name.

## Sound effects (bundled in `Assets/_Main/Audio/Kenney`)

All of these are by Kenney (www.kenney.nl) and licensed **CC0 1.0** (public domain). The license files are copied next to the clips.

| File | Pack | Source |
|---|---|---|
| `click_001.ogg`, `switch_002.ogg`, `maximize_006.ogg`, `minimize_006.ogg`, `error_006.ogg`, `toggle_001.ogg` | Interface Sounds 1.0 | https://kenney.nl/assets/interface-sounds |
| `lowDown.ogg` | Digital Audio | https://kenney.nl/assets/digital-audio |
| `card-place-1.ogg` | Casino Audio | https://kenney.nl/assets/casino-audio |

## Code libraries

| Library | License | Source |
|---|---|---|
| ZXing.Net 0.16.11 (QR encoding and decoding, vendored in `Assets/_Main/NuGet/ZXing.Net.0.16.11`) | Apache License 2.0 | https://github.com/micjahn/ZXing.Net · https://www.nuget.org/packages/ZXing.Net |
| jsQR 1.4.0 (QR decoding in the WebGL build, embedded in `Assets/Plugins/WebGL/SpookyJsQR.jspre`, license in `jsQR-LICENSE.txt`) | Apache License 2.0 | https://github.com/cozmo/jsQR · https://www.npmjs.com/package/jsqr |
| Arch 2.1.0 + Arch.LowLevel (ECS, from the template) | Apache License 2.0 | https://github.com/genaray/Arch |
| VContainer 1.17.0 (DI, from the template) | MIT | https://github.com/hadashiA/VContainer |
| OneOf (from the template) | MIT | https://github.com/mcintyre321/OneOf |
| Newtonsoft.Json for Unity (from the template) | MIT | https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2 |
| TextMeshPro Essential Resources (imported by the builder) | Unity Companion License | Unity `com.unity.ugui` package |

## Print materials (`Tools/print`, not part of the game build)

| Asset | Author | License | Source |
|---|---|---|---|
| Rubik (`Tools/print/fonts/Rubik[wght].ttf`, used for Belarusian signs since the game fonts have no Cyrillic) | The Rubik Project Authors | SIL Open Font License 1.1 (`Rubik-OFL.txt`) | https://github.com/google/fonts/tree/main/ofl/rubik |
| segno (QR codes on the download posters, Python tool dependency) | Lars Heuer | BSD 3-Clause | https://github.com/heuer/segno |

## Made for this project (original, released as CC0 except where noted)

- **Peekaboo Pals brand**: the mascot "Peeky", the logo, the app icon, the pal and albums icons and the party-lights hills (`Assets/_Main/Art/UI`), drawn in `Tools/art/brand.py`.
- **Halloween 2026 collection** (`Packs/halloween-2026`): all 10 characters (also kept in `Assets/_Main/Art/Characters` for the print tools), the pack logo and the graveyard scenery, drawn as SVG in `Tools/art/characters.py`, `brand.py` and `ui.py`. Nine characters are original designs.
- **Jenny XJ-Boo** (the legendary) is a fan tribute to Jenny Wakeman / XJ-9 from *My Life as a Teenage Robot* (created by Rob Renzetti, © Nickelodeon). The drawing is new, made from simple shapes and not traced or copied from any official artwork, but the character concept it references belongs to its owners and is **not** covered by this section's CC0 release. It is fine for a free, private party game; remove or replace it before any commercial or public release.
- **UI art**: card frames, card back, icons, particle atlas, sky and moon (`Assets/_Main/Art/UI`), drawn in `Tools/art/ui.py`. The logo lettering uses Luckiest Guy (see above).
- **Music and sounds**: the lobby music loop, the rarity stingers, the legendary and completion fanfares, the 10 Halloween character voices (copied into `Packs/halloween-2026/voice`; Jenny's is synthesized robot chirps, not a recording), and the scan beep, whoosh, sparkle, rumble, build-up, firework, slot-fill and points-tick sounds (`Assets/_Main/Audio/Generated`). They are synthesized from scratch by `Tools/audio/synth.py`, with no samples.

To regenerate them, run `python3 Tools/art/build.py` (needs Google Chrome and Pillow) and `python3 Tools/audio/synth.py` (needs numpy).

Art and sounds you add to new collections in `Packs/` are yours: list their sources and licenses here too.
