# Specification

- Status: Approved
- Source: [`intent.md`](intent.md)
- Owner: Product owner
- Approver: Product owner

This specification covers the first playable release of **Swarm Catcher**.

## Scope

Swarm Catcher is a single-player Windows mini-game for ages 7 through adult. A simulated swarm flies over a borderless game surface, may bivouac more than once, and eventually settles. The player places a cardboard box below the cluster and sweeps bees into it with a brush. The game evaluates the catch, explains the result, and reinforces accurate beekeeping concepts.

The first release targets .NET 10 LTS, C# 14, WPF, and Microsoft-supported Windows 10 and Windows 11 versions. The game runs on one selected monitor and does not require a network connection.

### Out of Scope

- Other beekeeping mini-games.
- Multiplayer, accounts, online services, telemetry, and cloud saves.
- Simulating a complete hive or colony after capture.
- Simultaneous play across multiple monitors.
- Interacting with actual desktop icons, windows, or files.
- User-created containers, brushes, levels, or bee species.

## Requirements

### Gameplay

1. **R1 — Start:** The player can start a new game, view concise instructions, pause, resume, restart, and exit at any time.
2. **R2 — Swarm:** Each game creates approximately 5,000 independently simulated bees, exactly one of which is the queen.
3. **R3 — Flight:** Bees move continuously as a coherent but non-uniform swarm without visible frame-to-frame jumps.
4. **R4 — Bivouac:** The swarm settles at a reachable location on the game surface. A game may show one or more temporary bivouacs before the final catchable bivouac.
5. **R5 — Inventory:** The player can select a cardboard box and a bee brush from an always-available inventory.
6. **R6 — Box placement:** The player can place and reposition the box while the swarm is settled. The game clearly indicates the box opening and valid placement area.
7. **R7 — Sweeping:** The player can sweep through the settled cluster with the brush. Contacted bees fall with varied trajectories rather than moving as one block.
8. **R8 — Capture:** A bee entering the box opening becomes captured. A bee that falls outside the opening can recover and fly out of view.
9. **R9 — Resolution:** The box closes when the sweep ends or no unsettled bees remain. The game reports the captured percentage and whether the queen was captured.
10. **R10 — Success:** The player succeeds only when at least 50% of all bees and the queen are inside the box.
11. **R11 — Retry:** After either outcome, the player can retry with a newly generated swarm.

### Education

12. **R12 — Docility:** Instructions explain that swarming honey bees are generally docile while avoiding a guarantee that they cannot sting.
13. **R13 — Container:** The game explains that any sufficiently large, bee-tight container can hold a captured swarm; the cardboard box is one suitable example.
14. **R14 — Bivouac:** The first settled cluster introduces the term *bivouac*. If the swarm moves again, the game explains that swarms may inspect or occupy several temporary locations before settling.
15. **R15 — Queen:** The outcome identifies whether the queen was captured and explains why the new colony needs her.
16. **R16 — Learning recap:** The result screen summarizes the lesson using the events that occurred during that game rather than requiring a quiz.

### Platform and Quality

17. **R17 — Platform:** The application targets `net10.0-windows`, uses C# source files and WPF, and runs on Microsoft-supported Windows 10 and Windows 11 versions.
18. **R18 — Performance:** During swarm flight at 1920 × 1080, the game sustains at least 60 rendered frames per second at the published minimum hardware specification, excluding initial loading and window activation.
19. **R19 — Stability:** Simulation and rendering perform no avoidable per-frame managed allocations and remain responsive for a continuous 30-minute session.
20. **R20 — Input:** All gameplay supports mouse input. Inventory selection, pause, restart, and exit also have documented keyboard controls.
21. **R21 — Accessibility:** The game provides independent buzz-volume control, mute, text scaling, high-contrast interaction cues, and a reduced-motion option. Information is not conveyed by color or sound alone.
22. **R22 — Safety:** The application does not capture the screen, inspect desktop content, read user documents, require elevation, or send data over the network.

## Acceptance Criteria

| Requirement | Verifiable outcome |
| --- | --- |
| R1 | A player can complete start, pause, resume, restart, and exit flows by mouse and by the documented keyboard commands where required. |
| R2 | A seeded game reports 5,000 bee identities with one and only one queen identity. |
| R3, R18, R19 | An automated performance scenario records continuous independent movement, at least 60 rendered frames per second at 1920 × 1080 on minimum hardware, no sustained allocation growth, and responsive controls during a 30-minute run. |
| R4 | Seeded scenarios demonstrate both a direct final bivouac and at least one temporary bivouac before the final location; every final cluster is reachable with the tools. |
| R5, R6, R7 | A player can select each tool, reposition the box, and sweep bees only while the corresponding interaction is valid. |
| R8 | Deterministic trajectory tests prove that bees crossing the opening are captured and bees missing it leave the visible game area. |
| R9, R10 | Boundary tests fail at 49.98%, pass at 50% when the queen is captured, and fail at any percentage when the queen is absent. |
| R11 | Retry resets the score, queen state, tools, swarm seed, and game phase without restarting the process. |
| R12–R16 | A content review confirms every approved learning point appears in context and that the wording remains suitable for ages 7 through adult. |
| R17 | A clean build produces a runnable .NET 10 Windows application and automated tests pass on supported Windows 10 and Windows 11 environments. |
| R20, R21 | A manual accessibility pass completes the game using the documented keyboard commands, 200% text, muted audio, high-contrast cues, and reduced motion. |
| R22 | Static review and runtime observation confirm no screen-capture, document-access, elevation, telemetry, or network capability. |

## Design

### User Experience

The application opens as a borderless surface on one monitor. It is visually laid over the desktop, but it treats the desktop only as a backdrop and never reads or manipulates desktop content. A compact inventory and game controls remain visible above the play area. `Esc` always pauses and exposes an exit action.

The session follows six visible phases:

1. **Briefing:** The player sees the goal, the two tools, and the docility safety note.
2. **Swarming:** Bees enter and move as a flock. The player observes but cannot place tools.
3. **Bivouacked:** The cluster forms at a reachable location. A short label defines *bivouac*. The cluster may depart and repeat this phase before the final stop.
4. **Box placement:** The player drags the box beneath the cluster. Invalid placement gets a clear shape and text cue.
5. **Sweeping:** The player drags the brush across the cluster. Bees fall individually; misses recover and leave the scene.
6. **Result:** The box closes and the game shows the capture percentage, queen status, outcome, and learning recap.

The game never hides an exit behind the animation. Reduced motion lowers flight speed and visual oscillation while preserving every simulated bee and the rules of play.

### System Behavior

Use one WPF application with a small set of modules. The simulation owns gameplay truth; WPF displays snapshots and forwards player intent. UI elements never own bee state.

#### `GameSession`

The top-level coordinator exposes commands such as start, pause, place box, begin sweep, end sweep, restart, and exit. It owns the phase transition table and rejects commands that are invalid in the current phase. Other modules do not coordinate phases with one another.

Phases are `Briefing`, `Swarming`, `Bivouacked`, `BoxPlacement`, `Sweeping`, `Resolved`, and `Paused`. A pause remembers and restores the prior phase.

#### `SwarmSimulation`

This module owns all bee state and advances the world by a fixed simulation step independent of rendering speed. Each bee has a stable identity, position, velocity, behavior state, and queen flag. Storage should be data-oriented and contiguous; “independent bee” does not require 5,000 WPF controls or 5,000 allocation-heavy objects.

Flight combines separation, alignment, cohesion, bounded noise, and attraction to a moving swarm center. Settling assigns nearby landing positions around a bivouac anchor. Sweeping applies force only to bees intersected by the brush path. Collision with the box opening captures a bee; leaving the play bounds after a miss removes it from active rendering without counting it as captured.

The simulation accepts a seed so tests can reproduce swarm movement and catch outcomes.

#### `SwarmRenderer`

A single custom WPF rendering surface draws batched bee sprites from an immutable simulation snapshot. It does not create a `FrameworkElement` for each bee. Rendering interpolates between fixed simulation steps, scales for DPI, and drops visual frames rather than altering simulation results when the machine is busy.

#### `PlayerTools`

This module owns box geometry, brush geometry, drag rules, keyboard equivalents, and hit testing. It sends semantic commands to `GameSession`; it does not modify bee state directly.

#### `LessonPresenter`

This module maps game events to approved educational text. It controls when a concept appears, avoids repeating the same message, and builds the result recap from events that actually occurred.

#### `OutcomeEvaluator`

This module receives the final captured identities and returns the captured count, percentage, queen status, and success result. The success rule lives here so UI, tests, and future game modes cannot implement competing versions.

### Invariants

- Every game contains exactly one queen.
- A bee identity belongs to exactly one state: active, settled, falling, captured, or departed.
- Captured and departed bees never return to active play.
- The capture result depends only on simulation state, never frame rate or rendering detail.
- Success requires both a capture percentage of at least 50% and the queen.
- The final bivouac and valid box placement area always fit on the selected monitor.

### Platform Decision

.NET 10 is the latest generally available supported .NET release as of 2026-09-16. It is an active LTS release supported through 2028-11-14. .NET 11 RC1 is a prerelease, so it is not the production target. Microsoft documents WPF support in .NET 10 and .NET 10 support on qualifying Windows 10 and Windows 11 versions.

Sources: [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy), [Install .NET on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows), [WPF in .NET 10](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net100)

## Policy Review

| Area | Decision or question | Owner |
| --- | --- | --- |
| Security and privacy | Local-only application with no elevation, screen capture, telemetry, network access, or user-document access. | Engineering reviewer |
| Child audience | No accounts, purchases, advertising, chat, personal data, or external links in gameplay. | Product owner |
| Beekeeping accuracy | A knowledgeable beekeeper reviews learning text before release. | Content reviewer |
| Accessibility | Keyboard-accessible controls, scalable text, high-contrast cues, reduced motion, and independent audio controls. | Product owner |

## Quality Risks

- Rendering 5,000 independently moving bees may miss the 60 frames-per-second target. Mitigate with contiguous state, fixed-step simulation, batched drawing, profiling, and an early performance prototype.
- A transparent desktop overlay can interfere with normal Windows interaction or trap the player. Keep the game on one monitor, provide a persistent pause/exit path, and test focus changes, DPI scaling, and multi-monitor edges.
- Random movement can produce unreachable bivouacs or inconsistent tests. Generate anchors from validated safe regions and make every automated scenario seedable.
- Young players may misread “docile” as “safe to handle without protection.” Use qualified wording and include a concise real-world safety note.
- The queen may be visually lost among thousands of bees. Do not make visual identification a success prerequisite; report queen capture clearly at resolution.

## Open Questions

- What CPU, GPU, and memory define the minimum hardware for the 60 frames-per-second acceptance test?
- Should the first release support touch or pen input in addition to mouse and keyboard?
- Who will perform the beekeeping content review?
- Should the box close automatically when sweeping stops, or should the player choose when to close it?

## Approval

- Decision: Approved
- Approved by: Product owner
- Date: 2026-09-16
- Notes: Approved as the basis for implementation planning.
