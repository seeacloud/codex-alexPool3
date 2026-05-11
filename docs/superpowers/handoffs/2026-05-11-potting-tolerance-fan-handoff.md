# 2026-05-11 Potting Tolerance Fan Handoff

## Current State

The user confirmed the desired visualization:

- Show a translucent fan starting from the cue ball center.
- The fan extends to the table boundary.
- The fan represents cue-stick aim tolerance, not ball travel trajectory.
- If the player's manual aim line falls inside the fan, the shot direction is considered acceptable for potting the selected pocket.

The design spec has been written and committed:

- Spec: `docs/superpowers/specs/2026-05-11-potting-tolerance-fan-design.md`
- Commit: `31277de docs: design potting tolerance fan`

## Important Design Decisions

Use a new dedicated renderer instead of adding this to `TrajectorySnapshotRenderer`.

Planned component:

- `Assets/Scripts/Visualization/PottingToleranceFanRenderer.cs`

Planned `AimManager` integration:

- Add `public PottingToleranceFanRenderer toleranceFanRenderer;`
- Auto-wire it on `_Visualization`, matching the existing renderer pattern.
- In `Recompute()`, after `AimSolver.Compute()` succeeds, call the fan renderer.
- Hide the fan in all existing no-pocket, unsolvable, or hidden-aim early returns.

The renderer should receive the current pocket and `jawGap`, not only the potting point. This lets it call `TableGeometry.BuildPocketMouth()` and use the same mouth endpoints as the shot geometry.

Intended call shape from the spec:

```csharp
toleranceFanRenderer.Show(cueBall.Center, targetBall.Center, currentPocket, table.ballRadius, table, jawGap);
```

`jawGap` should come from `shotSimulator.jawGap` when available, matching `AimManager.GetPottingPointFor()`.

## Geometry Notes

The ideal direction is `cueBall -> ghostBallCenter`.

Boundary directions:

1. Get the current pocket index from `table.Pockets.IndexOf(currentPocket)`.
2. Build the pocket mouth with `TableGeometry.BuildPocketMouth(table, pocketIndex, jawGap)`.
3. Shrink the mouth endpoints inward by one ball radius, following the same idea used by `ShotSimulator.RayMouthEntryTime()`.
4. For each usable mouth endpoint, compute the target-ball outbound direction.
5. Convert each outbound direction into a ghost-ball center two ball radii behind the target ball.
6. Convert each ghost-ball center into a cue direction from the cue ball.
7. Render the sector between those two cue directions, clipped to the playfield rectangle.

Inspector parameters from the spec:

- `minHalfAngleDegrees`
- `maxHalfAngleDegrees`
- `segmentCount`
- `fanColor`
- `boundaryColor`
- `surfaceYOffset`

## Suggested Next Steps

1. Use `superpowers:writing-plans` before implementation, because the design spec is approved.
2. Add focused tests for the pure geometry helpers if practical.
3. Implement `PottingToleranceFanRenderer`.
4. Wire it into `AimManager`.
5. Run the relevant edit-mode tests or at least compile the Unity C# project.
6. Visually verify in Unity that the fan starts at the cue ball, reaches the table boundary, and updates when pocket, cue ball, target ball, or manual aim changes.

## Working Tree Warning

The repository already contains many unrelated modified and untracked files. Do not reset or clean them. Treat them as user/session work and only touch files needed for the tolerance fan implementation.

Observed dirty files at handoff included existing changes under:

- `Assets/Scripts/Core/AimManager.cs`
- `Assets/Scripts/Trajectory/ShotSimulator.cs`
- `Assets/Scripts/Visualization/CutAngleArcRenderer.cs`
- `Assets/Scripts/Visualization/TrajectorySnapshotRenderer.cs`
- `Assets/Tests/EditMode/`
- scene/editor/project settings files

Run `git status --short` at the start of the next session before editing.

## Temporary Visual Aid

A standalone local visual aid was created outside the Unity project root:

- `E:\codexbase\codex-alexPool3\tolerance-fan-visual.html`

It is only a discussion artifact. It does not need to be committed or used by Unity.
