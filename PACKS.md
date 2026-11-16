# Collections (packs): how to bring new pals to a party

Peekaboo Pals is one app that holds many **collections**, one per party (Halloween 2026, Christmas 2026, a birthday…).
The app's code never changes for a new party. A collection is just a **folder of files** in `Packs/`:
a `pack.json` describing it, plus its pictures and sounds.

```
Packs/
  site.json                ← web address for QR links, which collection opens first, teaser books
  halloween-2026/          ← one folder per collection (the folder name is its id)
    pack.json
    codes.json             ← QR secrets. Generated for you. PRIVATE, back it up!
    logo.png  scenery.png
    art/pip.png  art/skelly.png  …
    voice/pip.wav  …
  _template/               ← copy this to start a new collection (folders starting with "_" are ignored)
```

When you build the app, Unity validates every folder and ships it inside the app
(`Assets/StreamingAssets/Packs`, which is generated, so don't edit it). Only a *hash* of each QR secret ships, never the
secret itself, so nobody can read the codes out of the app or the website.

**Collections also download by themselves.** The website (your Netlify site) carries the same Packs folder, and
every installed Android app checks it on launch. A new collection reaches every phone with **one Netlify upload,
no new APK**. See [Delivering a new collection](#delivering-a-new-collection-no-new-app).

---

## New party checklist (e.g. Christmas)

1. **Copy** `Packs/_template` to `Packs/christmas-2026`. The folder name is the collection id: lowercase letters, digits and dashes.
2. **Edit** `Packs/christmas-2026/pack.json`: set `"id"` to the folder name, then the title, date, colors, texts and characters (reference below).
3. **Add art**: one PNG per character in `art/`, named after the character id (`art/snowbo.png`). Specs below.
4. *(optional)* Add a logo, cover, background, scenery and one short sound per character (`voice/snowbo.mp3`).
5. *(optional)* In `Packs/site.json`, set `"featured": "christmas-2026"` so the app opens on it, and remove the
   "Christmas 2026" teaser from `"upcoming"`. Add the next party's teaser if you like.
6. **In Unity**: menu **Party → Packs → Build Packs**. It lists errors and warnings per pack in the Console and
   generates the QR secrets into `codes.json`. Fix what it reports and run it again until it says OK.
7. **Print the tags**: menu **Party → Packs → Export QR Sheets**. It opens `QRSheet/christmas-2026/index.html`.
   Open it in Chrome → Print → A4, scale 100%, margins *None*, no headers/footers. The last page is your
   **cheat sheet** (which tag is which character); keep it to yourself.
8. **Deliver it**: **Party → Packs → Update Website Folder**, then upload `Builds/WebGL` to Netlify (next section).
   Then test one tag with your own phone *before* hiding them.

> Back up `codes.json`. If it's lost, the printed tags stop matching. Building again would generate new
> secrets, and you'd have to re-print.

---

## Delivering a new collection (no new app)

This is the usual way, for every new party:

1. Unity: **Party → Packs → Update Website Folder**. It builds the packs and puts them into the last web build
   (`Builds/WebGL/StreamingAssets/Packs`). It takes seconds and doesn't rebuild the app. (The very first time, it
   asks you to run **Party → Build WebGL** once.)
2. Netlify → your site (guileless-dusk-fc1546) → **Deploys** → drag the **`Builds/WebGL` folder** onto the drop area.
3. Done. Here's what happens on the phones:
   - **Android app**: when it opens with internet, it checks the website, downloads only what's new or changed
     (verifying every file), and keeps it on the phone. A new collection pops up right away: *"A new collection!
     Open it"*. An open app checks again when it comes back from the background (after 5+ minutes away).
   - **iPhone / web**: the page *is* the website, so it has the new collection on the next visit.
   - **A guest scans a tag of a collection their phone doesn't have yet**: the app says *"A pal from a new
     collection! Downloading it…"*, fetches it and catches the pal. No internet: *"Connect, then scan it again"*.
   - **Offline phones** keep working with everything already on them: the built-in collection plus all downloads.

Good habits:
- Upload **a day before the party** and open the app once on your own phone online. Guests' apps fetch it in the background.
- **Fixing a typo or a picture** after uploading: edit, then Update Website Folder and upload again. Phones download
  only the changed pack. A collection that's already open on a phone switches to the new version the *next time the
  app opens* (the album shelf says *"Collection updates downloaded!"*).
- Deleting a collection from the website **does not** delete it from phones: people keep the pals they caught.
- `codes.json` never goes to the website (Update Website Folder only copies the shipped files).
- The pack files on the website are public (anyone who knows the address can open the pictures). Hiding works because
  of the QR secrets, not because the art is private. If spoilers matter, upload close to the party.

## Delivering a new app version (only when the code changes)

You only need a new APK when the *game itself* changes (a new effect, a new feature, a bug fix). New characters,
art, sounds, texts, colors and themes never need one.

1. Unity: **Edit → Project Settings → Player → Version**: raise it, e.g. `2.0` → `2.1`. (The Android "version
   code" follows it automatically, so phones accept the update.)
2. **Party → Build WebGL (iPhone + browsers)**, then **Party → Build Android APK** (WebGL first, Android last,
   so the editor stays on Android).
3. Google Drive: right-click the existing APK → **File information → Manage versions → Upload new version**.
   The link stays the same, so the posters keep working.
4. Netlify: drag `Builds/WebGL` onto **Deploys** (do this *after* the Drive upload).
5. Phones with the older app get a one-time popup *"A new version is out! Get it"* that opens the Drive link (the link
   comes from `"androidDownload"` in `site.json`), plus a banner on the album shelf. Installing over the old app keeps
   all progress (same app id since the Halloween version).

**Collections that need the new app.** If a collection uses something only the new app has, set
`"minAppVersion": "2.1"` in its pack.json. (Build Packs also does it for you when a pack uses an effect added in a
later version; it lists *"needs app 2.1+"*.) Older apps then show it as a gold **UPDATE** book on the album shelf,
and scanning its tags says *"Update the app!"* with a Get it button, instead of a half-working collection.

**This update (2.0) is the one APK everybody needs once**: 1.x apps (the Halloween build) can't download
collections. Ask everyone to install 2.0 before the next party. After that, new parties only need Netlify.

## File specs

| File | Size | Notes |
|---|---|---|
| `art/<id>.png` | **512×512**, transparent PNG, under ~200 KB | The character centered, feet near the bottom, a little padding. Locked cards show its silhouette, generated automatically from the transparency (a soft ground shadow is ignored). Want your own? Add `"silhouette": "art/<id>_locked.png"`. |
| `logo.png` | ~940×340, transparent PNG | Shown at the top of the collection's home screen and on the printed tags. Without one, the title is written in the game font in your accent color. |
| `cover` | any square PNG | The picture on the album-shelf book. Defaults to the first character. Pick a common character so you don't spoil the legendary. |
| `background.png` | 1080×1920 PNG | Full-screen sky. Without one, the app's purple night sky is used. Keep it dark so the cards pop. |
| `scenery.png` | 1080×560, transparent top | The strip along the bottom of the screen (Halloween has graveyard hills). Without one, the app's party-lights hills are used. |
| `voice/<id>.wav` or `.mp3` | under ~3 s, ~200 KB | Plays when the character is revealed or tapped. Found automatically by name. **Not `.ogg`**: iPhone Safari can't play it. |

Draw art however you like (by hand, or SVG like `Tools/art/characters.py`), as long as it's yours to use.
List outside assets in `CREDITS.md`.

---

## pack.json reference

Only `id`, `title` and `characters` are required; everything else has a sensible default.

```jsonc
{
  "id": "christmas-2026",            // = folder name
  "title": "Christmas 2026",         // shown everywhere
  "date": "2026-12-24",              // party date; the album shelf is sorted newest first
  "tagline": "Frosty pals are hiding around the tree",
  "legacyPrefix": "SPOOKY",          // ONLY Halloween uses this (its old tags look like SPOOKY:id:SECRET). Leave out.
  "minAppVersion": "2.1",            // optional: older apps show "update the app" instead of opening it. Usually leave out.

  "theme": {
    "accent": "#7fd4ff",             // titles, the album book, the albums button
    "accent2": "#ffffff",            // progress bars, the "found" counter
    "logo": "logo.png", "cover": "art/snowbo.png",
    "background": "background.png", "scenery": "scenery.png",
    "moon": true,                    // the glowing moon in the sky
    "ambient": "snow",               // what drifts by: sparkles | embers | snow | confetti | hearts | none
    "bats": false                    // Halloween bats flying by (and in reveals)
  },

  "texts": {
    "friends": "frosty pals",        // "You found all 4 frosty pals!"
    "haulTitle": "MY FROSTY HAUL",   // results screen title
    "ranks": ["…", "…", "…", "…", "…", "…"]  // exactly 6: nothing yet, <30%, <60%, <90%, almost, complete
  },

  "rarities": {                      // rename/recolor tiers for this party (points stay the same)
    "common":    { "name": "Chilly" },
    "rare":      { "name": "Frosty" },
    "epic":      { "name": "Jolly" },
    "legendary": { "name": "Christmas", "color": "#ffd23f", "secondary": "#0f1a3d", "glow": "#fff3b0" }
  },

  "floatGrades": ["Fresh Snow", "Crisp", "Lightly Thawed", "Slushy", "Muddy Snow", "Melted"], // 6 names, best first

  "characters": [
    { "id": "snowbo",                // lowercase, unique in this pack; it's in the QR code: don't rename after printing
      "name": "Snowbo",
      "rarity": "common",            // common | rare | epic | legendary
      "power": 18,                   // 1-99, shown on the card. Usual ranges: common 10-30, rare 40-60, epic 70-85, legendary 99
      "color": "#e8f6ff",
      "description": "Lost his carrot nose. Pretends it's a fashion choice.",
      "art": "art/snowbo.png",       // optional, this is the default
      "voice": "voice/snowbo.mp3",   // optional, found automatically
      "teaser": "Something is coming down the chimney…",  // optional: replaces the build-up line of a legendary reveal
      "signature": "robot"           // optional extra reveal effects; only "robot" exists (Jenny's sparks)
    }
  ]
}
```

**How scoring works** (the same for every collection, set in `Assets/_Main/Configs/CollectionConfig.asset`):
common 10 · rare 25 · epic 50 · legendary 100 points, plus a float bonus (up to +100 for a super-rare roll),
plus **+100 for completing** the collection. A pal can be re-caught every 3 minutes for a chance at a rarer float.
A good party roster is about **4 common, 3 rare, 2 epic, 1 legendary** (8–12 tags is plenty for a house).
Small parties work too (Build Packs only warns if there's no legendary).

**`site.json`**

```jsonc
{
  "linkBase": "https://guileless-dusk-fc1546.netlify.app/",  // printed into every new QR code; phones also look for new
                                                              //   collections at <linkBase>StreamingAssets/Packs/
  "androidDownload": "https://drive.google.com/file/d/…/view", // where "A new version is out → Get it" sends Android players
  "featured": "halloween-2026",                               // the collection the app opens on (first launch / new players)
  "upcoming": [ { "title": "Christmas 2026", "date": "2026-12-24" } ]  // locked teaser books on the album shelf
}
```

---

## About the QR codes

- New tags contain a link: `https://<your site>/?c=christmas-2026.snowbo.K7QX2M`. Scanning one **with the phone's
  own camera opens the web game and catches the pal right away**, which is handy for guests without the app. Scanning in
  the app works as before. The app accepts the code from any web address, so tags keep working even if the site moves.
- **Want a nicer address than guileless-dusk-fc1546?** Netlify → Site configuration → Change site name (e.g.
  `peekaboo-pals.netlify.app`). Do it **before** printing a collection, update `linkBase` in `site.json`, and rebuild
  *and ship the APK* (installed apps look for new collections at the address they were built with, and at the address
  in the last index they downloaded). Already-printed tags keep working in the app, but their "open with the phone
  camera" link would point at the old address. Netlify keeps no redirect from the old name, so change it only once, early.
- Your Halloween tags (`SPOOKY:pip:…`) keep working forever.
- A tag from a collection the phone doesn't have yet downloads it from the website and catches the pal. If the website
  doesn't have it either, the app says "A pal from the future!".
- Menu **Party → Packs → Verify QR Codes** checks that every code decodes back to its own pal.

---

## What players see

- The home screen shows **one collection**. The **albums button** (top-left) opens the **album shelf**: every
  collection as a book with its date, progress, points and a gold COMPLETE badge, the lifetime score, and teaser
  books for upcoming parties. Tap a book to look at that collection.
- Scanning any tag jumps to its own collection automatically.
- Host tools (hold the collection logo for about 1 second): **Reset this collection** wipes just the collection on screen.
  **Erase everything** wipes all of them. Nickname and settings are kept either way.
- iPhone Safari may delete website data that hasn't been used for ~7 days. Ask iPhone players to **Add to Home
  Screen** (the loading screen says so too), which keeps their collections safe between parties.

---

## How downloads work (for the curious)

- Every pack folder gets a `manifest.json` listing each file with its hash, and `index.json` lists every pack with
  one hash and an "updated" time. Build Packs keeps a pack's "updated" time while its files don't change.
- On launch, the Android app loads what it has (built-in + downloaded), starts the session, then reads the website's
  `index.json` in the background. A pack is downloaded only when its hash differs **and** it's newer than the copy on
  the phone, so an older website never downgrades a phone. Downloads land in a temporary folder, every file is checked,
  and only a complete pack replaces the old one.
- A newer built-in copy (a new APK) wins over an older download, which is then deleted.
- Testing locally: set the environment variable `PEEKABOO_REMOTE_PACKS` to any folder URL (e.g. a local
  `python3 -m http.server`); the editor only checks online when it's set, so pack edits in the editor are never hidden
  by an old download.

## Troubleshooting

| Problem | Fix |
|---|---|
| Build Packs says *"id must be the folder name"* | Make `"id"` in pack.json identical to the folder name. |
| *"art file 'art/x.png' not found"* | The file name must match the character id exactly (lowercase). |
| The build stops with *"Collection packs have errors"* | Every build runs Build Packs first. Open the Console, fix the listed problems. |
| A character shows a white box | Its PNG failed to load: re-export it as a normal PNG (not WebP/HEIC renamed to .png). |
| A tag says *"That's not one of our codes"* | The tag was printed before its secret changed (codes.json was lost/edited). Export and print again. |
| A pal's voice doesn't play on iPhone | Use .mp3 or .wav, not .ogg. |
| Phones don't get the new collection | Check `https://<your site>/StreamingAssets/Packs/index.json` in a browser: it should list it. Then open the app online (it checks on launch, or after 5+ minutes in the background). The album shelf says "Offline…" when it can't reach the site. |
| A phone still shows the old version of a collection | Updates of a collection already on the phone apply the next time the app is opened (swipe it away, open again). |
| Update Website Folder says there's no web build | Run **Party → Build WebGL** once; after that, Update Website Folder is enough. |
| An Android phone shows "A new version is out" every launch | It shows once per version. If it keeps appearing, the APK on Drive is older than Player Settings → Version: upload the new APK. |
