# Bookworm Bot

Bookworm Adventures word helper — suggests high-damage words from your tile board.

## Projects

| Project | Role |
|---------|------|
| **Bookworm Bot Class** | Core library: solver, dictionary, scoring, grid board, `GameSession` |
| **Bookworm Bot GUI** | WinUI 3 desktop app (primary) |
| **Bookworm Bot CLI** | Console app (advanced / terminal) |
| **Bookworm Bot Tests** | MSTest unit tests |
| **Bookworm Bot Automation** | Reads the BWA Deluxe board from screen and prints ranked suggestions |

## Word Banks

Shared dictionary files live in [`Word Banks/`](Word Banks/) at the repo root. All apps copy them to output via [`Directory.Build.props`](Directory.Build.props).

- `words.txt` — BWA1 game dictionary (trie)
- `colors.txt`, `metals.txt`, `mammals.txt` — category tags only

## Build

```bash
dotnet build "Bookworm Bot.slnx" -p:Platform=x64
dotnet test "Bookworm Bot.slnx"
```

## GUI (recommended)

```bash
dotnet run --project "Bookworm Bot GUI" -p:Platform=x64
```

Or run **Bookworm Bot GUI** from Visual Studio (x64, Unpackaged profile).

### Using the board

1. **Tap a tile** on the 4×4 grid to open the editor — set letter, gem (emoji picker), and status (none / locked / smashed / plagued / empty).
2. Configure **loadout** (enemy, treasures, lex level, power-up potion) on the left.
3. **Refresh words** to see ranked suggestions with damage and Lex word-power labels (Good → Astonishing).
4. **Select a word** — used tiles highlight on the grid.
5. Edit the highlighted cells with drop-in letters, then click **Apply drop-in tiles**.

Locked tiles are excluded from word finding. **Smashed** and **plagued** tiles can be used in words but contribute **zero** to adjusted word length (matching the game). Empty cells are ignored until filled.

Use **Sample board** for a quick demo, or **Clear board** to reset.

## Scoring (from BWA Deluxe)

Damage uses **adjusted word length** (letter pip values summed and rounded up):

| Pip tier | Letters | Weight |
|----------|---------|--------|
| Bronze | A E I O U D G L N R S T | 1.0 |
| Silver | B C F H M P | 1.25 |
| Silver | V W Y | 1.5 |
| Gold | J K | 1.75 |
| Gold | X Z | 2.0 |
| Gold | Qu | 2.75 |

Base damage by adjusted length (hearts): 3→0.5, 4→0.75, 5→1, 6→1.5, 7→2, 8→2.75, 9→3.5, 10→4.5, 11→5.5, 12→6.75, 13→8, 14→9.5, 15→11, 16→13.

**Gems** add stacking damage bonuses: Amethyst +15%, Emerald +20%, Sapphire +25%, Garnet +30%, Ruby +35%, Crystal +50%, Diamond +100%.

**Power-up potion** (+25% damage) and **Lex level** attack bonuses are supported in loadout settings. **Jeweled Key** / **Endless Gem Pouch** show short-word gem creation odds in suggestion bonuses.

Words must be at least **3 letters** (some enemies require 4+). Tiles are selected from anywhere on the 4×4 grid — they need not be adjacent (unlike original Bookworm).

## CLI (advanced)

```bash
dotnet run --project "Bookworm Bot CLI"
```

Enter tiles with optional gems (`e$ruby`), pick a word by number or name, then re-enter the full board or `+` drop-in tiles. The CLI uses a flat tile list (no grid positions).

## Automation (screen read + suggest)

Reads the 4×4 tile grid from **Bookworm Adventures Deluxe** and prints damage-ranked word suggestions. Loadout settings are shared with the GUI via `%LocalAppData%\Bookworm Bot\loadout.json`.

**Two separate steps:** (1) **Calibration** saves where the grid is on screen. (2) **Reading** captures tiles and recognizes letters. If calibration saves but refresh fails, the grid position is probably fine — letter recognition failed. Press **D** to diagnose; do not recalibrate if `calibration-overlay.png` already lines up.

```bash
dotnet run --project "Bookworm Bot Automation"
```

Use **`--calibrate`** only the first time (or after resizing the game window). That flag calibrates, then drops you into the main loop automatically.

```bash
dotnet run --project "Bookworm Bot Automation" -- --calibrate
```

Close any running Bookworm Bot Automation window before rebuilding.

### First-time setup

1. Configure treasures, enemy, and lex level in the **GUI** app (saved automatically).
2. Start Bookworm Adventures Deluxe and open a battle so the tile grid is visible.
3. Calibrate once per window resolution (uses **F8**, not Enter):

```bash
dotnet run --project "Bookworm Bot Automation" -- --reset-calibration
dotnet run --project "Bookworm Bot Automation" -- --calibrate
```

Hover the mouse on the **top-left** then **bottom-right** outer corners of the 4×4 grid and press **F8** at each corner. You do not need to Alt+Tab or press Enter in the console. Calibration is saved to `%LocalAppData%\Bookworm Bot\automation.json`.

**Black preview?** The game must be **visible on screen** (not minimized, not covered). Windowed mode works best. The app captures what you see on screen, not the game's hidden back buffer.

### Runtime commands

| Key | Action |
|-----|--------|
| **R** | Refresh board read and suggestions |
| **C** | Run calibration again |
| **Q** | Quit |

Optional flags: `--save-frame` (writes `last-frame.png`), `--dump-board` (shows raw letter/gem classification).

### Limitations (v1)

- Suggest only — does not click or type words in the game.
- Tile modifiers (locked, smashed, plagued) are not detected by automation; all recognized tiles are treated as normal.
- Letter recognition uses Windows OCR; accuracy depends on resolution and calibration.
- During drop-in (partial board), suggestions appear only when enough tiles are recognized.
