//
//  RopeSimulation+Drag.swift
//  Hangly
//
//  Picking the charm up, moving it and letting it go.
//

import CoreGraphics
import Foundation

/// Dragging, which is input handling rather than physics: it decides what the
/// solver is asked to do with the final node, and the solver decides what the rope
/// does about it.
@MainActor
extension RopeSimulation {
    /// Grabs the charm if `location` is within its grab radius.
    /// - Returns: `true` when the drag was accepted.
    @discardableResult
    func beginDrag(at location: CGPoint) -> Bool {
        guard let index = points.indices.last else { return false }
        guard canGrab(at: location) else { return false }

        dragIndex = index
        dragTarget = location
        dragVelocity = .zero
        wake()
        return true
    }

    /// Whether a grab at `location` would hit the charm.
    func canGrab(at location: CGPoint) -> Bool {
        guard let charm = points.last else { return false }
        let radius = charmRadius + RopeConfiguration.Layout.grabPadding
        return charm.position.distance(to: location) <= radius
    }

    /// - Parameter velocity: Cursor velocity in points per second.
    func updateDrag(to location: CGPoint, velocity: CGPoint) {
        guard dragIndex != nil else { return }
        dragTarget = reachableTarget(for: location)
        dragVelocity = velocity.limited(to: configuration.maximumSpeed)
    }

    /// When dragging, allows the cord to stretch freely downward to the bottom of the canvas.
    /// Clamping only prevents the charm from inverting above the anchor ceiling.
    func reachableTarget(for location: CGPoint) -> CGPoint {
        if dragIndex != nil {
            let clampedY = max(location.y, anchor.y + 5.0)
            return CGPoint(x: location.x, y: clampedY)
        }

        let reach = configuration.totalLength * configuration.maximumReachRatio
        let offset = location - anchor
        let distance = offset.magnitude
        guard distance > reach, distance > .ulpOfOne else { return location }
        return anchor + ((offset / distance) * reach)
    }

    /// Releases the charm. The velocity written during the final step stays in the
    /// node's history, so the rope carries on at the speed it was thrown, oscillating
    /// back through elastic recoil.
    func endDrag() {
        dragIndex = nil
        dragVelocity = .zero
        wake()
    }
}
