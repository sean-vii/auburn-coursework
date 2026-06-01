

https://github.com/user-attachments/assets/4f285db1-b167-46c3-9119-d03e2c5d3d80



# Assignment II

A 2D Unity platformer where the player has to find a key, unlock a door, and reach it without dying to enemies or hazards.

## Controls

- **Move:** `A` / `D` or `←` / `→`
- **Jump:** `Space`, `W`, or `↑`

## How to play

1. Open the project folder in Unity Hub with **Unity 6000.4.8f1** (Unity 6).
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press **Play**.
4. Pick up the key, then walk into the (now open) door to win. Touch an enemy or a spike to die. A modal will appear with a **Play Again** button that reloads the scene.

## Gameplay pieces

- **Player** (`PlayerMovement.cs`) — physics-based left/right movement and jump, with a four-state sprite animation (idle / 3-frame walk / jump). Grounding is detected from collision normals so any surface below the player counts as ground, and horizontal input is suppressed when airborne and pressed into a wall to prevent friction-stick.
- **Enemy** (`EnemyMovement.cs`, `Enemy.prefab`) — randomly picks left / right / stay every few seconds, walks a random distance, and refuses to step off ledges (raycasts down past the target foot position; flips direction if there's no ground, stays still if both sides are edges).
- **Key** (`Key.cs`) — trigger pickup that fires a static `OnCollected` event and resets on scene reload.
- **Door** (`Door.cs`) — swaps between closed/open sprites based on key state. Closed is a solid collider; open is a trigger that ends the run as a win. Also re-checks overlap when it opens, so grabbing the key while already standing in the doorway still triggers the win.
- **Spike hazard** (`SpikeHazard.cs`) — drop-in component that kills the player on contact whether the collider is a trigger or solid.
- **Game manager** (`GameManager.cs`) — singleton that builds a Game Over / You Win modal at runtime (no prefab needed), pauses time, and reloads the active scene on **Play Again**. Uses the bundled Press Start 2P pixel font.

## Project layout

```
Assets/
  Scripts/          gameplay code listed above
  Scenes/           SampleScene.unity (the playable level)
  Sprites/          1-bit tile atlas (tile_NNNN.png) + materials
  Tiles/            tile assets and palette used by the Tilemap
  Fonts/            PressStart2P-Regular.ttf
  Settings/         URP renderer + quality assets
  Enemy.prefab      configured enemy prefab
Packages/           manifest.json + packages-lock.json
ProjectSettings/    Unity project settings (incl. ProjectVersion.txt)
```

`Library/`, `Temp/`, `Logs/`, and `UserSettings/` are intentionally excluded — Unity regenerates them on first open.
