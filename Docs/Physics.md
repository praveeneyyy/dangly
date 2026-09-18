# The physics engine

How Dangly's rope is simulated: a Verlet solver with position-based constraints,
running at a fixed rate independent of the display.

Part of the [architecture documentation](Architecture.md).

---

## The rope

### Why Verlet

Verlet stores no velocity. A node's velocity is implied by the gap between where it
is and where it was. Three consequences shape the whole design:

- **Momentum after release is free.** Letting go of the charm simply stops writing
  its position; the gap the drag left behind *is* its velocity.
- **Constraints are positional.** Satisfying a link means moving nodes, with no force
  to integrate and no stiffness term to tune into instability.
- **It is stable at large constraint counts**, which an explicit spring solver is not.

### Step order

```
enforce anchor  →  integrate  →  drive held node  →  relax  →  clamp stretch
```

The anchor is pinned first so a moved anchor drags the rope this step rather than
next, which is what makes resizing the overlay look physical.

### Fixed timestep

Physics advances in fixed 240 Hz slices; the display only decides how often
`step(deltaTime:)` is called. At 120 Hz that is two slices per frame, at 60 Hz four.
Behaviour is therefore *identical* across refresh rates, which is asserted directly:
two 120 Hz frames match one 60 Hz frame to within 1e-9.

The accumulator is clamped, so a stall or a wake from sleep cannot trigger a burst of
catch-up steps that would look like the rope teleporting.

### Inextensibility & Dynamic Drag Stretch

Dangly combines realistic hanging cord physics with playful interactive elasticity:

1. **Resting & Free-Swinging Inextensibility**: When free-hanging or swinging naturally, the cord maintains strict inextensibility within a 1.02 stretch ceiling via adaptive Gauss-Seidel relaxation and one-sided inequality projection passes.
2. **Dynamic Drag Stretch**: When actively dragging the charm, the cord becomes elastically stretchable all the way down to the bottom of the screen. As the drag target extends past the nominal cord length, the effective segment length scales smoothly ($L_{\text{drag}} = D / N$), allowing intermediate nodes and beads to space out naturally.
3. **Snappy Release Recoil & Pendulum Oscillation**: Upon releasing the drag (`endDrag`), stored elastic strain rapidly retracts the effective rest length back to nominal via exponential spring damping ($\sim 22\text{ s}^{-1}$). This positional retraction naturally converts into upward Verlet momentum, causing the charm to overshoot its resting position and trigger dynamic swinging oscillations ("moving and moving") until damping brings it gently to rest.

Measured worst-case link stretch:

| Input | Worst stretch |
|---|---|
| Free swinging | 1.0009 |
| Hard flick, 3000 pt/s, reversing, yanked past reach | 1.0115 |
| Synthetic torture, ~7 revolutions per second | 1.027 |

The first two are within the 1.02 ceiling and are asserted as such. The third exceeds
it briefly and is asserted only to stay bounded and recover, because no pointer can
produce it and relaxation cannot fully converge inside one frame at that rate.

### Sleeping

A settled rope is indistinguishable from a still image, so the solver stops. After
half a second with every node below the rest speed, `step` becomes a no-op, the view
model stops publishing snapshots — so Observation never fires and SwiftUI never
redraws — and the display link drops from 120 to 30 per second. It still ticks,
because the same tick polls the cursor for a grab.

Measured on a 120 Hz display:

| State | CPU |
|---|---|
| Swinging | ~14% of one core |
| Settled | ~0.6% of one core |

Grabbing the charm, moving the anchor or resizing wakes it.

### Interaction and click-through

AppKit cannot pass a click through part of a window and keep the rest, so
`ignoresMouseEvents` is toggled on the whole panel once per frame based on whether the
cursor is within the charm's grab radius. `NSEvent.mouseLocation` is polled rather
than monitored: it needs no event tap and therefore no Accessibility permission. The
assignment is guarded on change, because writing it unconditionally every frame talks
to the window server often enough to keep a settled overlay measurably busy.

SwiftUI's side is narrowed by a `contentShape` of the same disc, so the gesture only
fires on the charm.


---

## Beads

Charms hang on a cord with beads threaded above them, and those beads are
simulated too. The rules they follow, and why the bead pass cannot disturb the
rope, are described in [the charm system](Charm-System.md#beads-on-the-cord).
