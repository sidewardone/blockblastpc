English | [Русский](README.ru.md)

# BlockBlast!

![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-WPF-239120?logo=csharp&logoColor=white)
![Localization](https://img.shields.io/badge/localization-EN%20%7C%20RU-blue)
![License](https://img.shields.io/badge/license-MIT-brightgreen)
[![Release](https://img.shields.io/github/v/release/sidewardone/blockblastpc)](https://github.com/sidewardone/blockblastpc/releases/latest)

A bright, casual block puzzle game for Windows desktop, inspired by the popular mobile game *Block Blast*. Built with WPF and .NET 8.

## Download

Grab the latest Windows installer from the **[Releases page](https://github.com/sidewardone/blockblastpc/releases/latest)** — `BlockBlast-Setup.exe`. No .NET installation required, the app is fully self-contained.

## Table of contents

- [Gameplay](#gameplay)
- [Shapes](#shapes)
- [Scoring](#scoring)
- [Controls](#controls)
- [Localization](#localization)
- [Best score](#best-score)
- [Project structure](#project-structure)
- [Building and running from source](#building-and-running-from-source)
- [Publishing a self-contained executable](#publishing-a-self-contained-executable)
- [Building the Windows installer](#building-the-windows-installer)
- [License](#license)

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

| Action | Input |
|---|---|
| Place a shape | Drag from the tray and drop it on the board |
| Switch language | Click the **RU / EN** button (top-right corner) |
| Restart after Game Over | Click **Start again** |

## Localization

🇬🇧 English · 🇷🇺 Русский

The interface starts in the language matching your Windows system locale (Russian if your Windows UI language is Russian, English otherwise), and can be switched anytime with the language button in the top-right corner.

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
