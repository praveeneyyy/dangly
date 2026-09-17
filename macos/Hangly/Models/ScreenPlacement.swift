//
//  ScreenPlacement.swift
//  Hangly
//
//  Pure geometry for positioning the overlay. Deliberately AppKit-free.
//

import CoreGraphics

/// Computes the overlay's frame inside a screen's usable bounds.
///
/// Kept free of AppKit so the placement rules can be unit-tested on any machine,
/// with no attached display and no window server.
///
/// All rectangles use AppKit's coordinate convention: the origin is bottom-left and
/// `y` grows upward, so the "top" of a rect is `maxY`.
enum ScreenPlacement {
    /// - Parameters:
    ///   - size: Desired overlay size in points.
    ///   - anchor: Which corner/edge to hang from.
    ///   - bounds: The screen region to place within, in global screen coordinates.
    ///   - offset: User nudge. `x` positive moves right, `y` positive moves *down*.
    ///   - edgeInset: Margin kept between the overlay and the screen edges.
    /// - Returns: A frame in global screen coordinates, clamped to `bounds` when it fits.
    static func frame(
        for size: CGSize,
        anchor: OverlayAnchor,
        in bounds: CGRect,
        offset: CGPoint = .zero,
        edgeInset: CGFloat = 0
    ) -> CGRect {
        let originX: CGFloat
        switch anchor.horizontal {
        case .leading:
            originX = bounds.minX + edgeInset
        case .center:
            originX = bounds.midX - (size.width / 2)
        case .trailing:
            originX = bounds.maxX - size.width - edgeInset
        }

        // Top-anchored: the overlay's top edge sits just under the top of `bounds`.
        let originY = bounds.maxY - size.height - edgeInset

        let proposed = CGRect(
            x: originX + offset.x,
            y: originY - offset.y,
            width: size.width,
            height: size.height
        )

        return clamp(proposed, within: bounds)
    }

    /// Keeps `rect` fully inside `bounds` when it is small enough to fit.
    /// Oversized rects are returned untouched so the caller can decide what to do.
    static func clamp(_ rect: CGRect, within bounds: CGRect) -> CGRect {
        guard rect.width <= bounds.width, rect.height <= bounds.height else { return rect }

        let x = min(max(rect.minX, bounds.minX), bounds.maxX - rect.width)
        let y = min(max(rect.minY, bounds.minY), bounds.maxY - rect.height)
        return CGRect(x: x, y: y, width: rect.width, height: rect.height)
    }
}
