# Darkless — Final Project (Checkpoint 1)

**Course:** COMP-5970-D01
**Engine:** Unity `6000.4.8f1` (Unity 6.4) · Universal Render Pipeline (URP) · new Input System
**Project folder:** [`Darkless/`](./Darkless) — a standard Unity project (`Assets/`, `Packages/`, `ProjectSettings/`).

---

## Game title

**Darkless**

## Game concept

Darkless is a 3D low-poly **third-person survival-horror** game built on one rule: **light is life, darkness is death.**

You're stranded at a woodland campsite after dark. Your car is right there — but you lost the keys somewhere out in the woods earlier, and something is hunting in the dark. The monster doesn't want to fight you; it wants your light *out*. It moves freely and lethally in total darkness, but the more lit the ground it stands on, the slower it gets — so light is both your shield and your weapon.

The intended loop: **by day** you gather what you need (fruit for stamina, wood for the fire) in relative safety; **by night** you keep your campfire alive, manage your light, and search the woods for the car keys. Several keys can be found, but **only one starts your car** — find the right one and drive away to win.

> Checkpoint 1 establishes the world and the core survival systems. The full night-search / monster / escape loop is in progress (see *Roadmap* below).

## Controls

| Action | Input |
|---|---|
| Move | **W A S D** |
| Look | **Mouse** |
| Sprint | **Left Shift** |
| Jump | **Space** |
| Interact / harvest (e.g. apple trees) | **E** |
| Refuel the campfire *(temporary test key for this checkpoint)* | **F** |

## Current objective (Checkpoint 1)

Explore the dark, foggy forest and **stay alive in the light.** When night falls, don't get caught out in the darkness — a light meter drains when you're unlit and kills you when it empties. Keep the **campfire** fed so its safe radius doesn't shrink and go out, and harvest apple trees along the way. (The win condition — finding the car key at night and returning to the car — is planned for a later checkpoint.)

## What is working in Checkpoint 1

- **Explorable horror world** — the low-poly wilderness re-themed into a dark, dense, foggy forest with a color-graded horror look and a campsite/campfire.
- **Third-person player** — custom low-poly human character with the standard movement/camera controller (move, look, sprint, jump).
- **Day/night cycle with dynamic fog** — a looping cycle with **longer nights**; fog is thin and pale by day (so you can see) and thick and dark at night (so it closes in), blended automatically.
- **Layered ambient audio** tied to the cycle.
- **Navigation** — on-screen **mini-map** and **compass**.
- **Harvesting** — walk up to an apple tree, press **E**, play a pick animation, and an on-screen counter increments.
- **Core survival rule — the darkness meter** *(the heart of the game)* — you are safe in **daylight** or inside a **light source**; in the dark, a meter drains and, when empty, the screen fades to black, you die, and the scene restarts.
- **Campfire fuel system** — the campfire burns fuel over time; its **safe radius shrinks** as fuel drops, **goes out** (no safe zone) at zero, and **relights** when fed; the fire's glow scales with its fuel.

The project **runs without major errors** and is testable: open `Darkless/Assets/Scenes/SampleScene` in Unity 6000.4.8f1 and press **Play**.

## Roadmap (not yet implemented)

Flashlight (finite battery) · handheld torches (built from sticks, throwable) · a starting lantern network · a weighted backpack + stamina/food · the monster and its light-aware AI · night-only key search + the multi-key/"only one fits" car-escape win · a searchable-area boundary · a night-by-night difficulty ramp · a horror audio pass.

## How to open / run

1. Clone this repo and open the **`Darkless/`** folder as a project in **Unity 6000.4.8f1** (URP). Unity regenerates the `Library/` folder on first open.
2. Open the scene **`Assets/Scenes/SampleScene.unity`**.
3. Press **Play**. (To preview night quickly, select **DayNightManager** in the Hierarchy and drag **Time Of Day** toward ~0.6.)

## External assets & resources used so far

| Asset / resource | Use | Link |
|---|---|---|
| **Low Poly Environment — Nature (Free)** — Polytope Studio | Terrain, trees, grass, rocks, skybox, fruit-tree & log props | https://assetstore.unity.com/packages/3d/environments/low-poly-environment-nature-free-lowpoly-medieval-fantasy-series-187052 |
| **Starter Assets: Third Person Controller (URP)** — Unity (+ **Cinemachine**) | Player controller + follow camera | https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-urp-196526 |
| **FREE Low Poly Human - RPG Character** — Blink | The player character model + its animation set | https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/free-low-poly-human-rpg-character-219979 |
| **Mixamo** — Adobe | Harvest / interaction animations | https://www.mixamo.com |
| **Audio — "Atmospheric documentary"** (Pixabay) | Constant ambient bed | https://pixabay.com/music/meditationspiritual-atmospheric-documentary-509386/ |
| **Audio — "Nature forest daytime"** (Pixabay) | Day audio layer | https://pixabay.com/sound-effects/nature-forest-daytime-446356/ |
| **Audio — "Night forest, frogs & crickets"** (Pixabay) | Night audio layer | https://pixabay.com/sound-effects/nature-night-forest-with-frogs-and-crickets-for-sleep-451153/ |

All audio is free under the Pixabay Content License; asset-store packages are used under their respective Unity Asset Store licenses.
