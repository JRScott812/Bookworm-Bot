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

1. **Tap a tile** on the 4×4 grid to open the editor — set letter, gem (emoji picker), and status (none / locked / cracked / burning / empty).
2. Configure **loadout** (enemy, treasures, lex level) on the left.
3. **Refresh words** to see ranked suggestions with damage.
4. **Select a word** — used tiles highlight on the grid.
5. Edit the highlighted cells with drop-in letters, then click **Apply drop-in tiles**.

Locked and empty cells are excluded from word finding. Cracked and burning tiles are playable (visual only in v1).

Use **Sample board** for a quick demo, or **Clear board** to reset.

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
- Tile modifiers (locked, burning, cracked) are not detected; all recognized tiles are treated as normal.
- Letter recognition uses Windows OCR; accuracy depends on resolution and calibration.
- During drop-in (partial board), suggestions appear only when enough tiles are recognized.
