# Darkless

A 3D low-poly survival horror game made in Unity.

**Course:** COMP-5970-D01
**Engine:** Unity `6000.4.8f1` (Unity 6.4), URP, new Input System
**Project folder:** [`Darkless/`](./Darkless) — a normal Unity project (`Assets/`, `Packages/`, `ProjectSettings/`).

## What the game is

You're stuck at a campsite in the woods at night. Your car is right there, but you lost the keys
somewhere in the forest. Something is hunting you in the dark — but light slows it down. Gather what
you need by day, search for a key that fits by night, and drive away to escape before the nights get
too deadly.

The one rule: **light keeps you alive, the dark kills you.**

## How to win and lose

**Win:** find a car key (keys only show up at night), take it to the car, and press E to escape.
Several keys are hidden in the woods, but **only one actually starts your car** — and you won't know
which until you try it.

**Lose:**
- You stand in the dark too long (your light runs out).
- The monster touches you.
- You wander too far past the searchable area, out into the true dark.

## How to play

- Stay in light. In the dark, a meter drains and you die when it's empty. Daylight, the campfire,
  your flashlight, and a lit torch all count as light.
- Keep the **campfire** fed with **logs** so its safe area stays big and bright. A well-fed fire
  keeps the monster out of camp; let it burn down and the safe zone shrinks.
- Make a **torch** at your backpack by the fire (it costs a few sticks), then **equip it from your
  pack (Tab)**. A torch is a renewable light you carry to move through the dark — but it only lasts
  so long before it burns out and you have to make another.
- Your **flashlight** is a finite emergency light — save it for when the dark closes in.
- The **monster** moves fast in the dark and slows down in the light. Shine your flashlight at it to
  scare it off. It kills you if it touches you, and it gets bolder and attacks more often every night.
- At night, **glowing creatures** appear in the woods. Search them to look for keys (you find a key
  most of the time, but not always).
- Gather **fruit** (eat it for stamina) and **wood** during the day — **sticks** for torches,
  **logs** for the fire. Your backpack has a weight limit, so you can't carry everything at once.
- Watch your step for **bear traps**, and push through **bushes** slows you down.
- Open your **backpack and map** with **Tab** to see where you are and manage your gear.

## Controls

| Action | Key |
|---|---|
| Move | **W A S D** |
| Look | **Mouse** |
| Sprint | **Left Shift** |
| Jump | **Space** |
| Interact — pick up, feed the fire, make a torch, use the car | **E** (tap) |
| Search a creature | **Hold E** |
| Open/close backpack & map (equip a torch here) | **Tab** |
| Flashlight on/off | **F** |
| Flashlight burst (scare the monster) | **G** |

## What's in the game

- A title screen, and a dark, foggy forest with a campsite and a day/night cycle.
- A first-person player and camera.
- A monster that hunts you in the dark, slows in the light, and gets more aggressive each night.
- A light meter — stay lit or die — with the screen closing in as you run out of light.
- A **flashlight** with a limited battery, and craftable **torches** you build from sticks and equip
  from your pack (each one burns out over time).
- A **campfire** you feed with logs to keep a safe zone that grows and shrinks with the fire.
- A **backpack with a weight limit** (carry food, sticks, and logs), plus **stamina** — sprinting
  uses it up, eating fruit refills it.
- Night-only **search creatures** that can give you car keys, with **several keys but only one that
  fits** (randomized each run).
- **Bear traps** and slow-you-down **bushes** as hazards, and a **searchable-area boundary** — go too
  far and the dark takes you.
- A handheld **map**, an on-screen **objective and night counter**, and inner-voice subtitles.
- Win screen ("You Escaped") and death screen ("You Died"), both with a restart button.
- Background music, forest ambience, monster sounds, and procedural interaction/UI sound effects.

## How to open and run

1. Clone this repo and open the **`Darkless/`** folder in **Unity 6000.4.8f1** (URP). Unity rebuilds
   the `Library/` folder the first time it opens.
2. Open the scene **`Assets/Scenes/SampleScene.unity`**.
3. Press **Play**, then hit **PLAY** on the title screen. It starts in the daytime; night comes after
   about a minute. (To start closer to night, pick **DayNightManager** in the Hierarchy and set
   **Time Of Day** to about `0.6`.)

## Assets used

| Asset / resource | Used for | Link |
|---|---|---|
| **Low Poly Environment — Nature (Free)** — Polytope Studio | Terrain, trees, grass, rocks, sky, props | https://assetstore.unity.com/packages/3d/environments/low-poly-environment-nature-free-lowpoly-medieval-fantasy-series-187052 |
| **Starter Assets: Third Person Controller (URP)** — Unity (+ Cinemachine) | First-person player movement, camera, and player character | https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-urp-196526 |
| **Mimic Prototype** | The monster | https://assetstore.unity.com/packages/3d/characters/creatures/mimic-prototype-245997 |
| **Full Opaque Fire** | Stylized fire effect for the campfire and torches | Unity Asset Store (free) |
| **Survival low-poly prop pack** | Backpack (crafting station), bear traps, torch & bonfire props | Unity Asset Store (free, imported as FBX) |
| **Clash Grotesk** — Indian Type Foundry | UI font | https://www.fontshare.com/fonts/clash-grotesk |
| **Mixamo** — Adobe | Pick-up / search animations | https://www.mixamo.com |
| Low-poly **car**, **key**, and **creature** models | Escape car, key item, night search creatures | Free low-poly models (imported as FBX) |
| **Pixabay** audio — "Atmospheric documentary", "Nature forest daytime", "Night forest, frogs & crickets", plus monster / death / bear-trap sound effects | Music, ambience, and sound effects | https://pixabay.com |

The interaction and UI sound effects (pickups, crafting, button clicks) are generated in-engine, so
they don't use any external audio files. All Pixabay audio is free under the Pixabay Content License,
and the Asset Store packages are used under their Unity Asset Store licenses.
