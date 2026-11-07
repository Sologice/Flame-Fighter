# Flame Fighter: Game Plan

## Vision and pillars

Flame Fighter is a first-person, low-poly survival horror game where the fire is the monster: you wake in the middle of a burning forest with only a hose, and you win by putting out every flame before the forest is lost.

- **Fire as the antagonist.** No enemies. Dread comes from flames spreading, glowing through smoke, and cutting off your route.
- **One verb, done well.** Aim, spray, extinguish. The hose must feel great before anything else is added.
- **Atmosphere over spectacle.** Dim light, thick smoke, muffled sound, a red glow on the horizon.
- **Readable at a glance.** Low-poly shapes and strong color (orange fire, dark trees, blue water) keep the screen legible in the smoke.
- **Short and complete.** One map, two stages, a finished game rather than a sprawling one.

## Game structure: two stages, one map

Stage 1 is a short tutorial with unlimited water and no damage; Stage 2 is the real game on the same forest with a limited tank, heat danger and a hybrid spreading fire.

**Stage 1: Tutorial (about 5 to 8 minutes)**

- Unlimited water, no player damage, so you can learn safely.
- Small authored fires teach one idea at a time: spray a flame, hold the stream to drain it, hit the base of a fire, stop grass fire from reaching trees.
- Light on-screen prompts, no heavy text. The tone is already eerie, but nothing can hurt you.
- Ends with a scripted beat (a flare-up, a sudden wind, the sky turning red) that signals the real fire has begun.

**Stage 2: Main game (about 15 to 25 minutes)**

- Same map, but now with a limited water tank refilled at water sources such as a pond or a hydrant.
- Hybrid fire: authored fire zones plus real spreading between trees and grass, with a few scripted set pieces such as a blaze crossing a path.
- Win by extinguishing everything. Lose if too much forest burns down (a hidden burn-percentage meter): the more forest burns, the thicker the smoke, until the player coughs and passes out.

**Damage model:** there is no visible health bar. Burn percentage acts as a hidden health bar and shows up physically as smoke that hurts the player (see Core systems).

## Core systems

Three systems carry the game: the fire, the hose and the player controller. Build them in that order of risk, fire first.

**Fire system (hybrid)**

- Each flammable object (tree, bush, grass patch, log) has a fuel value, a burn state (unlit, burning, extinguished, burned out) and a fire health that water drains.
- Fire spreads by checking nearby flammables every 0.5 to 1 second using a cheap overlap check, with a spread chance that rises with fuel and wind.
- Authored fires are placed by hand as seed points. Scripted set pieces trigger by zone or timer.
- Extinguished objects can reignite only if flames next to them are still alive, which makes you clear a cluster rather than spot-spray.
- Visuals: pooled particle systems (flames, sparks, smoke) with a fire intensity that scales size and light. Limit real-time lights to the closest few fires.

**Hose**

- Hold-to-spray stream from a particle system, with a raycast or spherecast to find what it hits.
- Hits on a fire drain its health, and the base of the fire drains faster than the top.
- Stage 1 water is unlimited. Stage 2 adds a tank value that drains while spraying, with a refill at water sources.
- Feedback: spray sound loop, hiss and steam where water meets fire, slight camera kick, a tank gauge on the hose or a diegetic UI.

**Player**

- Standard first-person controller: walk, sprint (limited stamina optional), look, spray, interact for refilling.
- No health in Stage 1. In Stage 2 there is no visible health bar; a hidden health value drains as smoke thickens with the burn percentage. Stages: light smoke, then coughing with blurred vision and camera sway, then blackout.

**Win and lose**

- Win: zero active fires remain on the map.
- Lose: the player passes out from smoke. Smoke density follows burn percentage, so the burning forest is the health bar, with no on-screen gauge. Tune it so near-losses feel tense.

## Horror atmosphere

The horror is atmosphere plus limited sight and sound, with no enemies, so every effect should make the fire feel bigger than you.

- **Limited sight:** dense volumetric-style fog and smoke (cheap fog plus smoke particles), a dark sky with an orange glow, a short camera view distance, and flickering firelight that casts moving shadows.
- **Limited sound:** muffled ambient audio, a low roar that grows near large fires, crackling that gets louder with proximity, creaking and falling trees, distant radio static, and your own breathing.
- **Pacing:** quiet stretches broken by flare-ups, collapsing trees and sudden wind, so tension rises and falls.
- **Look:** low-poly with flat or lightly shaded materials, a warm and cold palette, and a mild pixelation or dither post-process for the indie horror feel.
- **Feedback without enemies:** the thickening smoke, the coughing and the sound of the forest dying do the work of a threat.

## Unity technical architecture

Keep the project simple: URP, a small set of scripts, and the fire manager as the one system that needs real care for performance.

- **Render pipeline:** Universal Render Pipeline (URP) with a low-poly material set, fog, bloom and a light vignette.
- **Input:** the new Input System, with one action map for movement, look, spray and interact.
- **Core scripts:** `FireManager` (ticks all flammables and tracks burn percentage), `Flammable` (fuel, state, spread), `FireVisual` (particles and light scaling), `HoseController` (spray, raycast, tank), `WaterSource` (refill), `GameStateManager` (stage flow, win, lose), `AudioManager`.
- **Performance:** pool all fire and smoke particles, cap active real-time lights, tick spread on a timer rather than every frame, and use layers so the hose raycast only tests flammables and terrain.
- **Scale target:** a map of about 300 to 600 flammable objects, tested on mid-range hardware before adding detail.
- **Data:** use ScriptableObjects for tunable values such as spread rate, fuel and water drain, so balancing needs no code changes.
- **Scenes:** one forest scene, with the stage change handled by game state rather than a scene load.

## Art and audio pipeline

A solo dev should lean on a small, consistent kit of low-poly assets and a few strong sounds rather than many unique models.

- **Models:** 4 to 6 tree variants, bushes, grass patches, rocks, logs, a hose and nozzle, a pond, a hydrant or tank, and a few props such as a cabin, signs and a truck wreck. Make them in Blender or use a licensed low-poly pack, and keep a consistent style and palette.
- **Terrain:** Unity Terrain or a hand-built low-poly mesh, with paths that guide you through the forest.
- **VFX:** flames, smoke, embers, water spray and steam, built from simple particle textures.
- **Audio:** fire loops at three intensities, spray loop, steam hiss, wind, creaking trees, footsteps on dirt and leaves, a low drone, and a few stingers. Free or licensed sound libraries cover most of it.
- **UI:** minimal. A water gauge and short tutorial prompts, styled to fit the low-poly look.

## Milestone roadmap

About 10 to 12 weeks for a solo developer, each milestone ending in something playable.

1. **Weeks 1 to 2, hose and fire prototype.** Grey-box scene, first-person controller, spray particles, one burning object that drains when hit. Gate: spraying a fire feels good.
2. **Weeks 3 to 4, fire spreading.** Flammable component, spread logic, burn percentage, win and lose checks on a test patch of trees. Gate: spread is readable and winnable.
3. **Weeks 5 to 6, map and Stage 1.** Block out the full forest, place authored fires, build the tutorial beats and prompts. Gate: a new player finishes Stage 1 without help.
4. **Weeks 7 to 8, Stage 2 systems.** Limited tank, water sources, set pieces, difficulty tuning. Gate: Stage 2 is tense but fair.
5. **Weeks 9 to 10, art and atmosphere pass.** Replace grey-box with low-poly assets, lighting, fog, post-processing, full audio. Gate: it reads as a horror game.
6. **Weeks 11 to 12, polish and release.** Performance pass, menus, pause, settings, bug fixing, playtests, build and store page.

If time runs short, cut set pieces and extra props first, never the hose feel or the fire spread.

## Risks and open questions

The biggest risks are fire performance and a hose that feels flat; the open questions below are the next round of grilling.

**Risks**

- **Fire performance:** hundreds of burning objects with particles and lights can tank frame rate. Mitigation: pooling, light caps, timed ticks, and an early stress test.
- **Spread balance:** fire that spreads too fast is unwinnable, too slow is boring. Mitigation: ScriptableObject tuning and frequent playtests.
- **Hose feel:** a weak spray ruins the whole game. Mitigation: prototype it first and iterate on sound, particles and camera kick.
- **Scope creep:** extra mechanics and assets. Mitigation: the cut order in the roadmap.

**Open questions to answer next**

- [ ] Is there any story, or is it purely atmosphere and gameplay?
- [ ] Should smoke depend only on total burn percentage, or also get thicker near individual large fires?
- [ ] Is wind a factor in spread, and does it change during the game?
- [ ] How big is the map: a compact clearing or a walkable forest of several minutes across?
- [ ] Target platform: PC only, or also consoles and mobile?
- [ ] Will you make the assets yourself or use asset packs?
- [ ] Controls: keyboard and mouse only, or controller support too?
- [ ] Is there a title screen, a checkpoint system or an ending cutscene?
