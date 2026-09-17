# Implementation Plan

- Status: Approved
- Source: [`spec.md`](spec.md)
- Owner: Engineering
- Approver: Product owner

Do not begin implementation until `intent.md` and `spec.md` are approved and this plan has been reviewed by an engineer.

## Approach

Build one .NET 10 solution with a deterministic core and a thin WPF application:

- `SwarmCatcher.Core` owns simulation state, session phases, tool effects, lesson events, and outcome evaluation. It has no WPF dependency.
- `SwarmCatcher.App` owns the borderless WPF surface, batched rendering, input translation, audio, and accessible presentation.
- `SwarmCatcher.Tests` verifies deterministic behavior through the public Core interfaces.
- `SwarmCatcher.Performance` runs repeatable simulation and rendering scenarios and records frame-time and allocation evidence.

The highest risk is rendering and updating 5,000 independent bees at 60 frames per second. Build a runnable performance slice before implementing the complete game. If the slice cannot meet the target after profiling and one focused optimization pass, stop and revise the design or acceptance target with the product owner.

Use fixed simulation steps and seeded randomness so gameplay results do not depend on rendering speed and failures can be reproduced. Keep the simulation state contiguous and render all bees through one custom surface. Do not create one WPF control per bee.

## Planned Changes

| Area | Change | Reason |
| --- | --- | --- |
| Solution | Add a .NET 10 solution, shared build settings, WPF application, Core library, tests, and performance harness. | Establish one repeatable build and dependency direction. |
| Session | Implement the approved phase machine behind `GameSession`. | Concentrate transition rules and reject invalid commands in one place. |
| Simulation | Implement seeded bee state, fixed-step movement, bivouacking, falling, capture, and departure. | Keep gameplay deterministic and independent of WPF. |
| Rendering | Add a single DPI-aware batched sprite surface with interpolation and diagnostics. | Render 5,000 bees without thousands of UI elements. |
| Interaction | Add inventory, box placement, brush paths, hit testing, pause, restart, and exit. | Translate user input into semantic session commands. |
| Education | Add event-driven learning messages and an outcome recap. | Tie each lesson to something the player observes. |
| Accessibility | Add keyboard controls, scalable text, high-contrast cues, reduced motion, and audio controls. | Meet the approved accessibility requirements. |
| Packaging | Produce a self-contained Windows x64 folder and ZIP archive. | Let users run the game without installing a separate .NET runtime. |
| Guidance | Replace command placeholders in `AGENTS.md` after the solution exists. | Give later agent sessions exact build, test, format, run, and performance commands. |

## Implementation Steps

Each completed step gets its own focused commit after its checks pass.

1. [x] **Solution shell:** Create the four projects, establish dependency direction, add a minimal app window, add one passing test, and document working commands in `AGENTS.md`.
2. [x] **Performance tracer bullet:** Represent 5,000 uniquely identified bees, update them with a fixed step, draw them on one WPF surface, and display frame-time and allocation diagnostics. Run in Release x64 at 1920 × 1080.
3. [ ] **Performance gate:** Profile the tracer bullet against a provisional minimum of a four-core x64 CPU, 8 GB RAM, and a DirectX 11-capable GPU. Record the actual machine. Confirm or revise the published minimum hardware before continuing.
4. [x] **Session and outcome rules:** Implement phase transitions, pause restoration, restart, bee-state invariants, queen uniqueness, capture percentage, and the queen-required success rule using test-first deterministic scenarios.
5. [x] **Swarm behavior:** Add separation, alignment, cohesion, bounded noise, the moving swarm center, safe bivouac selection, temporary bivouacs, and final settling. Verify seeded repeatability and reachable final placements.
6. [x] **Player tools:** Implement inventory selection, keyboard equivalents, box placement and repositioning, brush-path sampling, falling trajectories, capture collision, missed-bee departure, and automatic box closure when sweeping ends.
7. [x] **Playable flow:** Connect briefing, swarming, bivouac, box placement, sweeping, resolution, retry, and exit into one complete session without diagnostic controls.
8. [x] **Learning and accessibility:** Add approved educational text, contextual lesson events, result recap, real-world safety wording, volume and mute controls, text scaling, high-contrast cues, and reduced motion.
9. [ ] **Production verification:** Run the complete automated suite, 30-minute stability scenario, accessibility checks, Windows 10 and Windows 11 checks, DPI and monitor-edge checks, and beekeeping content review.
10. [ ] **Release candidate:** Publish the self-contained x64 build, verify it on a clean supported Windows environment, archive the evidence, and obtain human production approval.

## Verification

### Automated Checks

- [x] Build: `dotnet build SwarmCatcher.sln -c Release`
- [x] Tests: `dotnet test SwarmCatcher.sln -c Release --no-build`
- [x] Formatting: `dotnet format SwarmCatcher.sln --verify-no-changes`
- [ ] Performance: run the performance harness in Release x64 at 1920 × 1080 and save its frame-time, frame-rate, allocation, and hardware report.
- [ ] Correctness: cover every session transition, every bee-state transition, seeded repeatability, queen uniqueness, capture boundaries, and restart reset behavior.

### Exploratory Checks

- [ ] Observe swarm motion for cohesion, variation, realistic settling, and absence of visible jumps.
- [ ] Complete successful and failed catches with mouse and keyboard controls.
- [ ] Test at 100%, 150%, and 200% display scaling.
- [ ] Test focus loss, pause and exit, a second attached monitor, and monitor-edge placement.
- [ ] Complete a game with muted audio, high-contrast cues, larger text, and reduced motion.
- [ ] Confirm the educational messages are accurate, readable, and connected to game events.

### Evidence

Attach or link the following from the pull request:

- Build, test, and format command results.
- Performance report with hardware, resolution, duration, frame-time percentiles, rendered frames per second, and allocation data.
- Development-machine tracer result: [`docs/performance-baseline.md`](docs/performance-baseline.md).
- Local release and verification record: [`docs/release-evidence.md`](docs/release-evidence.md).
- Seed values for deterministic acceptance scenarios.
- Windows 10, Windows 11, DPI, keyboard, and accessibility check results.
- Beekeeping content-review result.
- Release archive checksum and clean-machine launch result.

## Release and Rollback

- Release method: Publish a self-contained Windows x64 folder, package it as a versioned ZIP file, and retain the previous release artifact.
- Production approval: A named human reviews the evidence and approves publication.
- Success signals: The clean-machine check passes, required gameplay and accessibility paths work, the performance gate passes, and no release-blocking defect remains.
- Rollback trigger: Failure to launch, corrupted or missing assets, loss of player control, incorrect success evaluation, or a material performance regression.
- Rollback procedure: Remove the affected archive from distribution, restore the preceding version, and open a new defect or `intent.md` as appropriate. No user-data migration is required because the first release stores no durable game state.

## Risks and Open Decisions

- The 60 frames-per-second target depends on the final minimum hardware. The performance tracer bullet resolves this before feature work expands.
- Windows 10 support means editions and versions still supported by Microsoft; the release matrix must name the exact versions tested.
- Touch and pen input are deferred unless product direction changes before the player-tools step.
- Automatic box closure occurs when the player releases the brush or no unsettled bees remain. Product review can change this before the player-tools step.
- The product owner must name a knowledgeable beekeeper to review educational content before the release candidate.
- Decide whether the public release requires code signing before packaging work begins.
- Final visual and audio assets must have documented creation or licensing provenance.

Implementation continued after the product owner accepted the development-machine tracer result and directed the agent to finish. This did not waive the minimum-hardware gate: step 3 remains a pre-release requirement and the stronger automated harness must be run in a foreground interactive desktop session on the agreed minimum machine.

## Approval

- Decision: Approved
- Approved by: Product owner
- Date: 2026-09-16
- Notes: Approved for implementation, beginning with the solution shell and performance tracer bullet.

## Completion Record

- Pull request: TBD
- Independent reviewer: TBD
- Result: Pending
