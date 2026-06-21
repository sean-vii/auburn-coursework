# Assignment V — Sky Roller: Endless Survival

An endless survival expansion of the Module 5 **Sky Roller 3D** game, built in
**Unity 6 (6000.4.8f1)** using the Universal Render Pipeline.

The class version let the player steer a rolling ball through a fixed level with a
follow camera and a few hazards. This version turns it into an **endless procedural
runner**: platform sections are generated ahead of the player and recycled behind
them, new hazard types disrupt the player, and a survival score tracks how long you
last until you fall off and restart.

## How to open and play

1. Clone or download this repository.
2. In **Unity Hub → Add → Add project from disk**, select this `Assignment V` folder.
3. Open with Unity **6000.4.8f1** (or the closest 6000.4.x — Hub may offer a safe
   minor-patch upgrade).
4. Open **`Assets/Scenes/EndlessMode.unity`** and press **Play**.
   (`EndlessMode` is also set as the first scene in Build Settings, so a built player
   starts there directly.)

> Only `Assets/`, `Packages/`, and `ProjectSettings/` are tracked. Unity regenerates
> `Library/`, `Temp/`, and `Logs/` automatically on first open.

## Controls

| Action        | Key                 |
| ------------- | ------------------- |
| Steer left    | A / Left Arrow      |
| Steer right   | D / Right Arrow     |
| Restart (after losing) | R, or click the **Restart** button |

The ball rolls forward automatically — your job is to steer, dodge hazards, and stay
on the platforms.

## Required features and where they live

| Requirement | Implementation |
| ----------- | -------------- |
| Endless procedural platform generation | `PlatformGenerator.cs` streams sections ahead of the player and **destroys** sections that fall behind. |
| At least 4 platform prefabs | Four distinct section variants built procedurally: **Flat**, **Narrow**, **Split** (gap down the middle), and **Offset** lane. |
| At least 3 hazard types (besides falling off) | `Hazard.cs`: **Kill** (instant game over), **Slow** field, **Bumper** (knocks you sideways), and **Reverse** (inverts steering) — plus a bonus **Boost** pad. |
| Survival score | `GameManager.cs` blends distance travelled, time survived, and sections passed. |
| Score on screen (UI text) | `ScoreUI.cs` draws a live uGUI HUD readout. |
| Lose condition | Falling below the platforms or hitting a Kill hazard triggers game over (`PlayerController.cs` / `GameManager.cs`). |
| Restart option | Press **R** or click the **Restart** button on the game-over panel. |
| Complete endless loop | Movement → camera follow → generation → hazards → score → lose → restart, all wired together. |

## How it is built

The endless mode is **constructed entirely in code** at runtime so the project is
self-contained and reproducible. `EndlessMode.unity` contains a single
`GameController` object running `GameBootstrap.cs`, which spawns the player, the
follow camera (reusing the class's `CameraFollow`), lighting, the platform generator,
the game manager, and the UI when you press Play.

```
Assets/
  Scenes/
    EndlessMode.unity     The endless survival game (open this)
    GameScene.unity        Original Module 5 game (kept for reference)
    MainMenu.unity         Original menu (kept for reference)
  Scripts/
    Endless/
      GameBootstrap.cs     Builds the whole scene at runtime
      GameManager.cs       State, survival score, lose + restart
      PlayerController.cs   Rolling-ball movement + hazard effects
      PlatformGenerator.cs  Procedural spawn/recycle, 4 variants, hazard placement
      Hazard.cs            Kill / Slow / Bumper / Reverse / Boost behaviours
      ScoreUI.cs           Runtime HUD + game-over panel + restart button
    CameraFollow.cs        Reused from the class version
    (PlayerMovement.cs, DeathZone.cs, SpeedBoostZone.cs, MainMenu.cs — original class scripts)
```

Base art/template assets are from the Module 5 Sky Roller skeleton project.
