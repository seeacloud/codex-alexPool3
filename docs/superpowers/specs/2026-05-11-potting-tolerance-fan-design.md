# Potting Tolerance Fan Design

## Goal

Show a translucent fan from the cue ball center that represents the range of cue-stick aim directions that can still pot the current target ball into the selected pocket.

The fan is an aim tolerance guide, not a predicted ball trajectory. If the player's manual aim line falls inside the fan, the shot direction is considered acceptable for potting. If it falls outside, the UI can later indicate that the aim is too thick or too thin.

## Current Context

`AimManager` already selects a pocket, computes the ideal ghost-ball aim with `AimSolver`, draws the normal ghost/aim lines, and displays the user's manual aim direction as the blue cue path. Existing visual components are auto-wired on the `_Visualization` object.

The new fan should follow the same pattern: a dedicated visualization component driven by `AimManager`, rather than folding this responsibility into `TrajectorySnapshotRenderer`.

## Recommended Approach

Add a new `PottingToleranceFanRenderer` component.

`AimManager` will call it when the current selected pocket is solvable. The renderer will:

- Compute the ideal cue direction from the cue ball to the ghost ball.
- Compute two tolerance boundary cue directions based on the left and right usable pocket-mouth boundaries.
- Render a translucent mesh sector starting at the cue ball center and extending to the table boundary.
- Render optional boundary lines so the tolerance range remains readable against the table.
- Hide itself whenever there is no solvable pocket, no table, or the aim visuals are disabled.

## Geometry

The ideal potting line is based on:

- target ball center
- selected potting point for the pocket
- ball radius

For each side of the pocket-mouth tolerance, the renderer derives a target-ball outbound direction, then places a corresponding ghost-ball center two ball radii behind the target ball along that outbound direction. The cue-ball-to-ghost direction for each side becomes one fan boundary.

The fan is clipped by intersecting each sampled ray with the playfield rectangle. It is rendered slightly above the table surface to prevent z-fighting.

Inspector clamps will keep extreme layouts readable:

- `minHalfAngleDegrees`
- `maxHalfAngleDegrees`
- `segmentCount`
- `fanColor`
- `boundaryColor`
- `surfaceYOffset`

## Integration

`AimManager` gains:

- `public PottingToleranceFanRenderer toleranceFanRenderer;`

`AutoWireCueThroughTarget()` will also auto-wire or add this renderer on `_Visualization`.

`Recompute()` will call:

```csharp
toleranceFanRenderer.Show(cueBall.Center, targetBall.Center, currentPocket, table.ballRadius, table, jawGap);
```

after `AimSolver.Compute()` succeeds. The renderer will use `TableGeometry.BuildPocketMouth()` with the current pocket index to get the mouth endpoints used for the boundary calculation. All early-return hide paths that hide ghost/aim lines will also hide the tolerance fan.

## Testing

Add edit-mode tests for the core geometry helper where practical:

- Boundary directions are ordered around the ideal direction.
- The fan hides or returns invalid geometry for degenerate cue/target/pocket positions.
- Ray clipping returns points on the table boundary.

Also compile the Unity C# project or run the existing edit-mode test command if available in this repo.

## Out Of Scope

This change does not implement scoring, thick/thin feedback text, or shot-quality grading. It only creates the visible tolerance range and wires it into the current aim visualization.
