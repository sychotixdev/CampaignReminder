using System.Collections.Generic;

namespace CampaignReminder;

/// <summary>
/// Provides the default campaign zone data (area names + Vaal Beacon rewards).
/// Called once on first load when Settings.Acts is empty.
/// </summary>
public static class CampaignData
{
    // Helper: build an AreaSettings entry. Zones with no beacon get showReward=false.
    private static AreaSettings Z(string name, string reward, bool highlightReward = false, string notes = "") => new()
    {
        AreaName = name,
        LeagueReward = reward,
        // Auto-disable display for zones that have no beacon or explicitly empty reward
        HighlightLeagueReward = highlightReward && !string.IsNullOrEmpty(reward),
        Notes = notes,
    };

    public static List<ActSettings> GetDefaultActs() =>
    [
        // ── Act 1 ────────────────────────────────────────────────────────────
        new ActSettings
        {
            ActName = "Act 1",
            Areas =
            [
                Z("Clearfell Encampment",       "", notes: "Check vendors"),
                Z("Clearfell",                  "Orb of Transmutation", notes: "Beira-+10% Cold Res"),
                Z("Mud Burrow",                 "Orb of Augmentation"),
                Z("The Grelwood",               "Orb of Transmutation"),
                Z("The Red Vale",               "Uncut Skill Gem (Level 2)"),
                Z("The Grim Tangle",            "Uncut Skill Gem (Level 3)"),
                Z("Cemetery of the Eternals",   "Regal Orb", true),
                Z("Tomb of the Consort",        "Normal Amulet"),
                Z("Mausoleum of the Praetor",   "Lesser Rune"),
                Z("Hunting Grounds",            "Exalted Orb", notes: "Crowbell- 2 passives, remember Freythorn"),
                Z("Freythorn",                  "Uncut Support Gem (Level 1)", notes: "King-+30 spirit"),
                Z("Ogham Farmlands",            "Uncut Skill Gem (Level 4)", notes: "Lute-+2 passives"),
                Z("Ogham Village",              "Artificer's Orb", true, notes:"smithing tools"),
                Z("Manor Ramparts",             "Uncut Skill Gem (Level 5)"),
                Z("Ogham Manor",                "Orb of Alchemy", notes:"Candlemass-+20 life"),
            ]
        },

        // ── Act 2 ────────────────────────────────────────────────────────────
        new ActSettings
        {
            ActName = "Act 2",
            Areas =
            [
                Z("The Ardura Caravan",     "", notes: "Check vendors"),
                Z("Vastiri Outskirts",      "Exalted Orb"),
                Z("Mawdun Quarry",          "Uncut Spirit Gem (Level 5)"),
                Z("Mawdun Mine",            "Uncut Support Gem (Level 2)"),
                Z("Traitor's Passage",      "Artificer's Orb"),
                Z("Halani Gates",           "Exalted Orb"),
                Z("Mastodon Badlands",      "Regal Orb, Abyss Currency"),
                Z("Bone Pits",             "Exalted Orb", notes:"relic drop"),
                Z("Keth",                  "Gemcutter's Prism", true, notes:"Kabala-+2 passives, relic drop"),
                Z("Lost City",             "Orb of Alchemy"),
                Z("Buried Shrines",        "Lesser Jeweller's Orb"),
                Z("Valley of the Titans",  "Unique Item, Abyss Currency", true),
                Z("The Titan Grotto",      "Chance Shard"),
                Z("Deshar",               "Lesser Rune", notes:"letter-+2 passives"),
                Z("Path of Mourning",     ""),   // No Vaal Beacon
                Z("Spires of Deshar",     "Gemcutter's Prism", true, notes:"shrine-+10% lightning res"),
                Z("Dreadnought",          ""),   // No Vaal Beacon
                Z("Dreadnought Vanguard", ""),   // No Vaal Beacon
            ]
        },

        // ── Act 3 ────────────────────────────────────────────────────────────
        new ActSettings
        {
            ActName = "Act 3",
            Areas =
            [
                Z("Ziggurat Encampment",        "", notes: "Check vendors"),
                Z("Sandswept Marsh",            "Uncut Support Gem (Level 3)"),
                Z("Jungle Ruins",               "Orb of Alchemy", notes:"Monkey-+2 passives"),
                Z("The Venom Crypts",           "Magic Ring, Abyss Currency", notes:"Vial-choice"),
                Z("Infested Barrens",           "Exalted Orb", notes:"Go Azak Bog for passives"),
                Z("The Azak Bog",               "Rune", notes:"Witch-+30 spirit"),
                Z("Chimeral Wetlands",          "Uncut Skill Gem (Level 9)"),
                Z("Jiquani's Machinarium",      "Artificer's Orb", notes:"Blackjaw-+10% fire res"),
                Z("Jiquani's Sanctum",          "Exalted Orb"),
                Z("The Matlan Waterways",       "Uncut Spirit Gem (Level 10)"),
                Z("The Drowned City",           "Uncut Support Gem (Level 3)", notes:"Molten Vault for reforge bench"),
                Z("The Molten Vault",           "Unique Item", true, notes:"Mektul-reforge bench"),
                Z("Apex of Filth",              "Vaal Orb"),
                Z("Temple of Kopec",            "Uncut Spirit Gem (Level 11)"),
                Z("Utzaal (Past)",              "Random Jewel or Time-Lost Jewel", notes:"Heart can drop"),
                Z("Aggorat (Past)",             "Uncut Skill Gem (Level 11)", notes:"Heart-+2 passives"),
                Z("The Black Chambers (Past)",  "Vaal Orb"),
            ]
        },

        // ── Act 4 ────────────────────────────────────────────────────────────
        new ActSettings
        {
            ActName = "Act 4",
            Areas =
            [
                Z("Kingsmarch",             "", notes: "Check vendors"),
                Z("Isle of Kin",            "Gemcutter's Prism", true),
                Z("Volcanic Warrens",       "Uncut Support Gem (Level 4)"),
                Z("Eye of Hinekora",        "Chaos Orb", notes:"Silent Hall-+5% mana"),
                Z("Halls of the Dead",      "Random Items", notes:"Trials-+5% all res"),
                Z("Trial of the Ancestors", "", notes:"Yama-+2 passives"),
                Z("Abandoned Prison",       "Exalted Orb", notes:"Godess-flask recovery"),
                Z("Solitary Confinement",   "Rune"),
                Z("Kedge Bay",              "Exalted Orb"),
                Z("Journey's End",          "Orb of Alchemy", notes:"Omniphobia-+2 passives"),
                Z("Shrike Island",          "Uncut Support Gem (Level 4)"),
                Z("Whakapanu Island",       "Artificer's Orb"),
                Z("Singing Cavern",         "Magic Charm"),
                Z("Arastas",                "Uncut Skill Gem (Level 12)"),
                Z("The Excavation",         "Rare Amulet"),
                Z("Ngakanu",                "Greater Jeweller's Orb, Abyssal Depths", true),
                Z("Heart of the Tribe",     "Uncut Spirit Gem (Level 12)"),
            ]
        },

        // ── Act 6 / Interludes ───────────────────────────────────────────────
        new ActSettings
        {
            ActName = "Act 6 (Interludes)",
            Areas =
            [
                Z("The Refuge",             "", notes: "Check vendors"),
                Z("The Khari Bazaar",       "", notes: "Check vendors"),
                Z("The Glade",              "", notes: "Check vendors"),
                Z("Scorched Farmlands",     "Uncut Support Gem (Level 4)"),
                Z("Stones of Serle",        "Exalted Orb, Abyss Currency"),
                Z("The Blackwood",          "Greater Orb of Transmutation"),
                Z("Holten",                 "Greater Rune",notes:"Find Wolvenhold for passives"),
                Z("Wolvenhold",             "Greater Orb of Augmentation", notes:"Oswin-+2 passives"),
                Z("Holten Estate",          "Artificer's Orb"),
                Z("The Khari Crossing",     "Gemcutter's Prism", true, notes:"Molten Shrine-5% life"),
                Z("Pools of Khatal",        "Orb of Alchemy"),
                Z("Sel Khari Sanctuary",    "Orb of Chance"),
                Z("The Galai Gates",        "Greater Orb of Augmentation, Abyss Currency"),
                Z("Qimah",                  "Exalted Orb", notes:"Pillars-choose bonus"),
                Z("Qimah Reservoir",        "Greater Orb of Transmutation"),
                Z("Ashen Forest",           "Rare Belt"),
                Z("Kriar Village",          "Greater Rune, Abyss Currency", notes:"Lythara-+40 spirit"),
                Z("Glacial Tarn",           "Greater Orb of Augmentation", notes:"Find Howling Caves for passives"),
                Z("Howling Caves",          "Chaos Orb", notes:"Yeti-+2 passives"),
                Z("Kriar Peaks",            "Greater Orb of Transmutation", notes:"Elder Madox-Free unique"),
                Z("Etched Ravine",          "Exalted Orb"),
                Z("The Cuachic Vault",      "Vaal Orb"),
            ]
        },
    ];
}
