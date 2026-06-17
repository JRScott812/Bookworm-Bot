using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Bookworm_Bot_Class;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bookworm_Bot_Tests
{
	[TestClass]
	public sealed class LetterInputTests
	{
		[TestMethod]
		public void Parse_qu_is_single_q_tile()
		{
			List<Tile> tiles = LetterInput.Parse("qu");
			Assert.HasCount(1, tiles);
			Assert.AreEqual('q', tiles[0].Letter);
		}

		[TestMethod]
		public void Parse_ruby_gem_on_e()
		{
			List<Tile> tiles = LetterInput.Parse("e$ruby");
			Assert.HasCount(1, tiles);
			Assert.AreEqual(GemType.Ruby, tiles[0].Gem);
		}

		[TestMethod]
		public void Parse_emerald_on_q_tile()
		{
			List<Tile> tiles = LetterInput.Parse("q$emerald");
			Assert.HasCount(1, tiles);
			Assert.AreEqual('q', tiles[0].Letter);
			Assert.AreEqual(GemType.Emerald, tiles[0].Gem);
		}
	}

	[TestClass]
	public sealed class LoadoutValidatorTests
	{
		[TestMethod]
		public void Rejects_duplicate_treasures()
		{
			Loadout loadout = new()
			{
				Slot1 = TreasureId.BowOfZyx,
				Slot2 = TreasureId.BowOfZyx
			};
			Assert.IsFalse(LoadoutValidator.TryValidate(loadout, out _));
		}

		[TestMethod]
		public void Rejects_upgrade_chain_conflict()
		{
			Loadout loadout = new()
			{
				Slot1 = TreasureId.HephaestusHammer,
				Slot2 = TreasureId.HandOfHercules
			};
			Assert.IsFalse(LoadoutValidator.TryValidate(loadout, out string? error));
			Assert.IsNotNull(error);
		}

		[TestMethod]
		public void Accepts_valid_three_slot_loadout()
		{
			Loadout loadout = new()
			{
				Slot1 = TreasureId.TomeOfAncients,
				Slot2 = TreasureId.ScimitarOfJustice,
				Slot3 = TreasureId.QuadrumvirSignet
			};
			Assert.IsTrue(LoadoutValidator.TryValidate(loadout, out _));
		}
	}

	[TestClass]
	public sealed class DamageCalculatorTests
	{
		[TestMethod]
		public void Scimitar_adds_ten_percent_per_gem_tile()
		{
			List<Tile> oneGem = [new Tile('a', GemType.Ruby)];
			List<Tile> twoGems =
			[
				new Tile('a', GemType.Ruby),
				new Tile('b', GemType.Emerald)
			];
			float onePlain = GemBoostCalculator.SumGemBonus(oneGem, scimitarEquipped: false);
			float oneScimitar = GemBoostCalculator.SumGemBonus(oneGem, scimitarEquipped: true);
			float twoScimitar = GemBoostCalculator.SumGemBonus(twoGems, scimitarEquipped: true);
			Assert.AreEqual(0.35f, onePlain, 0.001f);
			Assert.AreEqual(0.45f, oneScimitar, 0.001f);
			Assert.AreEqual(0.75f, twoScimitar, 0.001f);
		}

		[TestMethod]
		public void Quadrumvir_signet_bonus_for_qua_words()
		{
			AbilityProfile profile = new()
			{
				Loadout = new Loadout { Slot1 = TreasureId.QuadrumvirSignet }
			};
			float quack = profile.CalculateDamage("quack", WordCategory.None, gemBonus: 0f);
			float bear = profile.CalculateDamage("bear", WordCategory.None, gemBonus: 0f);
			Assert.IsGreaterThan(bear, quack);
		}

		[TestMethod]
		public void Parrot_doubles_r_tile_weight()
		{
			Loadout loadout = new() { Slot1 = TreasureId.WoodenParrot };
			List<Tile> used = [new Tile('r'), new Tile('a'), new Tile('t')];
			int withParrot = AbilityProfile.CalculateAdjustedLength("rat", used, loadout);
			int without = AbilityProfile.CalculateAdjustedLength("rat");
			Assert.IsGreaterThan(without, withParrot);
		}

		[TestMethod]
		public void Fight_context_min_word_length_filters_solver()
		{
			WordDictionary dictionary = WordDictionary.Load(
				Path.Combine(AppContext.BaseDirectory, "TestData", "mini-wordbank"));
			AbilityProfile profile = new()
			{
				Fight = new FightContext { MinWordLength = 4 }
			};
			Solver solver = new(dictionary, profile);
			IReadOnlyList<WordResult> results = solver.FindWords(LetterInput.Parse("c a t z i n c"), topCount: 20);
			Assert.IsFalse(results.Any(result => result.Word == "cat"));
			Assert.IsTrue(results.Any(result => result.Word == "zinc"));
		}

		[TestMethod]
		public void Enemy_catalog_weakness_applies_via_fight_context()
		{
			Assert.IsTrue(EnemyCatalog.TryGet("sphinx", out EnemyDefinition sphinx));
			FightContext fight = EnemyCatalog.ToFightContext(sphinx);
			AbilityProfile profile = new()
			{
				Fight = fight
			};
			float colorWord = profile.CalculateDamage("gold", WordCategory.Colors, gemBonus: 0f);
			float plainWord = profile.CalculateDamage("bear", WordCategory.Mammals, gemBonus: 0f);
			Assert.IsGreaterThan(plainWord * 2f, colorWord);
		}
	}

	[TestClass]
	public sealed class SessionContextTests
	{
		[TestMethod]
		public void Lex_level_three_and_above_boosts_damage()
		{
			AbilityProfile lowLex = new() { Session = new SessionContext { LexLevel = 2 } };
			AbilityProfile highLex = new() { Session = new SessionContext { LexLevel = 3 } };
			float low = lowLex.CalculateDamage("gold", WordCategory.None, gemBonus: 0f);
			float high = highLex.CalculateDamage("gold", WordCategory.None, gemBonus: 0f);
			Assert.IsGreaterThan(low, high);
		}
	}

	[TestClass]
	public sealed class AbilityProfileTests
	{
		[TestMethod]
		public void Waxy_has_adjusted_length_six() => Assert.AreEqual(6, AbilityProfile.CalculateAdjustedLength("waxy"));

		[TestMethod]
		public void Treasure_and_gem_bonuses_stack()
		{
			AbilityProfile profile = new()
			{
				Loadout = new Loadout { Slot1 = TreasureId.HandOfHercules }
			};
			float baseDamage = AbilityProfile.GetBaseDamage("gold");
			float damage = profile.CalculateDamage("gold", WordCategory.Colors | WordCategory.Metals, gemBonus: 0.35f);
			Assert.IsGreaterThan(baseDamage, damage);
		}

		[TestMethod]
		public void Hand_adds_flat_heart_bonus()
		{
			AbilityProfile profile = new()
			{
				Loadout = new Loadout { Slot1 = TreasureId.HandOfHercules }
			};
			float damage = profile.CalculateDamage("cat", WordCategory.Mammals, gemBonus: 0f);
			Assert.IsGreaterThanOrEqualTo(AbilityProfile.GetBaseDamage("cat") + 1f, damage);
		}

		[TestMethod]
		public void Scimitar_boosts_gem_bonus()
		{
			AbilityProfile withScimitar = new()
			{
				Loadout = new Loadout { Slot1 = TreasureId.ScimitarOfJustice }
			};
			AbilityProfile without = new();
			float boosted = withScimitar.CalculateDamage("cat", WordCategory.None, gemBonus: 0.35f);
			float plain = without.CalculateDamage("cat", WordCategory.None, gemBonus: 0.35f);
			Assert.IsGreaterThan(plain, boosted);
		}

		[TestMethod]
		public void Bow_increases_adjusted_length_for_z_tile()
		{
			Loadout loadout = new() { Slot1 = TreasureId.BowOfZyx };
			List<Tile> used = [new Tile('z'), new Tile('o'), new Tile('o')];
			int withBow = AbilityProfile.CalculateAdjustedLength("zoo", used, loadout);
			int without = AbilityProfile.CalculateAdjustedLength("zoo");
			Assert.IsGreaterThan(without, withBow);
		}

		[TestMethod]
		public void Enemy_weakness_multiplier_applies()
		{
			AbilityProfile profile = new()
			{
				EnemyWeakness = WordCategory.Metals,
				EnemyWeaknessMultiplier = 3f
			};
			float weak = profile.CalculateDamage("zinc", WordCategory.Metals, gemBonus: 0f);
			float normal = profile.CalculateDamage("bear", WordCategory.Mammals, gemBonus: 0f);
			Assert.IsGreaterThan(normal, weak);
		}
	}

	[TestClass]
	public sealed class SolverTests
	{
		private static WordDictionary LoadMiniDictionary() => WordDictionary.Load(GetMiniWordBankPath());

		private static string GetMiniWordBankPath() => Path.Combine(AppContext.BaseDirectory, "TestData", "mini-wordbank");

		[TestMethod]
		public void FindWords_finds_gold_with_gem()
		{
			WordDictionary dictionary = LoadMiniDictionary();
			Solver solver = new(dictionary, new AbilityProfile());
			List<Tile> tiles = LetterInput.Parse("g$ruby o l d i r o n");
			IReadOnlyList<WordResult> results = solver.FindWords(tiles, topCount: 10);
			WordResult? gold = results.FirstOrDefault(result => result.Word == "gold");
			Assert.IsNotNull(gold);
			Assert.IsTrue(gold.Value.Bonuses.Any(bonus => bonus.Contains("gems")));
		}

		[TestMethod]
		public void TryFindBestWord_does_not_consume_extra_u_for_q()
		{
			WordDictionary dictionary = LoadMiniDictionary();
			Solver solver = new(dictionary, new AbilityProfile());
			List<Tile> tiles = LetterInput.Parse("q u i z");
			Assert.IsTrue(solver.TryFindBestWord(tiles, "quiz", out WordResult result));
			Assert.AreEqual(3, result.TilesUsed);
		}
	}

	[TestClass]
	public sealed class TileBoardTests
	{
		[TestMethod]
		public void ApplyPlayedWord_replaces_used_tiles()
		{
			List<Tile> board = LetterInput.Parse("goldiron");
			int usedMask = 0b1111;
			List<Tile> replacements = LetterInput.Parse("z");
			List<Tile> next = TileBoard.ApplyPlayedWord(board, usedMask, replacements);
			Assert.HasCount(5, next);
			Assert.AreEqual('z', next[^1].Letter);
		}

		[TestMethod]
		public void GetUsedTiles_returns_played_letters()
		{
			List<Tile> board = LetterInput.Parse("zinc");
			List<Tile> used = TileBoard.GetUsedTiles(board, usedMask: 0b1111);
			Assert.HasCount(4, used);
		}
	}

	[TestClass]
	public sealed class GridBoardTests
	{
		[TestMethod]
		public void Playable_tiles_exclude_locked()
		{
			GridBoard board = new();
			board.SetCell(0, new GridCell('g'));
			board.SetCell(1, new GridCell('o', Modifier: TileModifier.Locked));
			board.SetCell(2, new GridCell('l'));
			board.SetCell(3, new GridCell('d', GemType.Ruby));

			(List<Tile> tiles, int[] indices) = board.GetPlayableTiles();
			Assert.HasCount(3, tiles);
			Assert.AreEqual(0, indices[0]);
			Assert.AreEqual(2, indices[1]);
			Assert.AreEqual(3, indices[2]);
		}

		[TestMethod]
		public void MapPoolMaskToGridMask_maps_solver_indices()
		{
			int[] gridIndices = [0, 3, 5];
			int poolMask = 0b101;
			int gridMask = GridBoard.MapPoolMaskToGridMask(poolMask, gridIndices);
			Assert.AreEqual((1 << 0) | (1 << 5), gridMask);
		}

		[TestMethod]
		public void ApplyRemovedWord_falls_tiles_and_leaves_drop_in_slots_at_top()
		{
			GridBoard board = new();
			board.SetCell(0, new GridCell('g'));
			board.SetCell(4, new GridCell('o'));
			board.SetCell(8, new GridCell('l'));
			board.SetCell(12, new GridCell('d'));

			GridBoard next = board.ApplyRemovedWord(1 << 4);

			Assert.IsTrue(next.GetCell(0).IsEmpty);
			Assert.AreEqual('g', next.GetCell(4).Letter);
			Assert.AreEqual('l', next.GetCell(8).Letter);
			Assert.AreEqual('d', next.GetCell(12).Letter);
		}

		[TestMethod]
		public void ApplyPlayedWord_keeps_board_full()
		{
			GridBoard board = new();
			for (int index = 0; index < GridBoard.CellCount; index++)
			{
				board.SetCell(index, new GridCell((char)('a' + index)));
			}

			GridBoard next = board.ApplyPlayedWord(1 << 4, [new GridCell('z')]);

			Assert.IsTrue(next.IsFull);
			Assert.AreEqual('z', next.GetCell(0).Letter);
		}

		[TestMethod]
		public void ApplyPlayedWord_rejects_empty_drop_ins()
		{
			GridBoard board = new();
			board.SetCell(0, new GridCell('g'));
			board.SetCell(4, new GridCell('o'));

			Assert.ThrowsExactly<InvalidOperationException>(() =>
				board.ApplyPlayedWord(1 << 4, [GridCell.Empty]));
		}

		[TestMethod]
		public void ApplyPlayedWord_drops_replacements_at_top_of_each_column()
		{
			GridBoard board = new();
			board.SetCell(0, new GridCell('g'));
			board.SetCell(4, new GridCell('o'));
			board.SetCell(8, new GridCell('l'));
			board.SetCell(12, new GridCell('d'));

			GridBoard next = board.ApplyPlayedWord(1 << 4, [new GridCell('x')]);

			Assert.AreEqual('x', next.GetCell(0).Letter);
			Assert.AreEqual('g', next.GetCell(4).Letter);
			Assert.AreEqual('l', next.GetCell(8).Letter);
			Assert.AreEqual('d', next.GetCell(12).Letter);
		}

		[TestMethod]
		public void ApplyPlayedWord_replaces_used_grid_cells_in_order()
		{
			GridBoard board = new();
			board.SetCell(0, new GridCell('g'));
			board.SetCell(1, new GridCell('o'));
			board.SetCell(2, new GridCell('l'));
			board.SetCell(3, new GridCell('d'));

			int usedMask = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 3);
			GridBoard next = board.ApplyPlayedWord(usedMask,
			[
				new GridCell('w'),
				new GridCell('a'),
				new GridCell('x'),
				new GridCell('z')
			]);
			Assert.AreEqual('w', next.GetCell(0).Letter);
			Assert.AreEqual('a', next.GetCell(1).Letter);
			Assert.AreEqual('x', next.GetCell(2).Letter);
			Assert.AreEqual('z', next.GetCell(3).Letter);
		}

		[TestMethod]
		public void FromFlatTiles_fills_row_major()
		{
			GridBoard board = GridBoard.FromFlatTiles(LetterInput.Parse("g o l d"));
			Assert.AreEqual('g', board.GetCell(0).Letter);
			Assert.AreEqual('d', board.GetCell(3).Letter);
		}

		[TestMethod]
		public void Session_maps_UsedGridMask_from_grid_board()
		{
			string path = Path.Combine(AppContext.BaseDirectory, "TestData", "mini-wordbank");
			WordDictionary dictionary = WordDictionary.Load(path);
			GameSession session = new(dictionary, new AbilityProfile());

			GridBoard board = new();
			board.SetCell(0, new GridCell('g', GemType.Ruby));
			board.SetCell(1, new GridCell('o'));
			board.SetCell(2, new GridCell('l'));
			board.SetCell(3, new GridCell('d'));
			session.SetBoard(board);

			WordResult? gold = session.GetSuggestions().FirstOrDefault(result => result.Word == "gold");
			Assert.IsNotNull(gold);
			Assert.AreNotEqual(0, gold.Value.UsedGridMask);
		}
	}

	[TestClass]
	public sealed class GameSessionTests
	{
		[TestMethod]
		public void SetBoard_and_suggest_workflow()
		{
			string path = Path.Combine(AppContext.BaseDirectory, "TestData", "mini-wordbank");
			WordDictionary dictionary = WordDictionary.Load(path);
			GameSession session = new(dictionary, new AbilityProfile());
			session.SetBoard(LetterInput.Parse("g o l d"));
			IReadOnlyList<WordResult> first = session.GetSuggestions();
			Assert.IsNotEmpty(first);
			session.SetBoard(LetterInput.Parse("b e a r"));
			IReadOnlyList<WordResult> second = session.GetSuggestions();
			Assert.IsTrue(second.Any(result => result.Word == "bear"));
		}
	}
}
