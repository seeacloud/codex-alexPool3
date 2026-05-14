# Potting Tolerance Fan Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render a translucent cue-aim tolerance fan from the cue ball to the table boundary for the currently selected potting pocket.

**Architecture:** Add a dedicated `PottingToleranceFanRenderer` under `Visualization`, with pure static geometry helpers on the same class so edit-mode tests can validate fan bounds without scene rendering. `AimManager` owns visibility and calls the renderer only after the current pocket is solvable.

**Tech Stack:** Unity C#, NUnit edit-mode tests, existing `TableController`, `PocketMarker`, `TableGeometry`, and `AimSolver`.

---

## File Structure

- Create `Assets/Scripts/Visualization/PottingToleranceFanRenderer.cs`: component responsible for computing fan boundary directions, clipping rays to the playfield, and rendering one mesh plus two optional boundary `LineRenderer`s.
- Create `Assets/Tests/EditMode/PottingToleranceFanRendererTests.cs`: edit-mode tests for pure geometry helpers and degenerate cases.
- Modify `Assets/Scripts/Core/AimManager.cs`: add the renderer field, auto-wire it on `_Visualization`, hide it on existing no-aim paths, and show it after `AimSolver.Compute()` succeeds.

## Task 1: Geometry Tests

**Files:**
- Create: `Assets/Tests/EditMode/PottingToleranceFanRendererTests.cs`
- Later implementation: `Assets/Scripts/Visualization/PottingToleranceFanRenderer.cs`

- [ ] **Step 1: Write the failing tests**

Add these tests:

```csharp
using NUnit.Framework;
using PoolAimTrainer.SceneObjects;
using PoolAimTrainer.Trajectory;
using PoolAimTrainer.Visualization;
using UnityEngine;

namespace PoolAimTrainer.Tests.EditMode
{
    public class PottingToleranceFanRendererTests
    {
        const float R = 0.0286f;

        [Test]
        public void TryComputeFanGeometry_ReturnsBoundariesAroundIdealDirection()
        {
            var table = CreateTableWithSixPockets();
            var pocket = table.Pockets[5];
            var cue = new Vector3(-0.35f, R, 0f);
            var target = new Vector3(0.25f, R, 0.18f);

            bool ok = PottingToleranceFanRenderer.TryComputeFanGeometry(
                cue, target, pocket, R, table, 0.08f, 1f, 40f,
                out var geometry);

            Assert.That(ok, Is.True);
            Assert.That(geometry.leftDir.sqrMagnitude, Is.GreaterThan(0.99f));
            Assert.That(geometry.rightDir.sqrMagnitude, Is.GreaterThan(0.99f));
            Assert.That(geometry.idealDir.sqrMagnitude, Is.GreaterThan(0.99f));
            Assert.That(Vector3.SignedAngle(geometry.leftDir, geometry.idealDir, Vector3.up), Is.GreaterThanOrEqualTo(0f));
            Assert.That(Vector3.SignedAngle(geometry.idealDir, geometry.rightDir, Vector3.up), Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void TryComputeFanGeometry_ReturnsFalseForCoincidentCueAndTarget()
        {
            var table = CreateTableWithSixPockets();
            var pos = new Vector3(0f, R, 0f);

            bool ok = PottingToleranceFanRenderer.TryComputeFanGeometry(
                pos, pos, table.Pockets[0], R, table, 0.08f, 1f, 40f,
                out _);

            Assert.That(ok, Is.False);
        }

        [Test]
        public void ClipRayToPlayfield_ReturnsPointOnOuterBoundary()
        {
            var table = CreateTableWithSixPockets();
            var start = new Vector3(0f, R, 0f);

            Vector3 hit = PottingToleranceFanRenderer.ClipRayToPlayfield(
                start, new Vector3(1f, 0f, 0.25f).normalized, table);

            Assert.That(hit.x, Is.EqualTo(table.playfieldHalfLength).Within(1e-4f));
            Assert.That(Mathf.Abs(hit.z), Is.LessThanOrEqualTo(table.playfieldHalfWidth + 1e-4f));
            Assert.That(hit.y, Is.EqualTo(R).Within(1e-4f));
        }

        static TableController CreateTableWithSixPockets()
        {
            var tableGo = new GameObject("Table");
            var table = tableGo.AddComponent<TableController>();
            table.playfieldHalfLength = 1.12f;
            table.playfieldHalfWidth = 0.56f;
            table.ballRadius = R;

            AddPocket(table, -1.12f, -0.56f);
            AddPocket(table, 0f, -0.56f);
            AddPocket(table, 1.12f, -0.56f);
            AddPocket(table, -1.12f, 0.56f);
            AddPocket(table, 0f, 0.56f);
            AddPocket(table, 1.12f, 0.56f);
            return table;
        }

        static void AddPocket(TableController table, float x, float z)
        {
            var go = new GameObject("Pocket");
            go.transform.SetParent(table.transform, false);
            go.transform.position = new Vector3(x, 0f, z);
            table.Pockets.Add(go.AddComponent<PocketMarker>());
        }
    }
}
```

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
Tuanjie.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml
```

Expected: compile failure or test failure because `PottingToleranceFanRenderer` does not exist yet.

## Task 2: Renderer Geometry And Mesh

**Files:**
- Create: `Assets/Scripts/Visualization/PottingToleranceFanRenderer.cs`

- [ ] **Step 1: Add minimal implementation**

Create `PottingToleranceFanRenderer` with:

```csharp
public struct FanGeometry
{
    public Vector3 origin;
    public Vector3 leftDir;
    public Vector3 rightDir;
    public Vector3 idealDir;
}

public static bool TryComputeFanGeometry(
    Vector3 cueBallCenter, Vector3 targetBallCenter, PocketMarker pocket,
    float ballRadius, TableController table, float jawGap,
    float minHalfAngleDegrees, float maxHalfAngleDegrees,
    out FanGeometry geometry)
```

The method should:

- resolve `pocketIndex = table.Pockets.IndexOf(pocket)`
- call `TableGeometry.BuildPocketMouth(table, pocketIndex, jawGap)`
- shrink mouth endpoints inward by `ballRadius`
- convert each mouth endpoint to target outbound direction
- convert outbound directions to ghost centers two ball radii behind target
- convert ghost centers to cue directions
- order the two directions around `idealDir`
- clamp the half-angle between `minHalfAngleDegrees` and `maxHalfAngleDegrees`

Also add:

```csharp
public static Vector3 ClipRayToPlayfield(Vector3 start, Vector3 dir, TableController table)
```

using `table.playfieldHalfLength` and `table.playfieldHalfWidth`.

- [ ] **Step 2: Run geometry tests to verify GREEN**

Run the edit-mode test command again.

Expected: `PottingToleranceFanRendererTests` pass.

- [ ] **Step 3: Add rendering behavior**

Add public inspector fields:

```csharp
public float minHalfAngleDegrees = 1f;
public float maxHalfAngleDegrees = 40f;
public int segmentCount = 32;
public Color fanColor = new Color(0.2f, 0.9f, 1f, 0.18f);
public Color boundaryColor = new Color(0.2f, 0.95f, 1f, 0.8f);
public float surfaceYOffset = 0.004f;
public bool showBoundaryLines = true;
```

Implement `Show(...)` to build a mesh sector from cue origin to clipped playfield edge, and `Hide()` to deactivate mesh and boundary lines.

## Task 3: AimManager Integration

**Files:**
- Modify: `Assets/Scripts/Core/AimManager.cs`

- [ ] **Step 1: Add renderer field**

Add:

```csharp
public PottingToleranceFanRenderer toleranceFanRenderer;
```

near the other visualization renderers.

- [ ] **Step 2: Auto-wire renderer**

Update `AutoWireCueThroughTarget()` so it also gets or adds `PottingToleranceFanRenderer` on `_Visualization`.

- [ ] **Step 3: Hide on existing early returns**

In `Recompute()`, hide `toleranceFanRenderer` wherever ghost, aim line, and cut angle visuals are hidden because aim visuals are off, pocket is missing, or the shot is unsolvable.

- [ ] **Step 4: Show after solvable AimSolver result**

After the existing ghost, aim line, and cut angle renderer calls, compute:

```csharp
float jawGap = shotSimulator != null ? shotSimulator.jawGap : DEFAULT_JAW_GAP;
toleranceFanRenderer.Show(
    cueBall.Center, targetBall.Center, currentPocket,
    table.ballRadius, table, jawGap);
```

## Task 4: Verification

**Files:**
- Verify: `Assets/Scripts/Visualization/PottingToleranceFanRenderer.cs`
- Verify: `Assets/Scripts/Core/AimManager.cs`
- Verify: `Assets/Tests/EditMode/PottingToleranceFanRendererTests.cs`

- [ ] **Step 1: Run edit-mode tests**

Run:

```powershell
Tuanjie.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml
```

Expected: all edit-mode tests pass.

- [ ] **Step 2: Inspect working tree**

Run:

```powershell
git status --short
```

Expected: only the planned files are newly changed by this implementation, alongside pre-existing dirty files.
