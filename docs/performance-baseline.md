# Performance Baseline

- Date: 2026-09-16
- Build: `0ae5864`
- Scenario: 5,000 independently updated and rendered bees in the maximized WPF performance harness.

## Machine

- Operating system: Windows 11 Enterprise 10.0.26200
- CPU: 13th Gen Intel Core i9-13900H, 14 cores and 20 logical processors
- Memory: 31.6 GB
- GPUs: NVIDIA RTX 2000 Ada Generation Laptop GPU and Intel Iris Xe Graphics
- Display: 2560 × 1440

## Observation

The product owner reported that frame rate and allocation rate were acceptable, spacing felt swarm-like, and `Esc` closed the harness reliably. The initial flight speed was too slow. Commit `0ae5864` increased the speed, and the product owner accepted the result.

## Limit

This machine exceeds the provisional minimum hardware in `plan.md`. This result accepts the tracer bullet on the development machine but does not close the minimum-hardware performance gate. Exact frame and allocation readings were visible in the harness but were not persisted; automated evidence capture remains required before release.
