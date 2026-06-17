using Bookworm_Bot_Class;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bookworm_Bot_Tests
{
	[TestClass]
	public sealed class AutomationCoreTests
	{
		[TestMethod]
		public void GemColors_matches_amethyst_sample()
		{
			Assert.IsTrue(GemColors.TryMatch(168, 85, 247, out GemType gem));
			Assert.AreEqual(GemType.Amethyst, gem);
		}

		[TestMethod]
		public void GemColors_rejects_neutral_gray()
		{
			Assert.IsFalse(GemColors.TryMatch(128, 128, 128, out _));
		}

		[TestMethod]
		public void TileLetterParser_rejects_multi_character_ocr_noise()
		{
			Assert.IsFalse(TileLetterParser.TryParseSingleLetter("CL", out _));
			Assert.IsTrue(TileLetterParser.TryParseSingleLetter("M", out char letter));
			Assert.AreEqual('m', letter);
		}

		[TestMethod]
		public void TileLetterParser_parses_qu_and_single_letters()
		{
			Assert.IsTrue(TileLetterParser.TryParse("QU", out char qu));
			Assert.AreEqual('q', qu);

			Assert.IsTrue(TileLetterParser.TryParse("z", out char zed));
			Assert.AreEqual('z', zed);

			Assert.IsFalse(TileLetterParser.TryParse("12", out _));
		}

		[TestMethod]
		public void LoadoutSettingsStore_builds_profile_with_manual_weakness()
		{
			LoadoutSettingsData data = new()
			{
				Weakness = WordCategory.Metals.ToString(),
				Multiplier = 4f,
				LexLevel = 5,
				Slot1 = TreasureId.TomeOfAncients.ToString()
			};

			AbilityProfile profile = LoadoutSettingsStore.BuildProfile(data);

			Assert.AreEqual(WordCategory.Metals, profile.EnemyWeakness);
			Assert.AreEqual(4f, profile.EnemyWeaknessMultiplier);
			Assert.AreEqual(5, profile.Session.LexLevel);
			Assert.IsTrue(profile.Loadout.Has(TreasureId.TomeOfAncients));
		}
	}
}
