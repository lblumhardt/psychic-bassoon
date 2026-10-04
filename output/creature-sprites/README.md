# Creature sprite concepts

Ten idle creature sprites generated with the built-in ImageGen tool, using the project's Hampterrian, Slamander, and Looners sprites as style references.

Game assets: `Assets/Textures/CreatureConcepts/*.png`.

Each game asset is a 32x32 RGBA PNG. The generated artwork was cropped to its visible bounds and sampled with nearest-neighbor scaling into a centered transparent canvas. Alpha is binary to avoid soft edges. Creature extents range from 19 to 28 pixels; these are single idle frames, not animations.

Unity metadata matches the existing Hampterrian sprite: Sprite (single), point filtering, no mipmaps, uncompressed default texture, 100 pixels per unit, centered pivot. These assets are not assigned to creature data or prefabs yet.

`preview.png` shows the actual exported sprites at 4x scale. `originals/` preserves the larger generated images. `manifest.json` records the exact prompts and source paths. `export-sprites.ps1` reproduces the exports on Windows using System.Drawing.
