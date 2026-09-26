# Level Definition Editor

## Open or create a level

1. In Unity's top menu, click **Tools > Level Definition Editor**. The window shows the level library on the left, the board in the middle, and settings on the right.
2. Click **New** in the window toolbar to create an unsaved 8 × 8 board, or click a filename in **LEVEL LIBRARY** to open it. **Open…** also accepts a level JSON selected through the file picker. Invalid or unsupported input is reported without replacing the current document.
3. Under **LEVEL SETTINGS**, enter **Level ID** and **Chapter ID**. For a copy, click **Duplicate** in the toolbar and enter a different **Level ID** before saving. Duplication keeps the original file intact.

## Paint the board

1. Under **TILE BRUSH**, enable **Playable tile** for ordinary cells or disable it for inactive cells. Enable **Empty board space** to paint holes. For a web on a playable tile, enable **Has feature**, select **Web** under **Feature**, and enter a positive **Web layers** value. Disable **Has feature** to paint a tile without a feature.
2. For a tile that can contain a flower, choose **Flower (None = random)**. Select **None** to let the game initialize the flower, or select a fixed flower. A fixed flower also enables **Skill**. Blocked tiles do not receive flower or skill data when painted.
3. Click or drag over the board to replace cells with the brush. Right click a cell to copy its settings into the brush. One drag is one undo operation. Use the toolbar's **Undo** and **Redo** buttons to revise edits.
4. To change dimensions, enter **Columns** and **Rows** above the board, then click **Resize**. Existing cells stay aligned to the top left; new cells are normal tiles with random flowers. Shrinking asks before removing cells.
5. Adjust **Zoom** above the board and use its scrollbars to inspect larger boards. **Fill board with brush** in the right panel replaces all cells after confirmation.

The board uses colored labels rather than game sprites. **RANDOM** is an authoring instruction, not a preview of the generated flower arrangement.

## Set the rules

1. In the right panel, scroll to **OBJECTIVES**. Click **+ Match**, then choose flower types and amounts. Click **Add flower goal** for additional targets. **+ Clear webs** adds a target measured in fully cleared web tiles, not individual layers. Butterfly objectives are not offered because the current game factory does not implement them.
2. Scroll to **LIMITS** and click **+ Move limit** or **+ Time limit**. Enter the limit and **Warn at remaining**. The warning must be positive and below the limit. Removing both limits allows unlimited play.
3. Under **STAR THRESHOLDS**, edit the star count in the left field and score in the right field. Both must increase together. **Add star threshold** creates a row for you to fill in.
4. Under **LEVEL SETTINGS**, toggle allowed boosters and, if needed, enable **Has next level** and enter **Next level ID**. **Published config flag** edits only the JSON flag; it does not upload anything.

## Save

1. Scroll to **VALIDATION** in the right panel and resolve reported errors. Checks cover config consistency, not solvability or difficulty.
2. Click **Save** in the toolbar. The file is written to `ServerData/configs/levels/level_<Level ID>.json`. Changing the ID saves to the new filename and leaves the original file intact. Opening a JSON from another folder also saves into this canonical output folder.
3. If a destination already exists or was changed outside the tool, review the overwrite dialog before choosing **Replace**. **Cancel** leaves the file unchanged. A failed save leaves the document unsaved and reports the error.

Saving does not upload remote configs, place chapter map buttons, modify scenes or prefabs, or start a gameplay session. Use the existing chapter editor for map placement and the existing deployment workflow to publish the JSON. Playable preview is a separate feature.
