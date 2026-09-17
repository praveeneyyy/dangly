# Dangly Physics Engine Specification

This specification documents the exact numerical model, equations, constants, and algorithms extracted from the authoritative macOS Swift 6 implementation in `Dangly/Physics/` (`RopeSimulation.swift`, `RopeConfiguration.swift`, `RopePoint.swift`, `RopeBead.swift`, `RopeCurve.swift`, `RopeSimulation+Drag.swift`, `RopeSimulation+Beads.swift`, and `RopeSimulation+Snapshot.swift`).

The Windows port (`CharmPhysicsEngine.cs`) must reproduce this specification faithfully without approximation or substitution.

---

## 1. Fundamental Simulation Parameters

| Parameter | Symbol / Field | Exact Value | Unit / Description | Source File |
|---|---|---|---|---|
| Rope segment count | `segmentCount` | `20` | Number of links | `RopeConfiguration.swift:99` |
| Rope node (point) count | `pointCount` | `21` | Number of particles (`segmentCount + 1`) | `RopeConfiguration.swift:90` |
| Default segment rest length | `segmentLength` | `11.0` | Points per link | `RopeConfiguration.swift:100` |
| Fitted length fraction | `Layout.lengthFraction` | `0.69` | Fraction of canvas height | `RopeConfiguration.swift:128` |
| Fitted anchor fraction | `Layout.anchorFraction` | `0.045` | Fraction of canvas height for anchor Y | `RopeConfiguration.swift:131` |
| Gravity acceleration | `gravity` | `2000.0` | Points / second² (positive downward in canvas coords) | `RopeConfiguration.swift:101` |
| Velocity damping factor | `damping` | `0.999` | Energy retained per 240 Hz step | `RopeConfiguration.swift:102` |
| Fixed timestep | `fixedTimeStep` | `1.0 / 240.0` (~`0.004166667`) | Seconds per physics slice (240 Hz) | `RopeConfiguration.swift:107` |
| Maximum frame duration | `maxFrameDuration` | `0.10` | Maximum accumulated time per render step | `RopeConfiguration.swift:108` |
| Maximum node speed | `maximumSpeed` | `6000.0` | Points / second | `RopeConfiguration.swift:109` |
| Maximum reach ratio | `maximumReachRatio` | `0.98` | Fraction of total length cursor may drag | `RopeConfiguration.swift:110` |
| Rest speed threshold | `restSpeed` | `4.0` | Points / second (displacement threshold: `restSpeed * dt`) | `RopeConfiguration.swift:111` |
| Frames before sleep | `framesBeforeSleep` | `60` | Consecutive still frames to enter sleep | `RopeConfiguration.swift:112` |
| Initial release angle | `initialAngle` | `0.38` | Radians from vertical on first spawn | `RopeConfiguration.swift:113` |
| Constraint iterations cap | `constraintIterations` | `256` | Maximum Gauss-Seidel passes per step | `RopeConfiguration.swift:103` |
| Stretch constraint passes | `stretchPasses` | `256` | Maximum inequality projection sweeps per step | `RopeConfiguration.swift:104` |
| Convergence tolerance | `convergenceTolerance` | `0.05` | Points of maximum node correction to stop relaxation | `RopeConfiguration.swift:105` |
| Maximum stretch ratio | `maxStretchRatio` | `1.02` | Hard ceiling on link length (`1.02 * segmentLength`) | `RopeConfiguration.swift:106` |
| Grab padding radius | `Layout.grabPadding` | `10.0` | Extra radius around charm accepting pointer grab | `RopeConfiguration.swift:134` |
| Bead tether stiffness | `beadTetherStiffness` | `0.05` | Fraction of bead displacement from rest restored per step | `RopeSimulation+Beads.swift:20` |
| Bead separation passes | `beadSeparationPasses` | `2` | Number of non-penetration passes per step | `RopeSimulation+Beads.swift:24` |
| Bead slide limit fraction | `slideLimit` | `spacingRadius * 0.6` | Maximum distance a bead may slide from rest | `RopeBead.swift:75` |
| Spline samples per segment | `samplesPerSegment` | `4` | Discretization density for `RopeCurve` quadratic spline | `RopeCurve.swift:42` |

---

## 2. Dynamic Sizing & Canvas Fitting

When canvas size `(width, height)` is updated:
```
usableLength = max(40.0, height * 0.69)
segmentLength = usableLength / 20.0
totalLength = 20.0 * segmentLength
anchor = (x: width / 2.0, y: height * 0.045)
```
If point count differs (never in standard configuration since `segmentCount == 20`), the rope is reset. Otherwise, `rebuildBeads(preservingMotion: true)` is called and the simulation is woken (`wake()`).

---

## 3. Mass & Inverse Mass Distribution

Each node has an `inverseMass` (where `0.0` indicates an unmovable / pinned node):
- **Anchor (Node 0):** `inverseMass = 0.0` (pinned).
- **Internal Nodes (Nodes 1..19):** Base `inverseMass = 1.0`.
- **Charm End Node (Node 20):** Base `inverseMass = 1.0 / max(charmMetrics.mass, 0.0001)`.
- **Bead Weight Load Sharing:**
  Bead masses are distributed onto the nodes when beads are attached (`applyMasses()`):
  ```
  load = array of size 21 initialized to 0.0
  for bead in beads:
      position = clamp(bead.arc / segmentLength, 0.0, 20.0)
      lower = int(position)
      upper = min(lower + 1, 20)
      fraction = position - lower
      load[lower] += bead.mass * (1.0 - fraction)
      load[upper] += bead.mass * fraction

  for index in 1..20 where load[index] > 0:
      base = (index == 20) ? charmMetrics.mass : 1.0
      inverseMass[index] = 1.0 / max(base + load[index], 0.0001)
  ```

---

## 4. Time Accumulator & Substep Execution

```
advanceTime(deltaTime):
    if not isRunning or deltaTime <= 0 or isSleeping:
        lastStepCount = 0
        return

    accumulator = min(accumulator + deltaTime, maxFrameDuration)
    timeStep = 1.0 / 240.0
    stepsTaken = 0

    while accumulator >= timeStep:
        advance(timeStep)
        accumulator -= timeStep
        stepsTaken += 1

    lastStepCount = stepsTaken
    updateSleepState()
```

---

## 5. Step Order of the Physics Solver (`advance`)

Every 240 Hz slice advances through these exact steps in order:
1. **`enforceAnchor()`**:
   ```
   points[0].position = anchor
   points[0].previousPosition = anchor
   ```
2. **`integrate(timeStep)`**:
   ```
   gravityStep = (x: 0, y: gravity * timeStep * timeStep)
   displacementLimit = maximumSpeed * timeStep  // 6000 * (1/240) = 25.0 points

   for index in 1..20 where index != dragIndex:
       if points[index].inverseMass <= 0: continue
       carried = clampLength((points[index].position - points[index].previousPosition) * damping, displacementLimit)
       points[index].previousPosition = points[index].position
       points[index].position += carried + gravityStep
   ```
3. **`driveDraggedPoint(timeStep)`**:
   If `dragIndex` is active (Node 20):
   ```
   travelLimit = maximumSpeed * timeStep  // 25.0 points
   current = points[dragIndex].position
   points[dragIndex].position = current + clampLength(dragTarget - current, travelLimit)
   points[dragIndex].previousPosition = points[dragIndex].position - (dragVelocity * timeStep)
   ```
4. **`solveDistanceConstraints()` (Gauss-Seidel Relaxation)**:
   ```
   relaxations = 0
   residual = infinity
   while relaxations < constraintIterations and residual >= convergenceTolerance:
       residual = 0.0
       for i in 0..19:
           correction = solveLink(i, i + 1, restLength = segmentLength)
           residual = max(residual, correction)
       relaxations += 1
   ```
   **`solveLink(indexA, indexB, restLength)`**:
   ```
   invA = (indexA == dragIndex) ? 0.0 : points[indexA].inverseMass
   invB = (indexB == dragIndex) ? 0.0 : points[indexB].inverseMass
   totalInv = invA + invB
   if totalInv <= 0: return 0.0

   delta = points[indexB].position - points[indexA].position
   distance = length(delta)
   if distance <= 1e-15: return 0.0

   correction = delta * ((distance - restLength) / distance / totalInv)
   points[indexA].position += correction * invA
   points[indexB].position -= correction * invB

   return max(length(correction * invA), length(correction * invB))
   ```
5. **`enforceMaximumStretch()` (Inequality Projection)**:
   ```
   limit = segmentLength * maxStretchRatio  // 11.0 * 1.02 = 11.22

   for pass in 0..<stretchPasses:
       corrected = false
       for i in 0..19:
           if clampLink(i, limit):
               corrected = true
       if not corrected: break
   ```
   **`clampLink(index, limit)`**:
   ```
   invLower = (index == dragIndex) ? 0.0 : points[index].inverseMass
   invUpper = (index + 1 == dragIndex) ? 0.0 : points[index + 1].inverseMass
   totalInv = invLower + invUpper
   if totalInv <= 0: return false

   delta = points[index + 1].position - points[index].position
   distance = length(delta)
   if distance <= limit or distance <= 1e-15: return false

   correction = delta * ((distance - limit) / distance / totalInv)
   points[index].position += correction * invLower
   points[index + 1].position -= correction * invUpper
   return true
   ```
6. **`refreshCord()`**:
   Rebuilds `RopeCurve` through node positions and solves knot intersection.
7. **`advanceBeads(timeStep)`**:
   Advances bead positions along the cord curve.

---

## 6. Dragging and Interaction Model

- **Grab Hit Test:**
  `canGrab(location) = distance(points[20].position, location) <= (charmRadius + 10.0)`
  where `charmRadius = totalLength * charmMetrics.radiusRatio`.
- **Reach Constraint:**
  ```
  reach = totalLength * maximumReachRatio  // totalLength * 0.98
  offset = location - anchor
  distance = length(offset)
  dragTarget = (distance > reach && distance > 1e-15)
      ? anchor + (offset / distance) * reach
      : location
  ```
- **Drag Velocity Clamping:**
  `dragVelocity = clampLength(cursorVelocity, maximumSpeed)` (clamped to 6000 pt/s).
- **Release:**
  Setting `dragIndex = null`, `dragVelocity = (0, 0)`. The velocity previously written into `points[20].previousPosition` naturally carries forward in the next Verlet step.

---

## 7. RopeCurve Geometry and Knot Solving

1. **Quadratic Spline Construction:**
   - Sample 0 = Node 0.
   - For link $i \in [1, 19]$:
     - `control = points[i]`
     - `finish = (points[i] + points[i + 1]) * 0.5`
     - Interpolate 4 quadratic samples with $t \in \{0.25, 0.5, 0.75, 1.0\}$:
       $P(t) = (1 - t)^2 \cdot \text{start} + 2(1 - t)t \cdot \text{control} + t^2 \cdot \text{finish}$
     - `start = finish`
   - Final sample = `charmCenter` (Node 20).
   - Compute cumulative arc length array $L[0 \dots N-1]$.
2. **Knot Boundary (`arc(enteringCircleAround:radius:)`):**
   - Circle center = `charmCenter`, radius = `charmRadius * charmMetrics.knotInset`.
   - Backward search from last sample down to 0: find where segment from $S_{i-1}$ to $S_i$ enters radius.
   - `cordLength` = arc length at that intersection.
   - `cordEnd = point(atArc: cordLength)`.
   - `charmOrientation = atan2(charmCenter.y - cordEnd.y, charmCenter.x - cordEnd.x)`.

---

## 8. Bead Simulation Model

1. **Free Verlet Integration:**
   For each bead $j$:
   ```
   gravityStep = (0, gravity * dt * dt)
   carried = clampLength((bead.position - bead.previousPosition) * damping, maximumSpeed * dt)
   predicted = bead.position + carried + gravityStep
   ```
2. **Projection & Tether Constraint:**
   ```
   restArc = clamp(cordLength - bead.restOffset, 0.0, cordLength)
   window = bead.slideLimit + bead.spacingRadius + segmentLength
   arc = curve.nearestArc(to: predicted, near: bead.arc, window: window)
   arc += (restArc - arc) * beadTetherStiffness  // 0.05
   bead.arc = clamp(arc, restArc - bead.slideLimit, restArc + bead.slideLimit)
   ```
3. **Bead Separation Passes (2 passes):**
   ```
   last = beads.count - 1
   beads[last].arc = min(beads[last].arc, cordLength - beads[last].spacingRadius)
   for i in stride(from: last - 1, through: 0, by: -1):
       minGap = beads[i].spacingRadius + beads[i + 1].spacingRadius
       gap = beads[i + 1].arc - beads[i].arc
       if gap < minGap:
           beads[i].arc -= (minGap - gap)
   beads[0].arc = max(beads[0].arc, beads[0].spacingRadius)
   ```
4. **Position & Angle Update:**
   ```
   bead.previousPosition = bead.position
   bead.position = curve.point(atArc: bead.arc)
   bead.angle = curve.angle(atArc: bead.arc)
   ```

---

## 9. Sleeping & Waking State Machine

- **Speed check per step:**
  `speedLimit = restSpeed * fixedTimeStep` = $4.0 \times \frac{1}{240} = \frac{1}{60} \approx 0.0166667\text{ points}$.
- A frame is *still* if:
  - `dragIndex == null`, and
  - all rope points have $\text{length}(\text{position} - \text{previousPosition}) \le \text{speedLimit}$, and
  - all beads have $\text{length}(\text{position} - \text{previousPosition}) \le \text{speedLimit}$.
- If still, `stillFrames += 1`. If `stillFrames >= 60`, `isSleeping = true`.
- If moving or dragging, `stillFrames = 0`.
- While `isSleeping == true`:
  - `step(deltaTime)` immediately returns without executing substeps.
  - No new render snapshots are published.
  - Display polling throttles from 120 Hz to 30 Hz.
- Waking (`wake()`): resets `isSleeping = false`, `stillFrames = 0`. Triggered by pointer grab, resizing, setting charm metrics/beads, or importing.

---

## 10. Sound Synthesis Model

Mono 44.1 kHz 32-bit float PCM:
- Struck materials: `Bell`, `Wood`, `Glass`, `Metal`, `Soft`.
- Voice: fundamental frequency + array of partials (frequency ratio, relative amplitude, exponential decay constant $\tau$).
- Render equation for sample index $n$ at time $t = n / 44100$:
  $$\text{attack}(t) = \min(1.0, t / t_{\text{attack}})$$
  $$\text{fade}(t) = \min(1.0, (t_{\text{duration}} - t) / t_{\text{release}})$$
  $$\text{envelope}(t) = \text{attack}(t) \cdot \max(0.0, \text{fade}(t))$$
  $$S(t) = \text{envelope}(t) \cdot \sum_{k} A_k e^{-t / \tau_k} \sin(2\pi f_0 r_k t)$$
- Deterministic LCG noise for percussive attack (`Wood` and `Soft`):
  $$\text{state} = \text{state} \times 1664525 + 1013904223 \pmod{2^{32}}$$
  Initial seed = `0x9E3779B9`. One-pole low-pass filter: $\text{filtered} += \alpha (\text{white} - \text{filtered})$.
- Peak normalized to: Bell 0.85, Glass 0.70, Metal 0.75, Wood 0.80, Soft 0.50.
- Played only on release if $\text{velocity} > 500\text{ pt/s}$ with intensity:
  $$\text{intensity} = \text{clamp}\left(\frac{\text{velocity} - 500.0}{2600.0}, 0.25, 1.0\right)$$
