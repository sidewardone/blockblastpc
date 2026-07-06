English | [Русский](README.ru.md)

# BlockBlast!

A bright, casual block puzzle game for Windows desktop, inspired by the popular mobile game *Block Blast*. Built with WPF and .NET 8.

## Gameplay

- The board is an 8x8 grid.
- Three random block shapes are offered in a tray at the bottom of the screen at all times.
- Drag a shape from the tray onto the board with the mouse. While dragging, the cells the shape would occupy are highlighted green (valid placement) or red (invalid — occupied or out of bounds).
- Drop the shape to place it. Once all three tray shapes have been used, three new random shapes appear.
- Completely filling a row or column clears it with a flash/fade animation.
- Clearing multiple lines in a row (without a placement in between that fails to clear anything) builds a combo streak, shown on screen as "Combo x2", "Combo x3", etc., multiplying your bonus score.
- The game ends when none of the three current shapes can be placed anywhere on the board. A Game Over screen shows your final score and lets you start again.

## Shapes

The full classic set is included: single cells, lines of 1–5 cells (horizontal and vertical), 2x2 and 3x3 squares, L/J tetrominoes, T-tetrominoes, S/Z zigzags, small 3-cell corners, large 5-cell corners, and the plus shape. Each shape is drawn in a random bright color.

## Scoring

- +1 point for every cell a placed shape occupies.
- +10 points per cleared row/column, multiplied by the current combo streak.
- The combo streak increases every time a placement clears at least one line, and resets when a placement clears none.

## Controls

- **Mouse drag-and-drop** — pick up a shape from the tray and drop it on the board.
- **RU / EN button** (top-right corner) — toggle the interface language at any time.
- **Start again** — restart after Game Over.

## Localization

The interface starts in the language matching your Windows system locale (Russian if your Windows UI language is Russian, English otherwise), and can be switched anytime with the language button.

## Best score

Your best score is saved automatically to `%AppData%\BlockBlast\save.json` and persists between launches.

## Project structure

```
BlockBlast/
  BlockBlast.sln
  BlockBlast/
    Core/             game logic — shapes, board, scoring, no UI dependencies
    Persistence/       best-score save/load
    Localization/       RU/EN string dictionary and system-language detection
    UI/                WPF visual builders (shape/block rendering)
    MainWindow.xaml(.cs)   the window, drag-and-drop, animations
    Assets/icon.ico
installer/
  setup.iss            Inno Setup installer script
tools/
  make_icon.ps1        regenerates Assets/icon.ico
```

## Building and running from source

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), Windows.

```powershell
dotnet build BlockBlast.sln -c Debug
dotnet run --project BlockBlast/BlockBlast.csproj
```

## Publishing a self-contained executable

Produces a single `BlockBlast.exe` that runs on a machine without .NET installed:

```powershell
dotnet publish BlockBlast/BlockBlast.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o BlockBlast/bin/publish
```

## Building the Windows installer

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php).

1. Publish the self-contained executable (see above) so `BlockBlast/bin/publish/BlockBlast.exe` exists.
2. Compile the installer:

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup.iss
```

The installer is written to `dist\BlockBlast-Setup.exe`. It installs to Program Files, adds a Start Menu shortcut, optionally a desktop shortcut, and registers an uninstaller in "Apps & Features".

## License

This project's source code is released under the [MIT License](LICENSE). *Block Blast* is a trademark of its respective owner; this is an independent, unofficial fan implementation created for educational purposes and is not affiliated with or endorsed by the original game's publisher.
