using System;
using System.Collections.Generic;
using System.Linq;

using Bookworm_Bot_Class;
using Bookworm_Bot_GUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Windows.ApplicationModel.DataTransfer;

namespace Bookworm_Bot_GUI
{
	public sealed partial class MainWindow : Window
	{
		private WordResult? _lastPlayedWord;
		private GridBoard? _prePlayBoard;
		private IReadOnlyList<WordResult> _currentSuggestions = [];
		private readonly bool _uiReady;

		public MainWindow()
		{
			InitializeComponent();
			RestoreThemeSelection();
			ThemeHelper.Apply(AppThemeSettings.Load(), RootGrid);
			ThemeHelper.ThemeChanged += OnAppThemeChanged;
			PopulateEnemyCombo();
			PopulateTreasureCombos();
			RestoreSavedLoadout();
			UpdateManualWeaknessVisibility();
			_uiReady = true;
			AppWindow?.Resize(new Windows.Graphics.SizeInt32(1100, 820));
			InitializeSession();
		}

		private void InitializeSession()
		{
			if (App.Session is null)
			{
				SubtitleText.Text = "Dictionary unavailable";
				ShowStatus(App.LoadError ?? "Dictionary failed to load.", InfoBarSeverity.Error);
				return;
			}

			SubtitleText.Text = $"{App.Dictionary?.WordCount:N0} words · ranked by {EmojiCatalog.Heart} damage";
			ApplyLoadoutToSession(save: false);
			LoadSampleBoard(showStatus: false);
			ShowStatus("Type letters in tiles · click a tile border to set gem/status.", InfoBarSeverity.Informational, showInfoBar: false);
		}

		private static GameSession RequireSession() =>
			App.Session is null
				? throw new InvalidOperationException(App.LoadError ?? "Session not available.")
				: App.Session;

		private void BoardGrid_BoardChanged(object sender, EventArgs e)
		{
			SyncBoardToSession();
			RefreshSuggestions();
		}

		private async void BoardGrid_StatusEditRequested(object sender, int cellIndex)
		{
			GridCell current = BoardGrid.Board.GetCell(cellIndex);
			TileStatusDialog dialog = new(current) { XamlRoot = Content.XamlRoot };
			ContentDialogResult result = await dialog.ShowAsync();
			if (result != ContentDialogResult.Primary)
			{
				return;
			}

			BoardGrid.SetCell(cellIndex, dialog.Result);
			SyncBoardToSession();
			RefreshSuggestions();
		}

		private void SampleBoard_Click(object sender, RoutedEventArgs e) => LoadSampleBoard();

		private void LoadSampleBoard(bool showStatus = true)
		{
			if (_lastPlayedWord is not null)
			{
				ClearPlayState(restoreBoard: true);
			}

			GridBoard board = new();
			List<Tile> tiles = LetterInput.Parse("g$ruby o l d i r o n z i n c a t e s");
			for (int index = 0; index < Math.Min(tiles.Count, GridBoard.CellCount); index++)
			{
				board.SetCell(index, GridCell.FromTile(tiles[index]));
			}

			BoardGrid.SetBoard(board);
			SyncBoardToSession();
			RefreshSuggestions();
			if (showStatus)
			{
				ShowStatus("Sample board loaded.", InfoBarSeverity.Success);
			}
		}

		private void ClearBoard_Click(object sender, RoutedEventArgs e) => LoadSampleBoard();

		private void RefreshWords_Click(object sender, RoutedEventArgs e)
		{
			RefreshSuggestions();
			ShowStatus("Suggestions refreshed.", InfoBarSeverity.Informational, showInfoBar: false);
		}

		private async void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			int index = ResultsList.SelectedIndex;
			if (index < 0 || index >= _currentSuggestions.Count)
			{
				CopyWordButton.IsEnabled = false;
				return;
			}

			try
			{
				if (_lastPlayedWord is not null && _prePlayBoard is GridBoard savedBoard)
				{
					if (!BoardGrid.Board.IsFull)
					{
						BoardGrid.SetBoard(savedBoard);
					}

					BoardGrid.EndWordPlay();
				}

				_lastPlayedWord = _currentSuggestions[index];
				CopyWordButton.IsEnabled = true;
				_prePlayBoard = BoardGrid.GetBoard();
				await BoardGrid.BeginWordPlayAsync(_lastPlayedWord.Value.UsedGridMask);
				SyncBoardToSession();
				ApplyDropsButton.IsEnabled = true;
				CancelPlayButton.IsEnabled = true;
				RefreshSuggestions();
				ShowStatus(
					$"Selected {_lastPlayedWord.Value.Word.ToUpperInvariant()} ({_lastPlayedWord.Value.Damage:0.0} {EmojiCatalog.Heart}). " +
					"Tiles fell — type new letters in the empty slots at the top, then Apply.",
					InfoBarSeverity.Informational,
					showInfoBar: false);
			}
			catch (Exception ex)
			{
				ShowStatus(ex.Message, InfoBarSeverity.Error);
				ClearPlayState(restoreBoard: true);
			}
		}

		private async void ResultsList_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) =>
			await CopySelectedWordAsync();

		private async void CopyWord_Click(object sender, RoutedEventArgs e) => await CopySelectedWordAsync();

		private async System.Threading.Tasks.Task CopySelectedWordAsync()
		{
			int index = ResultsList.SelectedIndex;
			if (index < 0 || index >= _currentSuggestions.Count)
			{
				return;
			}

			string word = _currentSuggestions[index].Word.ToUpperInvariant();
			DataPackage package = new();
			package.SetText(word);
			Clipboard.SetContent(package);
			ShowStatus($"Copied \"{word}\" to clipboard.", InfoBarSeverity.Success);
			await System.Threading.Tasks.Task.CompletedTask;
		}

		private async void ApplyDrops_Click(object sender, RoutedEventArgs e)
		{
			if (_lastPlayedWord is not WordResult played)
			{
				return;
			}

			try
			{
				GameSession session = RequireSession();
				if (_prePlayBoard is not GridBoard prePlayBoard)
				{
					ShowStatus("Play state was lost — cancel and select the word again.", InfoBarSeverity.Error);
					return;
				}

				List<GridCell> replacements = [];
				foreach (int index in GetUsedGridIndices(BoardGrid.DropInMask))
				{
					GridCell replacement = BoardGrid.Board.GetCell(index);
					if (replacement.IsEmpty)
					{
						ShowStatus("Every empty slot at the top needs a letter before applying.", InfoBarSeverity.Error);
						return;
					}

					replacements.Add(replacement);
				}

				GridBoard final = prePlayBoard.ApplyPlayedWord(played.UsedGridMask, replacements);
				BoardGrid.SetBoard(final);
				await BoardGrid.CompleteWordPlayAsync();
				session.SetBoard(BoardGrid.GetBoard());
				ClearPlayState();
				RefreshSuggestions();
				ShowStatus("Word applied — new letters dropped in from the top.", InfoBarSeverity.Success);
			}
			catch (Exception ex)
			{
				ShowStatus(ex.Message, InfoBarSeverity.Error);
			}
		}

		private void CancelPlay_Click(object sender, RoutedEventArgs e)
		{
			ClearPlayState(restoreBoard: true);
			ShowStatus("Play cancelled.", InfoBarSeverity.Informational, showInfoBar: false);
		}

		private void ClearPlayState(bool restoreBoard = false)
		{
			if (restoreBoard && _prePlayBoard is GridBoard savedBoard)
			{
				BoardGrid.SetBoard(savedBoard);
				SyncBoardToSession();
			}

			_prePlayBoard = null;
			_lastPlayedWord = null;
			BoardGrid.EndWordPlay();
			ApplyDropsButton.IsEnabled = false;
			CancelPlayButton.IsEnabled = false;
			CopyWordButton.IsEnabled = false;
			ResultsList.SelectedIndex = -1;

			if (restoreBoard)
			{
				RefreshSuggestions();
			}
		}

		private static IEnumerable<int> GetUsedGridIndices(int usedGridMask)
		{
			for (int index = 0; index < GridBoard.CellCount; index++)
			{
				if ((usedGridMask & (1 << index)) != 0)
				{
					yield return index;
				}
			}
		}

		private void SyncBoardToSession() => RequireSession().SetBoard(BoardGrid.GetBoard());

		private void LoadoutChanged(object sender, RoutedEventArgs e)
		{
			if (!_uiReady || App.Session is null)
			{
				return;
			}

			UpdateManualWeaknessVisibility();
			ApplyLoadoutToSession();
			if (BoardGrid.Board.PlayableCount > 0)
			{
				RefreshSuggestions();
			}
		}

		private void WeaknessMultiplierChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
			LoadoutChanged(sender, new RoutedEventArgs());

		private void LexLevelChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
			LoadoutChanged(sender, new RoutedEventArgs());

		private void StatusInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args) => StatusInfoBar.IsOpen = false;

		private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_uiReady || ThemeCombo.SelectedItem is not ComboBoxItem { Tag: string tag })
			{
				return;
			}

			if (!Enum.TryParse(tag, out AppThemeMode mode))
			{
				return;
			}

			ThemeHelper.Apply(mode, RootGrid);
			AppThemeSettings.Save(mode);
		}

		private void OnAppThemeChanged(object? sender, EventArgs e) => BoardGrid.RefreshAppearance();

		private void RestoreThemeSelection()
		{
			AppThemeMode mode = AppThemeSettings.Load();
			for (int index = 0; index < ThemeCombo.Items.Count; index++)
			{
				if (ThemeCombo.Items[index] is ComboBoxItem { Tag: string tag }
					&& string.Equals(tag, mode.ToString(), StringComparison.Ordinal))
				{
					ThemeCombo.SelectedIndex = index;
					return;
				}
			}

			ThemeCombo.SelectedIndex = 0;
		}

		private void ApplyLoadoutToSession(bool save = true)
		{
			Loadout loadout = new()
			{
				Slot1 = GetSelectedTreasure(TreasureSlot1Combo),
				Slot2 = GetSelectedTreasure(TreasureSlot2Combo),
				Slot3 = GetSelectedTreasure(TreasureSlot3Combo)
			};
			loadout.Normalize();

			AbilityProfile profile = new()
			{
				Loadout = loadout,
				Session = new SessionContext
				{
					LexLevel = (int)Math.Clamp(LexLevelBox.Value, 1, 42),
					PowerUpActive = PowerUpCheckBox.IsChecked == true
				}
			};

			string? enemyId = GetSelectedEnemyId();
			if (!string.IsNullOrEmpty(enemyId)
				&& EnemyCatalog.TryGet(enemyId, out EnemyDefinition enemy))
			{
				profile.Fight = EnemyCatalog.ToFightContext(enemy);
				profile.EnemyWeakness = WordCategory.None;
			}
			else
			{
				profile.Fight = null;
				profile.EnemyWeakness = GetSelectedWeakness();
				profile.EnemyWeaknessMultiplier = (float)(WeaknessMultiplierBox.Value > 0 ? WeaknessMultiplierBox.Value : 3);
			}

			GameSession session = RequireSession();
			session.SetProfile(profile);
			if (profile.Fight is not null)
			{
				session.SetFight(profile.Fight);
			}

			session.SetSession(profile.Session);

			if (save)
			{
				LoadoutSettings.Save(
					enemyId,
					profile.EnemyWeakness,
					profile.EnemyWeaknessMultiplier,
					profile.Session.LexLevel,
					profile.Session.PowerUpActive,
					loadout.Slot1,
					loadout.Slot2,
					loadout.Slot3);
			}
		}

		private void RestoreSavedLoadout()
		{
			if (!LoadoutSettings.TryLoad(
				out string? enemyId,
				out WordCategory weakness,
				out float multiplier,
				out int lexLevel,
				out bool powerUpActive,
				out TreasureId slot1,
				out TreasureId slot2,
				out TreasureId slot3))
			{
				return;
			}

			SelectEnemy(enemyId);
			SelectWeakness(weakness);
			WeaknessMultiplierBox.Value = multiplier;
			LexLevelBox.Value = lexLevel;
			PowerUpCheckBox.IsChecked = powerUpActive;
			SelectTreasure(TreasureSlot1Combo, slot1);
			SelectTreasure(TreasureSlot2Combo, slot2);
			SelectTreasure(TreasureSlot3Combo, slot3);
			UpdateManualWeaknessVisibility();
		}

		private void SelectWeakness(WordCategory weakness) =>
			SelectComboByTag(WeaknessCombo, weakness == WordCategory.None ? "None" : weakness.ToString());

		private void PopulateEnemyCombo()
		{
			EnemyCombo.Items.Clear();
			EnemyCombo.Items.Add(new ComboBoxItem { Content = "Manual weakness", Tag = string.Empty });
			foreach (EnemyDefinition enemy in EnemyCatalog.All)
			{
				EnemyCombo.Items.Add(new ComboBoxItem
				{
					Content = enemy.DisplayName,
					Tag = enemy.Id
				});
			}

			EnemyCombo.SelectedIndex = 0;
		}

		private void SelectEnemy(string? enemyId) =>
			SelectComboByTag(EnemyCombo, string.IsNullOrWhiteSpace(enemyId) ? string.Empty : enemyId);

		private string? GetSelectedEnemyId()
		{
			if (EnemyCombo.SelectedItem is not ComboBoxItem item)
			{
				return null;
			}

			string? tag = item.Tag as string;
			return string.IsNullOrEmpty(tag) ? null : tag;
		}

		private void UpdateManualWeaknessVisibility()
		{
			bool manual = string.IsNullOrEmpty(GetSelectedEnemyId());
			ManualWeaknessPanel.Visibility = manual ? Visibility.Visible : Visibility.Collapsed;
		}

		private static void SelectTreasure(ComboBox combo, TreasureId treasure) =>
			SelectComboByTag(combo, treasure.ToString());

		private void PopulateTreasureCombos()
		{
			PopulateTreasureCombo(TreasureSlot1Combo);
			PopulateTreasureCombo(TreasureSlot2Combo);
			PopulateTreasureCombo(TreasureSlot3Combo);
		}

		private static void PopulateTreasureCombo(ComboBox combo)
		{
			combo.Items.Clear();
			combo.Items.Add(CreateTreasureItem(TreasureId.None));
			foreach (TreasureId treasure in TreasureCatalog.ScoringTreasures)
			{
				combo.Items.Add(CreateTreasureItem(treasure));
			}

			combo.SelectedIndex = 0;
		}

		private static ComboBoxItem CreateTreasureItem(TreasureId treasure) =>
			new()
			{
				Content = EmojiCatalog.TreasureDisplayName(treasure),
				Tag = treasure.ToString()
			};

		private static TreasureId GetSelectedTreasure(ComboBox combo) =>
			combo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag
				? TreasureId.None
				: Enum.TryParse(tag, out TreasureId treasure)
					? treasure
					: TreasureId.None;

		private WordCategory GetSelectedWeakness() =>
			WeaknessCombo.SelectedItem is ComboBoxItem { Tag: string tag }
			&& Enum.TryParse(tag, out WordCategory weakness)
				? weakness
				: WordCategory.None;

		private static void SelectComboByTag(ComboBox combo, string tag)
		{
			for (int index = 0; index < combo.Items.Count; index++)
			{
				if (combo.Items[index] is ComboBoxItem { Tag: string itemTag } && itemTag == tag)
				{
					combo.SelectedIndex = index;
					return;
				}
			}
		}

		private void UpdateBoardCountBadge()
		{
			GridBoard board = BoardGrid.Board;
			BoardCountBadge.Text = !board.IsFull
				? $"{board.FilledCount}/{GridBoard.CellCount} tiles set"
				: $"{board.PlayableCount} {EmojiCatalog.Tile} playable";
		}

		private void RefreshSuggestions()
		{
			GameSession session = RequireSession();
			SyncBoardToSession();

			bool dropInMode = _lastPlayedWord is not null;
			if (dropInMode && !session.Board.IsFull)
			{
				ResultsList.ItemsSource = null;
				_currentSuggestions = [];
				UpdateResultsEmptyState(true);
				EmptyResultsText.Text = "Type new letters in the empty slots at the top, then Apply.";
				ResultsCountBadge.Text = "Drop-in mode";
				BoardCountBadge.Text = $"{BoardGrid.Board.FilledCount}/{GridBoard.CellCount} tiles set";
				return;
			}

			if (!session.Board.IsFull)
			{
				ResultsList.ItemsSource = null;
				_currentSuggestions = [];
				UpdateResultsEmptyState(true);
				EmptyResultsText.Text = $"Fill all {GridBoard.CellCount} tiles to see suggestions.";
				ResultsCountBadge.Text = string.Empty;
				UpdateBoardCountBadge();
				return;
			}

			if (session.Board.PlayableCount == 0)
			{
				ResultsList.ItemsSource = null;
				_currentSuggestions = [];
				UpdateResultsEmptyState(true);
				EmptyResultsText.Text = "No playable tiles — unlock or replace locked tiles.";
				ResultsCountBadge.Text = string.Empty;
				UpdateBoardCountBadge();
				return;
			}

			EmptyResultsText.Text = "No words found on this board.";

			_currentSuggestions = session.GetSuggestions();
			List<string> lines = _currentSuggestions
				.Select((word, index) =>
				{
					string bonuses = word.Bonuses.Count == 0
						? string.Empty
						: $" [{string.Join(", ", word.Bonuses)}]";
					string power = WordPowerRatings.GetLabel(WordPowerRatings.FromAdjustedLength(word.AdjustedLength));
					string powerLabel = string.IsNullOrEmpty(power) ? string.Empty : $" · {power}";
					return $"{index + 1,2}. {word.Word.ToUpperInvariant(),-12} {word.Damage,5:0.0} {EmojiCatalog.Heart}{powerLabel}  ({word.TilesUsed} {EmojiCatalog.Tile}, adj {word.AdjustedLength}){bonuses}";
				})
				.ToList();

			ResultsList.ItemsSource = lines;
			UpdateResultsEmptyState(lines.Count == 0);
			ResultsCountBadge.Text = lines.Count == 0
				? dropInMode ? "Drop-in mode · no words" : "No words found"
				: dropInMode
					? $"{lines.Count} suggestions · Apply to confirm"
					: $"{lines.Count} suggestions";
			UpdateBoardCountBadge();
		}

		private void UpdateResultsEmptyState(bool showEmpty)
		{
			EmptyResultsText.Visibility = showEmpty ? Visibility.Visible : Visibility.Collapsed;
			ResultsList.Visibility = showEmpty ? Visibility.Collapsed : Visibility.Visible;
		}

		private void ShowStatus(string message, InfoBarSeverity severity, bool showInfoBar = true)
		{
			StatusText.Text = message;
			if (!showInfoBar || severity == InfoBarSeverity.Informational)
			{
				StatusInfoBar.IsOpen = false;
				return;
			}

			StatusInfoBar.Severity = severity;
			StatusInfoBar.Message = message;
			StatusInfoBar.IsOpen = true;
		}
	}
}
