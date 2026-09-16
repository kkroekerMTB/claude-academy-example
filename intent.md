# Intent

- Status: Approved
- Owner: Product owner
- Approver: Product owner

## Problem

People ages 7 through adult need an entertaining, approachable way to learn how honey bee swarms behave and how beekeepers catch them. Existing explanations do not provide the experience of observing a moving swarm, recognizing when it bivouacs, and safely collecting it.

## Desired Outcome

Create a set of educational beekeeping mini-games for Windows. The first game, **Swarm Catcher**, should let a player:

1. Watch a swarm buzz around before it settles somewhere in the user interface.
2. Select a cardboard box from an inventory and place it beneath the settled swarm.
3. Select a brush and sweep the bees toward the box.
4. See the box close after bees fall inside while bees that miss it fly out of view.

A successful catch contains at least 50% of the swarm and includes the queen.

By playing, the user should learn that:

- Swarming bees are generally docile.
- A beekeeper can catch a swarm in any sufficiently large, bee-tight container.
- A settled cluster is a bivouac.
- A swarm may move several times before settling.
- A swarm includes a queen, whose presence is necessary for the new colony to succeed.

## Constraints

- Audience: Ages 7 through adult.
- Platform: Windows.
- Runtime and language: .NET 10 LTS with C# source files.
- Simulation: A swarm contains approximately 5,000 independently animated bee objects, including one queen.
- Experience: Flying movement should appear smooth and realistic.
- Initial scope: Implement Swarm Catcher before adding other beekeeping mini-games.

## Open Questions

- Should the bees move only inside the game window or across the Windows desktop?
- Which input and accessibility needs must the first release support?
- How should the game confirm that players understood the educational material?

## Approval

- Decision: Approved
- Approved by: Product owner
- Date: 2026-09-16
- Notes: Approved as the basis for the Swarm Catcher specification. Amended on 2026-09-16 by product-owner direction to target .NET 10 LTS instead of .NET Framework.
