//
//  RopeInteractionTests.swift
//  HanglyTests
//

import CoreGraphics
import Foundation
import Testing

@testable import Hangly

/// Interaction and lifecycle: grabbing, releasing, resting and resizing.
@Suite("Rope interaction")
@MainActor
struct RopeInteractionTests {
    private let anchor = CGPoint(x: 260, y: 15)
    private let frame120: TimeInterval = 1.0 / 120.0

    private func makeRope() -> RopeSimulation {
        let rope = RopeSimulation(configuration: .default, anchor: anchor)
        rope.start()
        return rope
    }

    private func run(_ rope: RopeSimulation, seconds: TimeInterval) {
        for _ in 0..<Int(seconds / frame120) {
            rope.step(deltaTime: frame120)
        }
    }

    // MARK: - Dragging

    @Test("Only the charm can be grabbed")
    func grabIsLimitedToTheCharm() {
        let rope = makeRope()
        run(rope, seconds: 5)

        #expect(rope.canGrab(at: rope.points[20].position))
        #expect(!rope.canGrab(at: anchor))
        #expect(!rope.canGrab(at: rope.points[10].position))
        #expect(rope.beginDrag(at: anchor) == false)
        #expect(rope.isDragging == false)
    }

    @Test("A held charm settles exactly on the cursor")
    func draggingMovesTheCharmToTheCursor() {
        let rope = makeRope()
        run(rope, seconds: 5)
        rope.beginDrag(at: rope.points[20].position)

        // The held node follows at a bounded speed rather than teleporting, so it
        // takes a few frames to cover a jump this large. At pointer speeds the limit
        // is never reached and tracking is frame-exact.
        let target = CGPoint(x: anchor.x + 120, y: anchor.y + 140)
        for _ in 0..<10 {
            rope.updateDrag(to: target, velocity: .zero)
            rope.step(deltaTime: frame120)
        }

        #expect(rope.points[20].position.distance(to: target) < 1e-6)
    }

    @Test("Releasing the charm preserves its momentum")
    func releasePreservesMomentum() {
        let rope = makeRope()
        run(rope, seconds: 5)
        rope.beginDrag(at: rope.points[20].position)

        // Sweep along the arc the rope can actually reach, so the motion is a real
        // swing rather than a stretched cord waiting to snap back.
        let radius = rope.configuration.totalLength * 0.9
        let speed = 900.0
        let angularStep = (speed / radius) * frame120
        var angle = Double.pi / 2

        for _ in 0..<16 {
            angle -= angularStep
            let target = anchor + (CGPoint(x: cos(angle), y: sin(angle)) * radius)
            let tangent = CGPoint(x: sin(angle), y: -cos(angle)) * speed
            rope.updateDrag(to: target, velocity: tangent)
            rope.step(deltaTime: frame120)
        }

        let held = rope.points[20].displacement / frame120
        rope.endDrag()
        rope.step(deltaTime: frame120)
        let released = rope.points[20].displacement / frame120

        // It must keep most of its speed, and keep going the same way.
        #expect(released.magnitude > held.magnitude * 0.5)
        #expect((released.x * held.x) + (released.y * held.y) > 0)
        #expect(released.x > 0)
    }

    @Test("Releasing a stationary charm adds no momentum")
    func releaseWithoutMotionDoesNotLaunchTheCharm() {
        let rope = makeRope()
        run(rope, seconds: 5)
        rope.beginDrag(at: rope.points[20].position)

        // Held still, well inside the rope's reach.
        let held = anchor + (CGPoint(x: 0.6, y: 0.8) * (rope.configuration.totalLength * 0.9))
        for _ in 0..<60 {
            rope.updateDrag(to: held, velocity: .zero)
            rope.step(deltaTime: frame120)
        }
        rope.endDrag()
        rope.step(deltaTime: frame120)

        // It should begin falling under gravity, not shoot sideways.
        let speed = rope.points[20].displacement.magnitude / frame120
        #expect(speed < 200)
    }

    // MARK: - Idling

    @Test("The rope stops simulating once it has settled")
    func sleepsWhenSettled() {
        let rope = makeRope()
        #expect(rope.isSleeping == false)

        run(rope, seconds: 40)

        #expect(rope.isSleeping)
        #expect(rope.lastStepCount == 0)
    }

    @Test("A settled rope stays exactly where it stopped")
    func sleepingRopeDoesNotDrift() {
        let rope = makeRope()
        run(rope, seconds: 40)
        let resting = rope.points.map(\.position)

        run(rope, seconds: 20)

        for (index, position) in rope.points.map(\.position).enumerated() {
            #expect(position == resting[index])
        }
    }

    @Test("Grabbing the charm wakes the rope")
    func draggingWakesTheRope() {
        let rope = makeRope()
        run(rope, seconds: 40)
        #expect(rope.isSleeping)

        rope.beginDrag(at: rope.points[20].position)

        #expect(rope.isSleeping == false)
        rope.updateDrag(to: rope.points[20].position + CGPoint(x: 40, y: 0), velocity: CGPoint(x: 400, y: 0))
        rope.step(deltaTime: frame120)
        #expect(rope.lastStepCount > 0)
    }

    @Test("Moving the anchor wakes the rope")
    func resizingWakesTheRope() {
        let rope = makeRope()
        run(rope, seconds: 40)
        #expect(rope.isSleeping)

        rope.resize(to: CGSize(width: 700, height: 300))

        #expect(rope.isSleeping == false)
    }

    // MARK: - Reduce Motion

    @Test("The rest pose hangs straight down with no motion")
    func restPoseIsStill() {
        let rope = makeRope()
        rope.resetToHanging()

        for point in rope.points {
            #expect(abs(point.position.x - anchor.x) < 1e-9)
            #expect(point.displacement.magnitude < 1e-9)
        }
        #expect(rope.points[20].position.y > anchor.y + (rope.configuration.totalLength * 0.99))

        // It stays put and goes to sleep on its own.
        run(rope, seconds: 2)
        #expect(rope.isSleeping)
        #expect(abs(rope.points[20].position.x - anchor.x) < 0.01)
    }

    // MARK: - Resizing

    @Test("Resizing re-anchors and re-fits without destroying motion")
    func resizingKeepsTheRopeAlive() {
        let rope = makeRope()
        run(rope, seconds: 1)

        rope.resize(to: CGSize(width: 1040, height: 600))

        #expect(rope.anchor.x == 520)
        #expect(rope.points.count == 21)
        #expect(rope.configuration.segmentCount == 20)

        run(rope, seconds: 3)
        #expect(rope.measuredMaximumStretch <= rope.configuration.maxStretchRatio + 1e-9)
    }
}
