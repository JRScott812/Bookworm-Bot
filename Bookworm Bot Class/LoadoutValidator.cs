using System.Collections.Generic;
using System.Linq;

namespace Bookworm_Bot_Class
{
	public static class LoadoutValidator
	{
		private static readonly (TreasureId Basic, TreasureId Upgraded)[] UpgradeChains =
		[
			(TreasureId.HephaestusHammer, TreasureId.HandOfHercules),
			(TreasureId.BowOfZyx, TreasureId.ArchOfXyzzy),
			(TreasureId.TomeOfAncients, TreasureId.TabletOfTheAges),
			(TreasureId.WolfbaneNecklace, TreasureId.SlayerTalisman)
		];
		public static bool TryValidate(Loadout loadout, out string? error)
		{
			List<TreasureId> equipped = [.. loadout.Equipped];
			if (equipped.Count > 3)
			{
				error = "A loadout can include at most 3 treasures.";
				return false;
			}

			if (equipped.Distinct().Count() != equipped.Count)
			{
				error = "Duplicate treasures are not allowed in the same loadout.";
				return false;
			}

			(TreasureId basic, TreasureId upgraded) conflict = UpgradeChains
				.FirstOrDefault(chain => equipped.Contains(chain.Basic) && equipped.Contains(chain.Upgraded));
			if (conflict != default)
			{
				error = $"Cannot equip both {TreasureCatalog.GetDisplayName(conflict.basic)} and {TreasureCatalog.GetDisplayName(conflict.upgraded)}.";
				return false;
			}

			error = null;
			return true;
		}
	}
}