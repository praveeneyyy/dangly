//
//  StudioAdjustments.swift
//  Hangly
//
//  Everything the user can change about an import in the Studio.
//

import Foundation

/// How the background is removed.
enum SubjectRemoval: Hashable, Sendable {
    /// Existing alpha, then a detected subject, then a flat-background fill.
    case automatic
    /// A subject found by Vision. `nil` keeps every detected instance.
    case detectedSubject(instance: Int?)
    /// A corner flood fill with the given per-channel tolerance.
    case flatBackground(tolerance: Int)
    /// The image exactly as it is.
    case keepOriginal

    /// Coarse identity for the method picker, ignoring the parameters.
    enum Kind: Hashable, CaseIterable, Sendable {
        case automatic
        case detectedSubject
        case flatBackground
        case keepOriginal

        var title: String {
            switch self {
            case .automatic: "Automatic"
            case .detectedSubject: "Detected subject"
            case .flatBackground: "Flat background"
            case .keepOriginal: "Keep original"
            }
        }
    }

    var kind: Kind {
        switch self {
        case .automatic: .automatic
        case .detectedSubject: .detectedSubject
        case .flatBackground: .flatBackground
        case .keepOriginal: .keepOriginal
        }
    }

    static let defaultTolerance = 36
}

/// The complete, undoable state of a charm being made.
///
/// A plain value so an undo step is a copy and a comparison, nothing more.
struct StudioAdjustments: Equatable, Sendable {
    var removal: SubjectRemoval = .automatic

    /// Display name. Empty means "derive one from the file".
    var name = ""

    /// Radius as a fraction of the rope's length.
    var sizeRatio = 0.12

    /// Multiplier on the mass the analysis derives from the image.
    var weightScale = 1.0

    /// Fraction of the square the subject spans; the rest is margin for the shadow.
    var fill = 0.92

    static let sizeRange: ClosedRange<Double> = 0.08...0.16
    static let weightRange: ClosedRange<Double> = 0.5...2.0
    static let fillRange: ClosedRange<Double> = 0.70...0.98
    static let toleranceRange: ClosedRange<Int> = 8...96

    /// Returns a copy with every number inside its range.
    func clamped() -> StudioAdjustments {
        var copy = self
        copy.sizeRatio = sizeRatio.clamped(to: Self.sizeRange)
        copy.weightScale = weightScale.clamped(to: Self.weightRange)
        copy.fill = fill.clamped(to: Self.fillRange)
        if case .flatBackground(let tolerance) = removal {
            copy.removal = .flatBackground(tolerance: tolerance.clamped(to: Self.toleranceRange))
        }
        return copy
    }
}
