# Vertigo Games Wheel Of Fortune

Made by Kerem Ihsan Mercan, for Vertigo Games Case Study.

A Unity-based Wheel Of Fortune gameplay prototype with animated UI, revive flow, progressive zone difficulty, and persistent player rewards.

## Overview
This project demonstrates a polished gameplay loop:
- Spin the wheel and collect rewards.
- Progress through zones with Bronze, Silver, and Gold wheel states.
- Handle bomb outcomes with a revive popup.
- Persist collected totals between sessions.

## Features
- Responsive wheel spin flow with DOTween animations.
- Zone progression with UI track updates and wheel type transitions.
- Revive system with scalable pricing and animated popup.
- Inventory and main-menu resource counters with animated number updates.
- Centralized audio playback service.
- Automatic scene reference assignment via OnValidate across manager scripts.

## Tech Stack
- Unity 2021.3.45f2
- C#
- TextMeshPro
- DOTween

## Project Structure
- Assets/Scripts
  - WheelManager: wheel generation, spin lifecycle, title/footer/button animation wiring.
  - GameManager: revive flow, popup transitions, bomb handling.
  - ZoneManager: zone progression and indicator reuse.
  - InventoryManager: session reward accumulation and reset.
  - PersistenceManager: total resource persistence and main-menu updates.
  - UIManager: panel switching and session exit flow.
  - AudioManager: audio playback and singleton bootstrap.
  - IAudioService, IPlayerDataStore, PlayerPrefsDataStore: abstractions and implementations.
  - Editor/PlayerPrefsEditor: custom editor utility for persistent values.

## Getting Started
1. Install Unity Hub and Unity Editor 2021.3.45f2.
2. Open this folder in Unity Hub.
3. Open scene: Assets/Scenes/MainScene.unity.
4. Press Play in the Unity Editor.

## My Design Decisions
### Single Responsibility Principle
- WheelManager focuses on wheel setup/spin logic.
- GameManager handles revive and game-loop transitions.
- PersistenceManager owns persistent total updates.
- ZoneManager owns zone progression visuals and state.

### Open/Closed Principle
- New data-store backends can be added by implementing IPlayerDataStore without changing dependent gameplay systems.
- New audio implementations can be introduced through IAudioService.

### Liskov Substitution Principle
- Any class implementing IAudioService can replace AudioManager where audioService is used.
- Any class implementing IPlayerDataStore can replace PlayerPrefsDataStore where playerDataStore is used.

### Interface Segregation Principle
- IAudioService and IPlayerDataStore are intentionally small and focused.
- Systems depend only on methods they actually use.

### Dependency Inversion Principle
- Managers consume abstractions (IAudioService, IPlayerDataStore), not concrete storage/audio classes.
- ResolveDependencies creates sensible defaults for runtime.
- Internal test seams (SetAudioServiceForTests, SetPlayerDataStoreForTests) support isolated testing.

### Core OOP Practices
- Encapsulation: serialized fields + private runtime state in manager components.
- Composition over inheritance: behavior built by connecting focused MonoBehaviours.
- Event-driven coordination: WheelManager.OnSpinComplete decouples wheel outcome from inventory/zone/game logic.

### How Object Pooling Is Applied
Object pooling is applied in ZoneManager for zone indicators:
- A fixed set of indicators is created once in InitializeZoneTrack.
- ReuseTopIndicator recycles the top item by moving it to the bottom and updating its visuals.
- This avoids repeated instantiate/destroy cycles during progression and reduces GC pressure.

Practical result:
- Smoother scrolling behavior.
- Lower runtime allocation spikes during long sessions.

### How the Editor Tool Is Used
A custom editor window is provided in Assets/Scripts/Editor/PlayerPrefsEditor.cs.

How to open:
1. In Unity menu bar, click Vertigo Case > PlayerPrefs Editor.

What it does:
- Displays current totals for Gold, Money, Melee, Armor, and Rifle.
- Allows editing and saving those values to PlayerPrefs.
- Provides Reset All with confirmation for quick state reset during testing.

Why it helps:
- Faster QA and balancing workflows.
- No need to manually modify PlayerPrefs through external tools.

## Gameplay Screenshots
### Main Menu
![Main Menu 16:9](ScreenShots_Gameplay/MainMenu_16.9.png)
![Main Menu 20:9](ScreenShots_Gameplay/MainMenu_20.9.png)
![Main Menu 4:3](ScreenShots_Gameplay/MainMenu_4.3.png)

### In-Game
![Game 16:9](ScreenShots_Gameplay/Game_16.9.png)
![Game 20:9](ScreenShots_Gameplay/Game_20.9.png)
![Game 4:3](ScreenShots_Gameplay/Game_4.3.png)

### Revive Panel
![Revive Panel 16:9](ScreenShots_Gameplay/RevivePanel_16.9.png)
![Revive Panel 20:9](ScreenShots_Gameplay/RevivePanel_20.9.png)
![Revive Panel 4:3](ScreenShots_Gameplay/RevivePanel_4.3.png)

### Gameplay Video
- ScreenShots_Gameplay/Gameplay.mp4

## Build Output
- Android build artifact currently present in root: WheelOfFortune.apk
