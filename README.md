# Snake Game - C# Windows Forms

This is a complete C# WinForms Snake game for Visual Studio.

## How to open source project

1. Extract `SnakeGameWinForms.zip`.
2. Open `SnakeGameWinForms.csproj` in Visual Studio.
3. Press `F5` to run.

## How to run the exe

1. Extract `SnakeGameWinForms-Release.zip`.
2. Open the extracted folder.
3. Run `SnakeGameWinForms.exe`.

## Features

- Top header branding with stylish NIAZI logo and Developed By Ali Zain Khan credit
- Proper fixed game boundary: the snake and food stay inside the board
- Right-side data panel for score, level, speed, food timer, combo, best score, last score, and games played
- Red food stays in one place for 6 seconds, then relocates and resets combo if missed
- First speed starts slow, then speed increases over time and as level rises
- Five food combo unlocks the next food as a +50 bonus; after bonus, scoring returns to normal
- Backend game engine separated into `GameEngine.cs`
- Sound effects for start, food, pause, level up, and game over
- Saved high score, last score, games played, and last played time
- 3.5 second loading screen showing Developed By Ali Zain Khan

## Saved data location

The game stores player data in:

`%AppData%\SnakeGameWinForms\save-data.json`

## Controls

- WASD: move snake
- Space: start, pause, resume, or restart
- M: mute or unmute sound
- Start / Restart button: restart game
- Pause button: pause or resume
- Sound button: mute or unmute

## Project structure

- `Program.cs`: application startup
- `MainForm.cs`: frontend/UI, drawing, buttons, keyboard handling
- `GameEngine.cs`: backend/game logic, scoring, movement, collisions, food timer, combo bonus, food placement
- `SaveDataStore.cs`: stores high score and player stats as JSON
- `SoundService.cs`: plays simple sound effects
- `SnakeGameWinForms.csproj`: Visual Studio project file

Target framework: `.NET 8 Windows` with Windows Forms enabled.
