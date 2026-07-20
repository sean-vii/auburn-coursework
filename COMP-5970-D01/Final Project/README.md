# Darkless

A 3D low-poly survival horror game made in Unity.

**Course:** COMP-5970-D01
**Engine:** Unity `6000.4.8f1` (Unity 6.4), URP, new Input System
**Project folder:** [`Darkless/`](./Darkless) — a normal Unity project (`Assets/`, `Packages/`, `ProjectSettings/`).

## What the game is

You're stuck at a campsite in the woods at night. Your car is right there, but you lost the keys
somewhere in the forest. Something is hunting you in the dark — but light slows it down. Stay in
the light, find the car key, and drive away to escape.

The one rule: **light keeps you alive, the dark kills you.**

## How to win and lose

**Win:** find a car key (keys only show up at night), take it to the car, and press E to escape.
Only one key starts the car.

**Lose:**
- You stand in the dark too long (your light runs out).
- The monster touches you.

## How to play

- Stay in light. In the dark, a meter drains and you die when it's empty. Daylight, the campfire,
  and your flashlight all count as light.
- Keep the **campfire** fed with wood so its safe area stays lit.
- The **monster** moves fast in the dark and slows down in the light. Shine your flashlight at it
  to scare it off. It kills you if it touches you.
- At night, **glowing creatures** appear in the woods. Search them to look for keys (you find a
  key most of the time, but not always).
- Gather **fruit** (for stamina) and **wood** (for the fire) during the day. Your backpack has a
  weight limit, so you can't carry everything at once.

## Controls

| Action | Key |
|---|---|
| Move | **W A S D** |
| Look | **Mouse** |
| Sprint | **Left Shift** |
| Jump | **Space** |
| Interact — pick up, feed the fire, use the car | **E** (tap) |
| Search a creature | **Hold E** |
| Flashlight on/off | **F** |
| Flashlight burst (scare the monster) | **G** |
| Open/close backpack | **Tab** |

## What's in the game

- A dark, foggy forest with a campsite and a day/night cycle.
- A first-person player and camera.
- A monster that hunts you in the dark and slows in the light.
- A light meter — stay lit or die.
- A flashlight with a limited battery.
- A backpack with a weight limit (carry food and wood).
- Stamina — sprinting uses it up, eating food refills it.
- A campfire you feed with wood to keep a safe zone.
- Night-only search creatures that can give you car keys.
- Win screen ("You Escaped") and death screen ("You Died"), both with a restart button.
- Background music, forest ambience, and monster sounds.

## How to open and run

1. Clone this repo and open the **`Darkless/`** folder in **Unity 6000.4.8f1** (URP). Unity rebuilds
   the `Library/` folder the first time it opens.
2. Open the scene **`Assets/Scenes/SampleScene.unity`**.
3. Press **Play**. It starts in the daytime; night comes after about 7 seconds. (To start at night,
   pick **DayNightManager** in the Hierarchy and set **Time Of Day** to about `0.6`.)

## Assets used

| Asset / resource | Used for | Link |
|---|---|---|
| **Low Poly Environment — Nature (Free)** — Polytope Studio | Terrain, trees, grass, rocks, sky, props | https://assetstore.unity.com/packages/3d/environments/low-poly-environment-nature-free-lowpoly-medieval-fantasy-series-187052 |
| **Starter Assets: Third Person Controller (URP)** — Unity (+ Cinemachine) | Player movement + camera | https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-urp-196526 |
| **FREE Low Poly Human - RPG Character** — Blink | Player character model + animations | https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/free-low-poly-human-rpg-character-219979 |
| **Mimic Prototype** | The monster | https://assetstore.unity.com/packages/3d/characters/creatures/mimic-prototype-245997 |
| **Mixamo** — Adobe | Pick-up / search animations | https://www.mixamo.com |
| Low-poly **car**, **key**, and **creature** models | Escape car, key item, night search creatures | Free low-poly models (imported as FBX) |
| **Pixabay** audio — "Atmospheric documentary", "Nature forest daytime", "Night forest, frogs & crickets", plus monster/death sound effects | Music, ambience, and sound effects | https://pixabay.com |

All Pixabay audio is free under the Pixabay Content License. Asset Store packages are used under
their Unity Asset Store licenses.
