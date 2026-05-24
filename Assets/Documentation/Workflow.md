# Map Content Workflow

## Adding Map Chunks

1. Slice and import new chunk PNG into Unity
2. Mark as Addressable — address format: `LevelPath_X` (next number in sequence)
3. **Tools → Addressable Metadata Exporter**
    - Group: `LevelPath`
    - File name: `chunk_manifest`
    - Click Export

## Adding Level Buttons

> Add chunks first before placing new buttons — content height depends on chunks.

1. **Tools → Level Position Editor**
2. Browse → select `StreamingAssets/levels/level_meta.json`
3. Drag background image → click **Fit Content to Background**
4. Click **Start Editing** — existing buttons spawn on road
5. Click **Add New Level** — new marker spawns at last button position
6. Drag marker to correct position on road
7. Click **Finish Editing** — saves JSON, removes markers from scene

## Notes
- Button IDs are derived from GameObject name — do not rename markers to non-numeric values
- Chunk addresses must follow `LevelPath_X` naming exactly
- Always commit both `chunk_manifest.json` and `level_meta.json` after changes