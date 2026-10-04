# Original roster sprites

Nine single-frame idle sprites created with the built-in ImageGen tool, based on the user's creature design document and the project's existing Hampterrian, Slamander, and Looners art.

Final assets: `Assets/Textures/OriginalCreatures/`.

- Beautipillar: green caterpillar with a pink flower.
- Critical Cat: red apple cat with a leaf and stem.
- Gooby: small lavender blob with a mint belly.
- Xylos: violet alien with ribbon arms and a teal core.
- Concreter: concrete construction creature with a hardhat and rebar.
- Entity: golden eye-ring angel with six ivory wings.
- Toadstack: three green toads stacked vertically.
- Horseshoe: horseshoe-shaped western bandit.
- OuttaTime: purple and ivory time jester.

Each export is a transparent 32x32 PNG, with a centered silhouette occupying 19–29 pixels along its longest edge. Generated originals are cropped and sampled with nearest-neighbor scaling; alpha is binary. Unity import settings match Hampterrian: Sprite (single), point filtering, no mipmaps, uncompressed default texture, centered pivot, 100 pixels per unit.

Beautipillar, Critical Cat, Gooby (`DebugEnemyData.asset`), and Xylos are assigned through their existing `creatureTexture` fields. The other five have artwork ready for future creature implementation.

`preview.png` shows actual exports at 4x scale. `originals/` preserves generated source images. `manifest.json` records the full prompts and source paths; `export-sprites.ps1` reproduces the exports.
