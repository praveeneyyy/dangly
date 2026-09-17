//
//  Secret.swift
//  Hangly
//
//  The things the About window will tell you if you ask it enough times.
//

import Foundation

/// One thing the app admits to.
struct Secret: Hashable, Sendable {
    enum Rarity: Hashable, Sendable {
        case common
        case rare
        case ultraRare
    }

    /// Shown above the message, in the emphasised face. The common secrets have none.
    var title: String?

    var message: String

    /// A signature, for the one secret that has earned one.
    var attribution: String?

    var rarity: Rarity

    init(title: String? = nil, message: String, attribution: String? = nil, rarity: Rarity = .common) {
        self.title = title
        self.message = message
        self.attribution = attribution
        self.rarity = rarity
    }
}

/// Picks a secret, and remembers enough not to be boring about it.
///
/// The roll is a parameter rather than something this reaches for itself, which is
/// what makes a one-in-a-thousand outcome testable: a test asks for the roll it
/// wants and gets the secret that roll should produce. `reveal()` supplies real
/// randomness for the app.
struct SecretVault: Equatable {
    /// Chance of the ultra-rare secret, per reveal.
    static let ultraRareProbability = 0.001

    /// Chance of the rare secret, per reveal.
    static let rareProbability = 0.01

    static let rare = Secret(
        title: "Achievement Unlocked",
        message: "You found the rare secret.",
        rarity: .rare
    )

    static let ultraRare = Secret(
        title: "There is no secret.",
        message: "You just really like clicking buttons.",
        rarity: .ultraRare
    )

    static let common: [Secret] = [
        Secret(message: "The charm believes in you."),
        Secret(message: "No charms were harmed during testing."),
        Secret(message: "This rope has survived more swings than most relationships."),
        Secret(message: "Physics simulation: 97%. The remaining 3% is hope."),
        Secret(message: "Every swing is calculated. The luck is not."),
        Secret(message: "The rope knows where it is. The rope knows where it isn't."),
        Secret(message: "You're supposed to be working right now."),
        Secret(message: "This app began as: \"What if desktop icons needed emotional support?\""),
        Secret(message: "Warning: Excessive charm staring may reduce productivity."),
        Secret(message: "Today's luck level: ████████░░"),
        Secret(message: "Your charm has been silently judging your desktop organization."),
        Secret(message: "The charm has witnessed every tab you've left open."),
        Secret(message: "The physics engine is working harder than it looks."),
        Secret(message: "Gravity is doing most of the work."),
        Secret(message: "Somewhere, a charm is swinging perfectly.")
    ]

    /// How many secrets have been revealed since the app started.
    private(set) var revealedCount = 0

    /// The last common secret shown, so the next one can avoid it.
    private(set) var lastCommon: Secret?

    /// Reveals a secret using real randomness.
    mutating func reveal() -> Secret {
        reveal(rarityRoll: .random(in: 0..<1), selection: .random(in: 0..<1))
    }

    /// Reveals a secret from two rolls.
    ///
    /// - Parameters:
    ///   - rarityRoll: In `0..<1`. Decides which tier the secret comes from.
    ///   - selection: In `0..<1`. Chooses within the common secrets.
    /// - Returns: The secret to show.
    ///
    /// A common secret never immediately repeats: the one just shown is taken out of
    /// the running, and `selection` chooses from what is left. The two rare secrets
    /// are exempt — they are rare enough that seeing one twice in a row is a story
    /// rather than a repetition, and excluding them would quietly bend the odds the
    /// rest of this type exists to keep honest.
    mutating func reveal(rarityRoll: Double, selection: Double) -> Secret {
        revealedCount += 1

        if rarityRoll < Self.ultraRareProbability {
            return Self.ultraRare
        }
        if rarityRoll < Self.ultraRareProbability + Self.rareProbability {
            return Self.rare
        }

        let candidates = Self.common.filter { $0 != lastCommon }
        let pool = candidates.isEmpty ? Self.common : candidates
        guard !pool.isEmpty else { return Self.rare }

        let index = min(pool.count - 1, max(0, Int(selection * Double(pool.count))))
        let secret = pool[index]
        lastCommon = secret
        return secret
    }
}

extension Character {
    /// Whether this is one of Unicode's Block Elements — the shaded rectangles a
    /// progress bar can be spelled with.
    var isBlockElement: Bool {
        guard let scalar = unicodeScalars.first, unicodeScalars.count == 1 else { return false }
        return (0x2580...0x259F).contains(Int(scalar.value))
    }
}
