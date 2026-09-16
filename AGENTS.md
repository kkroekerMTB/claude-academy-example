# Repository Instructions

## Working Agreement

- Optimize for low cognitive load and prefer simple, direct designs.
- Concentrate complexity behind narrow, stable interfaces.
- Avoid speculative abstractions, unnecessary indirection, and scattered special cases.
- Use precise names. Add comments only for rationale, constraints, or hidden invariants.
- Preserve unrelated work and keep each change within the approved intent and specification.

## AI-Native SDLC

The committed artifacts are the handoffs and audit record for the work:

1. `intent.md` states the problem, outcome, constraints, and unresolved questions. A product owner approves it.
2. `spec.md` translates approved intent into requirements, design, policy checks, and acceptance criteria. The product owner and relevant policy owners approve it.
3. `plan.md` defines the implementation, verification, release, and rollback approach. An engineer approves it before implementation.
4. Code and tests provide the implementation and repeatable evidence.
5. The pull request records independent review. The agent that implements a change cannot approve it.
6. Production signals and incidents feed new tests, eval cases, or a new `intent.md`.

Keep these files current when a decision changes. Flag conflicts between artifacts instead of silently choosing one. Commit frequently to materialize the full audit trail for each committed artifact as you work.

## Controls

- Guidance belongs in this file or in reusable skills.
- Deterministic controls such as tests, hooks, permissions, and sandboxes enforce rules that must always hold.
- A named human owns risk acceptance and production approval.
- Do not weaken or remove a failing test merely to make a change pass without explicit approval.
- Use only approved deployment and rollback tools. Keep production changes behind human authorization.

## Repository Commands

Replace each placeholder when the repository gains an implementation. Agents must use the documented commands rather than inventing alternatives.

- Build: `TBD`
- Test: `TBD`
- Lint or static analysis: `TBD`
- Run locally: `TBD`

## Completion Standard

A change is ready for review when it matches `intent.md`, `spec.md`, and `plan.md`; required checks pass; quality risks have evidence; rollout and rollback are understood; and the final diff contains no unrelated changes.
