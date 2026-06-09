

https://github.com/user-attachments/assets/75adce86-a3e1-4afc-9288-2147f9421254



# Assignment III — Meteor Rush

A 2D arcade shooter built in **Unity 6 (6000.4.8f1)** using the Universal Render Pipeline (2D).

Base game adapted from the course's Module 3 project, extended with meteor hazards, a
player health system, scoring, and a full game-over / restart loop.

## How to open

1. Clone or download this repository.
2. Open **Unity Hub → Add → Add project from disk** and select this `Assignment III` folder.
3. Use Unity **6000.4.8f1** (or the closest 6000.4.x you have installed — Hub may prompt
   to upgrade, which is safe for a minor patch difference).
4. Open `Assets/Scenes/SampleScene.unity` and press **Play**.

> Only `Assets/`, `Packages/`, and `ProjectSettings/` are tracked. Unity regenerates the
> `Library/`, `Temp/`, and `Logs/` folders automatically on first open.

## Controls

| Action | Key               |
| ------ | ----------------- |
| Move   | WASD / Arrow Keys |
| Shoot  | Left Mouse Button |

## Features

- Player movement and projectile shooting
- Enemy spawning, wave movement, and enemy fire
- **Meteor hazards** that spawn from the lower half of the screen and home toward the
  player — a meteor strike is an instant game over
- **Player health** (3 hits) shown as ship icons; each enemy-bullet hit removes one icon
- **Score** that increases as enemies are destroyed, shown as on-screen UI text
- Explosion sound when an enemy is destroyed and when the player loses
- Game-over banner with automatic restart

## Project structure

```
Assets/
  Audio/      Sound effects
  Prefabs/    Bullet, Enemy, EnemyBullet, Meteor
  Scenes/     SampleScene (the game)
  Scripts/    Gameplay scripts (PlayerController, Enemy, GameManager, Meteor, ...)
  Sprites/    Player, enemy, projectile, meteor, and background art
```

Sprites and sounds are from [Kenney](https://kenney.nl/) (Space Shooter Extension, Sci-Fi Sounds).
