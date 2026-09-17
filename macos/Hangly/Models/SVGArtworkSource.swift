//
//  SVGArtworkSource.swift
//  Hangly
//
//  Resolves a charm to its SVG asset, and reports the ones that are missing.
//

import AppKit
import Foundation

/// Where the collection's SVG artwork is loaded from.
///
/// In the app it is the asset catalog, where each charm is an imageset that keeps
/// its vector data and may carry a dark variant. The build-time scripts point it at
/// the designer's `Assets/Charms` folder instead, via an environment variable, so
/// previews are rendered from the very files that will be bundled.
struct SVGArtworkSource: Sendable {
    enum Backend: Sendable, Equatable {
        case bundle
        case directory(URL)
    }

    /// Set to a folder of SVGs to bypass the asset catalog.
    static let environmentKey = "DANGLY_CHARM_SVG_DIR"

    let backend: Backend

    /// The environment override if present, otherwise the bundle.
    ///
    /// Production ignores the override and only ever reads the bundle: a shipped app
    /// has no business loading its artwork from a path someone can point at it.
    static func resolveDefault() -> SVGArtworkSource {
        #if !HANGLY_PRODUCTION
        let env = ProcessInfo.processInfo.environment
        if let path = env[environmentKey] ?? env["HANGLY_CHARM_SVG_DIR"], !path.isEmpty {
            return SVGArtworkSource(backend: .directory(URL(fileURLWithPath: path)))
        }
        #endif
        return SVGArtworkSource(backend: .bundle)
    }

    /// Asset-catalog name for a charm's artwork.
    static func assetName(for kind: CharmKind) -> String {
        "charm-\(kind.rawValue)"
    }

    /// The vector image for a charm, or `nil` when it has no artwork here.
    func vectorImage(for kind: CharmKind) -> VectorImage? {
        switch backend {
        case .bundle:
            let image = VectorImage(source: .named(Self.assetName(for: kind)))
            return image.isAvailable ? image : nil
        case .directory(let directory):
            guard let fileName = CollectionCharmCatalog.sourceFileName(for: kind) else { return nil }
            let url = directory.appending(path: fileName)
            guard FileManager.default.fileExists(atPath: url.path) else { return nil }
            let image = VectorImage(source: .file(url))
            return image.isAvailable ? image : nil
        }
    }

    func isAvailable(_ kind: CharmKind) -> Bool {
        vectorImage(for: kind) != nil
    }

    /// The charms among `kinds` that have no artwork in this source.
    func missingAssets(among kinds: [CharmKind]) -> [CharmKind] {
        kinds.filter { !isAvailable($0) }
    }
}
