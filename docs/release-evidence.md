# Release Evidence

- Candidate: Swarm Catcher 1.0.0 Windows x64
- Date: 2026-09-17
- Source checkpoint: `18c7fdc`
- Status: Local candidate; not approved for public distribution

## Automated verification

- `dotnet build SwarmCatcher.sln -c Release`: passed with no warnings or errors.
- `dotnet test SwarmCatcher.sln -c Release --no-build`: 15 passed, 0 failed.
- `dotnet format SwarmCatcher.sln --verify-no-changes`: passed.
- Published application launch: the self-contained executable remained responsive during a local smoke test.
- Archive launch: the ZIP was extracted to a separate directory and the extracted executable remained responsive during a local smoke test.

The performance harness now uses the production `SwarmRenderer` and can write a durable JSON report containing resolution, operating system, logical processor count, available memory, duration, rendered frames per second, average frame time, p95 and p99 frame times, and allocation rate. Run the required 30-minute foreground scenario with:

```powershell
dotnet run --project tools/SwarmCatcher.Performance/SwarmCatcher.Performance.csproj -c Release -- --duration-minutes 30 --output artifacts/performance-report.json
```

A short automated-launch smoke run proved report generation. Its rendering measurements are not acceptance evidence because the window was launched from a non-interactive automation context where WPF may be occluded or throttled. The product owner's earlier foreground observation remains recorded in [`performance-baseline.md`](performance-baseline.md).

## Packaging

The self-contained folder and versioned ZIP are produced under the ignored `artifacts/` directory. Rebuild the archive after any source change; record its final byte count and SHA-256 below.

- ZIP: `artifacts/SwarmCatcher-1.0.0-win-x64-18c7fdc.zip`
- Size: 65,109,270 bytes
- SHA-256: `C8EBC59532B262AE00214AB0CA9B24ABF0BD8C62287854021DFC81D059700DC3`

The generated buzz is synthesized locally at runtime, and all other visuals are code-drawn WPF geometry. No third-party visual or audio assets are distributed.

## Open production gates

- Run the foreground 30-minute performance scenario on the agreed minimum hardware and confirm at least 60 rendered frames per second without sustained allocation growth.
- Complete manual mouse, keyboard, 200% text, mute, high-contrast, reduced-motion, DPI, focus-loss, and monitor-edge checks.
- Verify the self-contained archive on supported Windows 10 and Windows 11 clean environments.
- Obtain review of the learning text from a knowledgeable beekeeper.
- Decide whether public distribution requires code signing.
- Obtain named human production approval.
