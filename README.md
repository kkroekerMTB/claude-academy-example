# Swarm Catcher

Swarm Catcher is a Windows mini-game about safely collecting a bivouacked honey-bee swarm. It renders 5,000 independently simulated bees, including one queen, and teaches through the events of the game rather than a quiz.

## Play

1. Start the game and watch where the swarm settles.
2. Select the cardboard box and click or drag to position its opening below the cluster.
3. Select the bee brush, then drag through the cluster and release to close the box.
4. Catch at least 50% of the bees and the queen to succeed.

The inventory and exit control remain visible throughout play. Keyboard controls are:

- `S`: start
- `1`: select the cardboard box
- `2`: select the bee brush
- `P` or `Esc`: pause or resume
- `R`: restart with a new swarm
- `X`: exit

The accessibility panel provides buzz volume and mute controls, 100–200% text sizes, high-contrast interaction cues, and reduced motion.

## Develop

The repository targets .NET 10 LTS and C# 14. Use the commands documented in [`AGENTS.md`](AGENTS.md) to build, test, format, run, and measure the application.

The approved product record is [`intent.md`](intent.md), [`spec.md`](spec.md), and [`plan.md`](plan.md). The game is local-only: it has no accounts, network services, telemetry, screen capture, or document access.
