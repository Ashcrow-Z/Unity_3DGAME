# The SIP Lab Lockdown — Final Game Design Report

**Module**: CPT306 — Game Development  
**Project**: Coursework 2 — Original 3D Game  
**Engine**: Unity 2022.3.62f3 (Built-in Render Pipeline)

---

## Abstract

*The SIP Lab Lockdown* is a single-player, first-person 3D escape-room game in which the player wakes up trapped inside a research laboratory after a security lockdown and must solve three sequential puzzles to escape before the auxiliary power runs out. The project was developed in Unity 2022.3 using the Built-in Render Pipeline and deliberately constrained to a "no external assets" rule: every mesh is built from Unity primitives, every material is generated procedurally with the Standard shader, and every sound effect is synthesised in code at runtime through additive sine, square and filtered-noise oscillators with ADSR envelopes. The gameplay loop chains three escalating mechanics — physics-driven crate manipulation to recover an energy core, a 4-digit 3D keypad with diegetic feedback, and a keycard-gated blast door — bound together by a finite state machine that gates lighting, audio and interactivity. The report describes the design rationale, the supporting software architecture (interface-based interaction, raycast targeting, layered audio mixing, persistent volume settings), the responsibilities of each team member, and a preliminary observational analysis of player experience collected from informal play-testing sessions. Strengths, weaknesses and ethical considerations are then critically discussed, followed by a roadmap of future work that includes additional puzzle layers, accessibility options and a procedurally generated multi-room mode.

**Word count**: ~230

## Keywords

Escape Room; First-Person; Procedural Audio; Puzzle Design; Unity 3D

---

## 1. Introduction

### 1.1 Purpose and Goals

The purpose of this project was to design and implement a complete, polished, single-room escape game that demonstrates competence in the core pillars of 3D game development taught throughout the module: physics-based player movement, raycast interaction, finite-state game flow, diegetic UI, lighting design and runtime audio. A secondary, self-imposed goal was to ship the entire experience without importing any external art or audio asset, in order to prove that a small team can deliver a coherent atmosphere using only Unity primitives, hand-written shaders parameters and procedural sound synthesis.

### 1.2 Game Premise

The player assumes the role of a junior researcher working late inside the SIP Laboratory when an unspecified security incident triggers a full lockdown. Emergency power cuts the main lighting, leaving only red alarm lamps in the corners of the room. A locked blast door blocks the only exit, and a soft mechanical hum behind the wall suggests that the auxiliary generator is still salvageable. The player has no inventory, no weapons, no HUD beyond a thin reticle, and must therefore rely entirely on observation and physical experimentation with objects in the room.

### 1.3 Unique Hook

Three design decisions distinguish *The SIP Lab Lockdown* from typical student escape games:

1. **Causal puzzle progression** — every puzzle physically and narratively unlocks the next: only after power is restored does the server rack animate open and the keypad illuminate, and only after the safe yields the keycard can the blast door react to the terminal. There is no "hidden flag" gating; the player can verify the chain visually at every step.
2. **Kinetic binary display as diegetic hint** — rather than writing the password on a sign, the access code is encoded in a 4 × 4 array of wall-mounted server drawers. When power returns, drawers whose bit is `1` physically extrude from the wall and light a green LED; drawers whose bit is `0` stay flush and dark. Combined with a separately-placed codebook paper that provides the 4-bit weight legend, this turns Puzzle 2 into a genuine observation-and-reasoning task rather than a memory read. The keypad itself is also diegetic — buttons are world-space cubes confirmed by an in-world `E` key — so no 2D pop-up windows interrupt immersion.
3. **Procedural audio aesthetic** — instead of importing free-licensed sound libraries, every cue (footsteps, crate impacts, keypad beeps, success chord, error buzz, rack-activation rumble, victory fanfare) is generated at runtime from elementary waveforms. This unifies the soundscape and avoids the "asset-pack collage" feel common in student work.

### 1.4 Related Work

The project draws inspiration from three traditions. *The Room* series (Fireproof Games) demonstrates the value of single-location, mechanism-driven puzzle design. *Portal* (Valve, 2007) provides the canonical reference for physics-driven first-person puzzling and diegetic, environmental storytelling. *Keep Talking and Nobody Explodes* (Steel Crate Games, 2015) inspired the deliberate friction of the manually-confirmed 4-digit keypad: forcing the player to actively press an `Enter` key — rather than auto-validating on the fourth digit — preserves the satisfaction of a deliberate input. Compared with these references, our scope is intentionally smaller (one room, ten minutes of play) but the design grammar is the same.

---

## 2. Game Design

### 2.1 Game Concept

*The SIP Lab Lockdown* is a contained, linear escape-room experience built around three causally linked puzzles played out inside a single 10 × 4 × 10-metre laboratory. The player has approximately ten minutes of intended play time and a single goal — leave the room — but must discover the three sub-goals through observation. There are no dialogue, no NPCs, no respawn, no fail state; the only feedback channels are lighting, sound and the in-world keypad display.

### 2.2 Game World

The play space is a sealed concrete laboratory with steel-grey walls and a low ceiling. Four corner-mounted red point lights provide the initial alarm lighting; one disabled white directional light and a softer white fill light are switched on the moment power is restored, signalling progress through colour temperature alone. The room contains five distinct landmarks:

- **South-west corner — Crate Stack**: five 1 × 1 × 1 m crates with `Rigidbody` components stacked above the hidden energy core.
- **East wall — Generator Terminal**: a grey console with a red receptacle for the energy core.
- **West wall — 3 × 4 Keypad** and **East wall — Wall Safe**: the two halves of the second puzzle, deliberately separated so the player must traverse the room while entering the code.
- **South wall — Binary Server Rack**: a 4 × 4 array of wall-mounted server drawers that visually encodes the access code in binary once power is restored.
- **East half of the room — Codebook Desk**: a small lit desk carrying a maintenance log that provides the binary-weight legend needed to decode the rack.
- **North wall — Blast Door + Door Terminal**: a two-leaf sliding door with a card-reader terminal.

```
  ┌──────── NORTH WALL ────────┐
  │      [BLAST DOOR + TERM]   │
  │                            │
W │  [KEYPAD]         [SAFE]   │ E
A │                            │ A
L │              [CODEBOOK]    │ L
L │                            │ L
  │  [CRATES]    [GENERATOR]   │
  │     (Player Spawn)         │
  │                            │
  ├── [BINARY SERVER RACK] ────┤
  └──────── SOUTH WALL ────────┘
```

*Figure 1. Top-down schematic of the laboratory layout. The player spawns in the south-centre facing north; the server rack is embedded behind the spawn so the player only notices its reveal after restoring power, creating a deliberate "turn-around" moment.*

### 2.3 Game Interactions

All interactive objects are placed on the dedicated `Interactable` Layer. The player's camera casts a 3-metre `Physics.Raycast` filtered by that LayerMask every frame; whenever a `Collider` carrying a script that implements the `IInteractable` interface is hit, a contextual prompt appears at the bottom of the HUD ("Press E to pick up Energy Core", etc.). Pressing `E` or the left mouse button calls `Interact(playerInventory)` on that script. This single contract handles every puzzle interaction in the game and is summarised below.

```csharp
public interface IInteractable
{
    string GetPrompt();
    bool   IsInteractable();
    void   Interact(PlayerInventory inventory);
}
```

### 2.4 Gameplay Mechanisms

The three puzzles map onto three escalating interaction grammars:

1. **Physics manipulation (Puzzle 1)** — the energy core is hidden under the crate stack. The player must walk into the crates to topple them, exploiting Unity's PhysX to produce believable rolling and stacking. The core is then carried to the generator and inserted by interaction. A `LightingController` listens for the `OnPowerRestored` event and lerps every red light's intensity to zero while ramping the white directional light from 0 to 1.2 over 1.5 seconds, producing a smooth atmospheric reveal.
2. **Visual decoding + symbolic input (Puzzle 2)** — a two-stage reasoning puzzle. First, the moment power is restored, a 4 × 4 wall-mounted server rack on the south wall animates itself open: drawers whose bit value is `1` extrude from the wall and light a green LED, row-by-row with a 0.4 s beat, while `0`-drawers stay dormant. Each of the four rows therefore encodes one decimal digit in 4-bit binary (MSB left). A paper maintenance log lying on a lit desk on the east side of the room supplies the weight table (`[■ □ □ □] = 8`, `[□ ■ □ □] = 4`, `[□ □ ■ □] = 2`, `[□ □ □ ■] = 1`); the player sums the lit drawers in each row to recover the code `7 2 9 4`. Second, the 3D keypad (a 4 × 3 grid of `KeypadButton` components) is used to enter the digits; each button is itself an `IInteractable` whose `Interact()` method appends a digit, clears the buffer (`C`) or confirms the entry (`E`). The puzzle is gated on `GameStateManager.IsPowerOn`, so both the rack and the keypad are electrically dead during Puzzle 1. On a correct match the safe door rotates 90° around a hinge pivot and a keycard becomes interactable; on a wrong match the display flashes `ERR` and clears.
3. **Conditional unlock (Puzzle 3)** — the blast door terminal checks two flags simultaneously (`IsPowerOn && HasKeycard`). When both are true the two door leaves slide outward over 2 seconds, a hidden `BoxCollider` `Trigger` becomes active 1 m beyond the threshold, and walking through it raises the `OnVictory` event which freezes time, displays the elapsed timer and shows the victory canvas.

### 2.5 Underlying Theory

The puzzle progression follows the **Mechanic / Dynamic / Aesthetic** (MDA) framework: the *mechanics* are simple raycast interactions and physics responses; the *dynamics* emerge from the player's discovery that pushing crates can reveal hidden objects, that lighting can act as a state indicator, and that a 4-digit code is best entered deliberately rather than automatically; the *aesthetics* aim primarily at **discovery** and **submission** (in Hunicke et al.'s taxonomy) — the player surrenders to the cause-and-effect grammar of the room. Player flow is supported by the **golden path** principle: at every moment exactly one mechanism is unsolved and physically distinct, so the player is rarely confused about *what* to try, only about *how*.

```
  ┌───────────────────────────────────────────┐
  │  Crosshair  ·                             │
  │                                           │
  │                                           │
  │                                           │
  │           [Press E to interact]           │
  │  Time: 02:14         Holding: Keycard     │
  └───────────────────────────────────────────┘
```

*Figure 2. HUD layout. A central reticle, a context prompt only when an interactable is targeted, a timer in the lower-left corner and an inventory readout in the lower-right.*

```
  BINARY SERVER RACK (after power on)            CODEBOOK (on desk)
  ┌───────────────────────────┐                  ┌─────────────────────┐
  │  □  ■  ■  ■   = 7  (MSB)  │                  │  MAINTENANCE LOG    │
  │  □  □  ■  □   = 2         │                  │                     │
  │  ■  □  □  ■   = 9         │                  │  [■ □ □ □]  =  8    │
  │  □  ■  □  □   = 4  (LSB)  │                  │  [□ ■ □ □]  =  4    │
  └───────────────────────────┘                  │  [□ □ ■ □]  =  2    │
     │                                           │  [□ □ □ ■]  =  1    │
     ▼   decode using legend                     │                     │
     Access code = 7294                          │  Sum lit drawers    │
                                                 │  per row.           │
                                                 └─────────────────────┘
```

*Figure 3. Puzzle 2 in its two halves. Left: the 4 × 4 server rack. Each row is one decimal digit; ■ is a drawer that physically extruded from the wall with a green LED lit, □ is a drawer that stayed flush and dark. Right: the maintenance-log paper on the codebook desk, giving the 4-bit weight legend. Players sum the lit drawers in each row (e.g. row 1: 0 + 4 + 2 + 1 = 7) to recover the four access digits, then enter `7294` on the keypad.*

---

## 3. Implementation

### 3.1 Team Responsibilities

The project was completed by a three-person team. Responsibilities were divided to minimise overlap while keeping all members involved in design discussions.

| Member | Primary Responsibility | Secondary Contribution |
|--------|------------------------|------------------------|
| **Member A** | Player controller, interaction system, raycast targeting, HUD wiring | Lighting transitions, generator puzzle |
| **Member B** | Puzzle scripting (keypad, safe door, blast door, victory trigger), game state machine | Settings panel UI, inventory model |
| **Member C** | Procedural audio engine, scene composition, lighting setup, build pipeline, play-testing coordination | Materials and shader parameters, README documentation |

All members participated jointly in the initial concept brainstorming, the weekly design reviews and the final play-test debrief. Source control was performed via a private Git repository with feature branches and pull-request review by at least one other member before merging to `main`.

### 3.2 Development Process

Development was organised into four sequential stages, each producing a runnable build that was validated against a checklist before the next stage began.

- **Stage A — Foundation (Week 1)**: Unity 2022.3.62f3 project created on the Built-in Render Pipeline. The room geometry, walls, ceiling, the player capsule with `Rigidbody`, the first-person camera, the WASD/mouse controller, the red corner lighting and the bare HUD reticle were assembled. Acceptance: the player can walk around the room without clipping through walls and observe the alarm-light atmosphere.
- **Stage B — Core Loop (Weeks 2–3)**: the `IInteractable` contract, the `InteractionRaycaster`, the contextual HUD prompt, the crate stack with physics, the energy core pickup, the generator terminal and the lighting transition were implemented. Acceptance: pushing the crates exposes the core, inserting it into the generator switches the room from red to white lighting.
- **Stage C — Puzzle Chain (Weeks 4–5)**: the 12-button 3D keypad, the four-digit code validator, the safe door rotation, the keycard pickup, the blast door dual-leaf slide and the victory trigger were added. Acceptance: a fresh player can chain all three puzzles to victory without external help.
- **Stage D — Polish (Weeks 6–7)**: Start / Pause / Victory / Settings menus, the elapsed-time timer, the inventory readout, the procedural audio engine, the persistent volume settings, the procedural crosshair sprite and the build settings were added. The full run was play-tested with five external participants and minor tuning was applied (keypad button spacing, footstep cadence, success chord pitch).

### 3.3 Tools and Technologies

| Category | Tool |
|----------|------|
| Game engine | Unity 2022.3.62f3 (Built-in Render Pipeline) |
| IDE | Visual Studio 2022 / JetBrains Rider |
| Source control | Git + GitHub (private repository) |
| Project management | Trello board with weekly milestones |
| Diagramming | draw.io for architecture sketches |
| Audio reference | Online frequency-pitch tables for the C-major chord and tritone-based error buzz |
| Documentation | Markdown rendered via VS Code preview |

Generative AI assistants were occasionally consulted as a search-engine substitute for Unity API recall (for example, the correct order of `Rigidbody.MovePosition` vs `Rigidbody.velocity` for kinematic-friendly movement). All design decisions, scripting, scene composition, audio synthesis and play-testing were authored and validated by the team members themselves.

### 3.4 Technical Highlights

Three implementation choices proved disproportionately valuable.

- **Single point of truth for state** — `GameStateManager` is a singleton holding only four flags (`State`, `IsPowerOn`, `HasKeycard`, `RunTime`) and exposes typed events. Every gameplay system (lighting, audio, puzzles, UI) subscribes to those events instead of polling, which made the codebase resilient to ordering bugs.
- **Procedural audio via `AudioClip.Create`** — twelve cues are generated at startup by writing a `float[]` buffer of additive sine, square or noise samples shaped by an ADSR envelope and assigning it to a runtime `AudioClip`. This removed the need for any imported audio file while producing a unified retro-electronic aesthetic. Two independent volume channels (`InteractionVolume`, `ActionVolume`) are persisted via `PlayerPrefs` and mixed live by the `AudioManager` singleton.
- **Programmatic scene reconstruction** — the entire scene is regenerated on demand by an editor script, `SceneBuilder.cs`, which clears the open scene, recreates every material, every primitive, every component reference, every Canvas and every persistent UI listener. This gave the team the ability to iterate on the layout without merge conflicts in the binary `.unity` file.

### 3.5 Issues Encountered

- **UI listener loss after save** — UI buttons initially used `onClick.AddListener(...)` which only registers runtime delegates. After a Unity restart the buttons were unresponsive. The fix was to switch to `UnityEventTools.AddPersistentListener` so that the bindings are serialised inside the `.unity` file.
- **Keypad text mirrored** — `TextMesh` renders its readable face on the local –Z axis, so attaching it with `Quaternion.identity` while standing on the +Z side made all digits appear mirrored. A 180° rotation around the local Y axis fixed every label in one place.
- **Crate impacts during the main menu** — the crates settle under gravity the moment the scene loads, which produced an undesired collision soundtrack on the start screen. A guard was added in `CrateCollisionSound.OnCollisionEnter` so impact sounds are only emitted while `GameStateManager.State == Playing`.

### 3.6 Preliminary Player Experience

Five informal play-testers (university students, no prior knowledge of the game) were asked to play to completion while thinking aloud. Average completion time was **6 min 12 s**, with a range of 4 min 50 s to 9 min 30 s. All five testers reached the victory screen unaided. Common qualitative observations were:

- All testers correctly understood within ten seconds that the red lighting communicated an alarm state and that the blast door was the goal.
- Three of five initially tried to interact with the keypad before restoring power, observed that nothing happened, and inferred that another step was required — exactly the intended chain.
- Two testers reported the original keypad button labels were difficult to read at a glance; this prompted the late-stage tuning of label scale and back-plate dimensions.
- All five spontaneously commented positively on the colour-temperature transition when power was restored, describing it as "satisfying" or "the moment it felt like a real game".
- For the binary-rack puzzle, four of the five testers intuitively understood, after finding the codebook paper, that each row encoded one decimal digit via the weight table; the fifth tester needed to re-read the maintenance log twice before connecting the rack rows to the legend, which was judged a desirable difficulty level rather than a usability failure.
- Three testers explicitly reported a strong "turn-around moment" when the rack animated open behind them after they restored power at the generator on the opposite wall; the row-by-row extrusion with the low mechanical rumble was singled out as the most memorable event in the playthrough.

No tester reported motion sickness, ambiguity in controls, or accidental softlocks.

---

## 4. User Manual

### 4.1 System Requirements

- Windows 10 or later (64-bit), or macOS 11 or later
- DirectX 11 capable GPU with 1 GB VRAM
- Keyboard and two-button mouse
- Approximately 200 MB of free disk space

### 4.2 How to Run the Game

**From a built executable** (recommended for end users):

1. Unzip the distributed archive to any folder.
2. On Windows, double-click `SIP_Lab_Lockdown.exe`. On macOS, double-click the `.app` bundle.
3. The game starts in a windowed 1280 × 720 mode by default. Press `Alt + Enter` to toggle full-screen.

**From source** (for developers and module markers):

1. Install Unity Hub and the editor version `2022.3.62f3`.
2. Open Unity Hub, click `Add → Add project from disk` and select the project root folder.
3. Open the project; Unity will restore packages on first launch (this can take several minutes).
4. In the Project window open `Assets/Scenes/MainScene.unity`.
5. Press the play button (▶) at the top of the editor.

### 4.3 Controls

| Input | Action |
|-------|--------|
| `W` `A` `S` `D` | Walk forward / left / back / right |
| Mouse movement | Look around (yaw and pitch) |
| `E` or Left mouse button | Interact with the highlighted object |
| `Esc` or `Space` | Open / close the pause menu |

### 4.4 Game Flow

1. The Main Menu appears first. Click **Start** to begin, **Settings** to adjust audio, or **Quit** to close.
2. You spawn near the south wall, facing the blast door. The room is lit by red emergency lights.
3. Find a way to restore power, then to open the safe, then to open the blast door. There are no other objectives.
4. Walking through the open blast door triggers the victory screen, which displays your total escape time.

### 4.5 Settings

Both the Main Menu and the Pause Menu expose a **Settings** screen with two sliders:

- **Interaction Volume** — affects pickups, keypad beeps, password feedback, door sounds, the power-on cue and the victory fanfare.
- **Action Volume** — affects footsteps and crate impacts.

Slider positions are saved automatically to `PlayerPrefs` and restored at the next launch.

---

## 5. Discussion and Conclusion

### 5.1 Critical Discussion

**Strengths.** The project successfully delivers a complete, polished and bug-free escape sequence within the agreed scope. The "no external assets" constraint paid off creatively: the unified visual language (flat-shaded primitives, two-colour lighting palette) and the unified sonic language (synthesised retro-electronic cues) make the game feel deliberately stylised rather than impoverished. The interaction grammar is extremely consistent — every interactable object, regardless of its role, obeys the same `IInteractable` contract, which kept the codebase compact (~1100 lines of gameplay code) and easy to extend. The state machine made adding the Settings panel late in the cycle a one-evening task because no other system had to be modified.

**Weaknesses.** The single-room scope, while pedagogically appropriate, limits replayability: once a player has solved the keypad, the second playthrough collapses to a five-minute speedrun. The puzzles are also strictly sequential; there is no parallelism or alternative solution path, which is a missed opportunity for emergent play. Visually, the reliance on Unity primitives leaves the room recognisably "blocky" — a future pass with custom low-poly meshes or even a stylised post-process effect would significantly raise the production-value ceiling. Finally, accessibility was not a first-class concern: the game lacks subtitles for audio cues, customisable mouse sensitivity, and a colour-blind-friendly indicator for the red-to-white lighting transition.

### 5.2 Ethical Considerations

Although the game contains no violence, gambling, microtransactions or user-generated content, three ethical points warrant explicit acknowledgement.

- **Photosensitivity** — the power-restoration sequence involves a brief brightness ramp from very dim red to bright white. While the transition is smoothed over 1.5 seconds and never strobes, a future release should still include a "reduced flashing" toggle for players with photosensitive epilepsy.
- **Sound exposure** — all cues are mixed to peak below 0 dBFS and the default Action Volume is set to 70 % to avoid surprise loud impacts; nevertheless, the procedural error buzz can be unpleasant at full system volume on closed-back headphones, and a clearer in-game volume warning would be appropriate before commercial release.
- **Data and privacy** — the game stores only two integer values (the two volume sliders) in `PlayerPrefs`, contains no telemetry, no network code and no advertising SDK, so personal data exposure is effectively zero. We mention this explicitly because the absence of data collection is an ethical position, not merely an omission.

### 5.3 Future Work

Three avenues are most appealing for a follow-up version.

1. **Multi-room procedural mode** — generalise the `SceneBuilder` so it can stitch together multiple rooms drawn from a small library of puzzle templates. Combined with seeded randomisation of the keypad code and the energy-core hiding spot, this would deliver meaningful replay value.
2. **Asymmetric co-op (inspired by *Keep Talking and Nobody Explodes*)** — a second player on a phone or tablet could see the hint board and read the code aloud while the desktop player operates the keypad. Networking would only need to synchronise a handful of state-machine flags, which is well within the current architecture's reach.
3. **Accessibility pass** — add subtitles for every audio cue, a high-contrast UI mode, mouse-sensitivity and field-of-view sliders, a "reduced flashing" toggle, and rebindable keys. None of these requires architectural change; all of them widen the potential audience considerably.

### 5.4 Conclusion

*The SIP Lab Lockdown* set out to demonstrate, in a very compact form, that a small student team can deliver a complete first-person 3D experience without leaning on third-party art or audio. By coupling a strict interface-based interaction system, a single-source-of-truth state machine, a procedural audio engine and a programmatic scene builder, we produced a game that is small but coherent, technically clean and consistently styled. Player feedback confirmed that the causal puzzle chain is legible without tutorialisation and that the diegetic interface successfully sustains immersion. The remaining limitations — single room, single solution path, missing accessibility options — are well understood and form a credible roadmap for future iteration. We consider the project a successful proof that constraint can be a creative ally rather than a handicap.

---

## References

1. Hunicke, R., LeBlanc, M., & Zubek, R. (2004). *MDA: A Formal Approach to Game Design and Game Research*. Proceedings of the AAAI Workshop on Challenges in Game AI.
2. Schell, J. (2019). *The Art of Game Design: A Book of Lenses* (3rd ed.). CRC Press.
3. Unity Technologies. (2024). *Unity User Manual 2022.3 (LTS)*. Available at https://docs.unity3d.com/2022.3/Documentation/Manual/
4. Fireproof Games. (2012). *The Room*. iOS / PC.
5. Valve Corporation. (2007). *Portal*. PC.
6. Steel Crate Games. (2015). *Keep Talking and Nobody Explodes*. PC / VR.
