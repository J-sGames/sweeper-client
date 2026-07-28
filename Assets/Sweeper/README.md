# Sweeper project layout

All game-owned assets live below this directory. Unity template and render-pipeline
assets remain outside it.

- `Scenes`: production and development scenes.
- `Scripts/Core`: application bootstrap, game state, and shared lifecycle.
- `Scripts/Input`: touch/mouse input and swipe interpretation.
- `Scripts/Gameplay/Ball`: ball movement, launch, collision, and return.
- `Scripts/Gameplay/Bricks`: brick health, damage, and destruction.
- `Scripts/Gameplay/Board`: board bounds, rows, rounds, and level flow.
- `Scripts/UI`: player-facing UI.
- `Scripts/Debug`: development-only overlays and trajectory diagnostics.
- `Prefabs`: reusable Unity objects grouped by gameplay domain.
- `Art`: sprites, materials, animation, and visual effects.
- `Audio`: music and sound effects.
- `Settings`: game-specific configuration assets.
- `Tests/EditMode`: pure logic and editor tests.
- `Tests/PlayMode`: scene, input, physics, and integration tests.
