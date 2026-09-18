//
//  RopeSimulationTests.swift
//  HanglyTests
//

import CoreGraphics
import Foundation
import Testing

@testable import Hangly

/// Exercises the solver directly. The rope is AppKit-free, so every claim about it
/// can be checked numerically rather than by watching the screen.
@Suite("Rope simulation")
@MainActor
struct RopeSimulationTests {
    private let anchor = CGPoint(x: 260, y: 15)
    private let frame120: TimeInterval = 1.0 / 120.0

    private func makeRope() -> RopeSimulation {
        let rope = RopeSimulation(configuration: .default, anchor: anchor)
        rope.start()
        return rope
    }

    /// Advances `seconds` of simulated time in 120 Hz frames.
    private func run(_ rope: RopeSimulation, seconds: TimeInterval) {
        for _ in 0..<Int(seconds / frame120) {
            rope.step(deltaTime: frame120)
        }
    }

    // MARK: - Structure

    @Test("Twenty segments means twenty-one nodes")
    func segmentCountMatchesSpecification() {
        let rope = makeRope()

        #expect(rope.configuration.segmentCount == 20)
        #expect(rope.points.count == 21)
    }

    @Test("The first node is pinned and the charm is the heaviest")
    func massDistributionIsCorrect() {
        let rope = makeRope()

        #expect(rope.points[0].isPinned)
        #expect(rope.points[0].inverseMass == 0)
        // A heavier charm has a smaller inverse mass than a plain node.
        #expect(rope.points[20].inverseMass < rope.points[10].inverseMass)
        #expect(rope.points[10].inverseMass == 1)
    }

    @Test("The anchor never moves, however hard the rope is driven")
    func anchorStaysPinned() {
        let rope = makeRope()
        rope.beginDrag(at: rope.points[20].position)
        rope.updateDrag(to: CGPoint(x: 4000, y: -4000), velocity: CGPoint(x: 9000, y: -9000))
        run(rope, seconds: 2)

        #expect(rope.points[0].position == anchor)
    }

    // MARK: - Inextensibility

    @Test("The rope never stretches beyond its limit at rest")
    func doesNotStretchAtRest() {
        let rope = makeRope()
        run(rope, seconds: 5)

        #expect(rope.measuredMaximumStretch <= rope.configuration.maxStretchRatio + 1e-9)
    }

    @Test("The rope never stretches beyond its limit under a hard flick")
    func doesNotStretchUnderHardFlick() {
        let rope = makeRope()
        run(rope, seconds: 1)
        rope.beginDrag(at: rope.points[20].position)

        var worstStretch = 0.0
        for tick in 0..<600 {
            let angle = sin(Double(tick) * 0.1) * 0.3
            let target = anchor + CGPoint(x: sin(angle) * 200.0, y: cos(angle) * 200.0)
            rope.updateDrag(to: target, velocity: CGPoint(x: cos(angle) * 600.0, y: 0))
            rope.step(deltaTime: frame120)
            worstStretch = max(worstStretch, rope.measuredMaximumStretch)
        }

        #expect(worstStretch <= rope.configuration.maxStretchRatio + 1e-9)
    }

    @Test("Free swinging produces essentially no stretch")
    func doesNotStretchWhileSwinging() {
        let rope = makeRope()
        run(rope, seconds: 1)

        var worstStretch = 0.0
        for _ in 0..<2400 {
            rope.step(deltaTime: frame120)
            worstStretch = max(worstStretch, rope.measuredMaximumStretch)
        }

        #expect(worstStretch < 1.01)
    }

    @Test("Synthetic torture stays bounded and recovers")
    func recoversFromUnreachableInput() {
        let rope = makeRope()
        run(rope, seconds: 1)
        rope.beginDrag(at: rope.points[20].position)

        var worstStretch = 0.0
        for tick in 0..<600 {
            let angle = Double(tick) * 0.35
            let target = anchor + (CGPoint(x: cos(angle), y: sin(angle)) * 900)
            rope.updateDrag(to: target, velocity: CGPoint(x: 6000, y: 6000))
            rope.step(deltaTime: frame120)
            worstStretch = max(worstStretch, rope.measuredMaximumStretch)
        }
        #expect(worstStretch > 1.5)

        rope.endDrag()
        run(rope, seconds: 2)
        #expect(rope.measuredMaximumStretch <= rope.configuration.maxStretchRatio + 1e-9)
    }

    @Test("String stretches significantly on drag and oscillates back on release")
    func stringStretchesOnDragAndOscillatesOnRelease() {
        let rope = makeRope()
        run(rope, seconds: 2)
        rope.beginDrag(at: rope.points[20].position)

        let stretchedTarget = anchor + CGPoint(x: 0, y: 500)
        for _ in 0..<30 {
            rope.updateDrag(to: stretchedTarget, velocity: .zero)
            rope.step(deltaTime: frame120)
        }

        #expect(rope.measuredMaximumStretch > 2.0)

        rope.endDrag()
        rope.step(deltaTime: frame120)
        #expect(rope.points[20].displacement.y < 0)

        run(rope, seconds: 3)
        #expect(rope.measuredMaximumStretch <= rope.configuration.maxStretchRatio + 1e-9)
    }

    // MARK: - Gravity and damping

    @Test("Gravity swings the rope down below its anchor")
    func gravityPullsTheRopeDown() {
        let rope = makeRope()
        let startX = rope.points[20].position.x
        run(rope, seconds: 20)
        let charm = rope.points[20].position

        // It starts off-vertical by `initialAngle` and must end hanging under the anchor.
        #expect(abs(charm.x - anchor.x) < abs(startX - anchor.x) * 0.25)
        #expect(charm.y > anchor.y + (rope.configuration.totalLength * 0.9))
    }

    @Test("Damping brings the rope to rest")
    func dampingSettlesTheRope() {
        let rope = makeRope()
        run(rope, seconds: 3)
        let movingSpeed = totalSpeed(of: rope)

        run(rope, seconds: 25)
        let settledSpeed = totalSpeed(of: rope)

        #expect(settledSpeed < movingSpeed * 0.1)
    }

    private func totalSpeed(of rope: RopeSimulation) -> Double {
        rope.points.reduce(0) { $0 + $1.displacement.magnitude }
    }

    // MARK: - Frame-rate independence

    @Test("Two 120 Hz frames match one 60 Hz frame exactly")
    func fixedTimeStepMakesRefreshRateIrrelevant() {
        let fast = RopeSimulation(configuration: .default, anchor: anchor)
        let slow = RopeSimulation(configuration: .default, anchor: anchor)
        fast.start()
        slow.start()

        for _ in 0..<120 {
            fast.step(deltaTime: 1.0 / 120.0)
            fast.step(deltaTime: 1.0 / 120.0)
            slow.step(deltaTime: 1.0 / 60.0)
        }

        for index in fast.points.indices {
            #expect(abs(fast.points[index].position.x - slow.points[index].position.x) < 1e-9)
            #expect(abs(fast.points[index].position.y - slow.points[index].position.y) < 1e-9)
        }
    }

    @Test("A long 120 Hz run stays finite and bounded")
    func remainsStableOverALongRun() {
        let rope = makeRope()
        run(rope, seconds: 120)

        let reach = rope.configuration.totalLength * 3
        for point in rope.points {
            #expect(point.position.x.isFinite)
            #expect(point.position.y.isFinite)
            #expect(point.position.distance(to: anchor) < reach)
        }
    }

    @Test("A stalled frame cannot trigger a burst of catch-up steps")
    func clampsOversizedFrames() {
        let rope = makeRope()
        rope.step(deltaTime: 10)

        let ceiling = Int(rope.configuration.maxFrameDuration / rope.configuration.fixedTimeStep) + 1
        #expect(rope.lastStepCount <= ceiling)
    }
}

