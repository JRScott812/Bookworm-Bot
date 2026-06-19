using System;
using System.Collections.Generic;
using System.Linq;

namespace Bookworm_Bot_Class
{
	public sealed class Loadout
	{
		public TreasureId Slot1 { get; set; }
		public TreasureId Slot2 { get; set; }
		public TreasureId Slot3 { get; set; }

		public IEnumerable<TreasureId> Equipped =>
			new[] { Slot1, Slot2, Slot3 }.Where(slot => slot != TreasureId.None);

		public bool Has(TreasureId treasure) => Equipped.Contains(treasure);

		public void Normalize()
		{
			ResolveUpgradeChain(TreasureId.HephaestusHammer, TreasureId.HandOfHercules);
			ResolveUpgradeChain(TreasureId.BowOfZyx, TreasureId.ArchOfXyzzy);
			ResolveUpgradeChain(TreasureId.TomeOfAncients, TreasureId.TabletOfTheAges);
			ResolveUpgradeChain(TreasureId.WolfbaneNecklace, TreasureId.SlayerTalisman);
			ResolveUpgradeChain(TreasureId.JeweledKey, TreasureId.EndlessGemPouch);
		}

		public void SetSlot(int slot, TreasureId treasure)
		{
			switch (slot)
			{
				case 1: Slot1 = treasure; break;
				case 2: Slot2 = treasure; break;
				case 3: Slot3 = treasure; break;
				default: throw new ArgumentOutOfRangeException(nameof(slot));
			}
		}

		private void ResolveUpgradeChain(TreasureId basic, TreasureId upgraded)
		{
			if (Has(upgraded))
			{
				ClearSlot(basic);
			}
		}

		private void ClearSlot(TreasureId treasure)
		{
			if (Slot1 == treasure) Slot1 = TreasureId.None;
			if (Slot2 == treasure) Slot2 = TreasureId.None;
			if (Slot3 == treasure) Slot3 = TreasureId.None;
		}
	}

	public static class TreasureCatalog
	{
		public static IReadOnlyList<TreasureId> ScoringTreasures { get; } =
		[
			TreasureId.HephaestusHammer,
			TreasureId.HandOfHercules,
			TreasureId.BowOfZyx,
			TreasureId.ArchOfXyzzy,
			TreasureId.TomeOfAncients,
			TreasureId.TabletOfTheAges,
			TreasureId.WoodenParrot,
			TreasureId.ScimitarOfJustice,
			TreasureId.WolfbaneNecklace,
			TreasureId.SlayerTalisman,
			TreasureId.QuadrumvirSignet,
			TreasureId.JeweledKey,
			TreasureId.EndlessGemPouch
		];

		private static readonly Dictionary<string, TreasureId> Aliases = new(StringComparer.OrdinalIgnoreCase)
		{
			["hammer"] = TreasureId.HephaestusHammer,
			["hephaestus"] = TreasureId.HephaestusHammer,
			["hand"] = TreasureId.HandOfHercules,
			["hercules"] = TreasureId.HandOfHercules,
			["bow"] = TreasureId.BowOfZyx,
			["zyx"] = TreasureId.BowOfZyx,
			["arch"] = TreasureId.ArchOfXyzzy,
			["xyzzy"] = TreasureId.ArchOfXyzzy,
			["tome"] = TreasureId.TomeOfAncients,
			["tablet"] = TreasureId.TabletOfTheAges,
			["parrot"] = TreasureId.WoodenParrot,
			["scimitar"] = TreasureId.ScimitarOfJustice,
			["justice"] = TreasureId.ScimitarOfJustice,
			["wolfbane"] = TreasureId.WolfbaneNecklace,
			["wolf"] = TreasureId.WolfbaneNecklace,
			["slayer"] = TreasureId.SlayerTalisman,
			["talisman"] = TreasureId.SlayerTalisman,
			["quadrumvir"] = TreasureId.QuadrumvirSignet,
			["signet"] = TreasureId.QuadrumvirSignet,
			["qua"] = TreasureId.QuadrumvirSignet,
			["key"] = TreasureId.JeweledKey,
			["jeweled"] = TreasureId.JeweledKey,
			["jeweledkey"] = TreasureId.JeweledKey,
			["pouch"] = TreasureId.EndlessGemPouch,
			["endless"] = TreasureId.EndlessGemPouch,
			["endlessgem"] = TreasureId.EndlessGemPouch
		};

		public static string GetDisplayName(TreasureId treasure) => treasure switch
		{
			TreasureId.None => "None",
			TreasureId.HephaestusHammer => "Hephaestus's Hammer (+½ heart)",
			TreasureId.HandOfHercules => "Hand of Hercules (+1 heart, +50% metal)",
			TreasureId.BowOfZyx => "Bow of Zyx (X/Y/Z → 2.5 letters)",
			TreasureId.ArchOfXyzzy => "Arch of Xyzzy (X/Y/Z → 3 letters)",
			TreasureId.TomeOfAncients => "Tome of Ancients (+100% color)",
			TreasureId.TabletOfTheAges => "Tablet of the Ages (+150% color)",
			TreasureId.WoodenParrot => "Wooden Parrot (R → 2 letters)",
			TreasureId.ScimitarOfJustice => "Scimitar of Justice (+10% gems)",
			TreasureId.WolfbaneNecklace => "Wolfbane Necklace (+50% mammal)",
			TreasureId.SlayerTalisman => "Slayer Talisman (+75% mammal)",
			TreasureId.QuadrumvirSignet => "Quadrumvir Signet (+50% if word has \"qua\")",
			TreasureId.JeweledKey => "Jeweled Key (short words can create gems)",
			TreasureId.EndlessGemPouch => "Endless Gem Pouch (better short-word gem odds)",
			_ => throw new ArgumentOutOfRangeException(nameof(treasure))
		};

		public static bool TryParse(string? input, out TreasureId treasure)
		{
			treasure = TreasureId.None;
			if (string.IsNullOrWhiteSpace(input))
			{
				return false;
			}

			string normalized = input.Trim();
			if (int.TryParse(normalized, out int number)
				&& number >= 1
				&& number <= ScoringTreasures.Count)
			{
				treasure = ScoringTreasures[number - 1];
				return true;
			}

			normalized = normalized.ToLowerInvariant();
			if (Aliases.TryGetValue(normalized, out treasure))
			{
				return true;
			}

			treasure = ScoringTreasures.FirstOrDefault(candidate =>
				normalized.Equals(candidate.ToString(), StringComparison.OrdinalIgnoreCase)
				|| normalized.Equals(GetDisplayName(candidate), StringComparison.OrdinalIgnoreCase));

			return treasure != TreasureId.None;
		}
	}
}
