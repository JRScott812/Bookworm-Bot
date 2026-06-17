using System;
using System.Threading.Tasks;

using Bookworm_Bot_Class;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

using Windows.System;

namespace Bookworm_Bot_GUI.Controls
{
	public sealed class TileCellLetterEventArgs : EventArgs
	{
		public required int CellIndex { get; init; }
		public required string Text { get; init; }
	}

	public sealed partial class TileCellControl : UserControl
	{
		private bool _suppressLetterEvents;

		public event EventHandler<TileCellLetterEventArgs>? LetterCommitted;
		public event EventHandler<int>? StatusEditRequested;

		public int CellIndex
		{
			get => _cellIndex;
			set
			{
				_cellIndex = value;
				LetterBox.TabIndex = value;
			}
		}

		private int _cellIndex;

		private GridCell _lastCell = GridCell.Empty;
		private bool _lastHighlighted;

		public TileCellControl()
		{
			InitializeComponent();
			UpdateVisual(GridCell.Empty, highlighted: false);
			ActualThemeChanged += (_, _) => UpdateVisual(_lastCell, _lastHighlighted);
		}

		public void UpdateVisual(GridCell cell, bool highlighted)
		{
			_lastCell = cell;
			_lastHighlighted = highlighted;

			_suppressLetterEvents = true;
			LetterBox.Text = cell.IsEmpty
				? string.Empty
				: cell.Letter == 'q' ? "qu" : cell.Letter!.Value.ToString().ToUpperInvariant();
			LetterBox.Foreground = GetLetterForeground(cell, highlighted);
			_suppressLetterEvents = false;

			string modifier = EmojiCatalog.ForModifier(cell.Modifier);
			ModifierBadge.Text = modifier;
			ModifierBadge.Visibility = string.IsNullOrEmpty(modifier) ? Visibility.Collapsed : Visibility.Visible;

			TileBorder.Background = GetTileBackground(cell, highlighted);
			TileBorder.BorderBrush = GetBrush(
				highlighted ? "AccentControlElevationBorderBrush" : "ControlStrokeColorDefaultBrush");
			TileBorder.BorderThickness = highlighted ? new Thickness(2) : new Thickness(1);
		}

		public static bool TryParseLetter(string text, out char? letter)
		{
			text = text.Trim().ToLowerInvariant();
			if (string.IsNullOrEmpty(text))
			{
				letter = null;
				return true;
			}

			if (text is "qu")
			{
				letter = 'q';
				return true;
			}

			if (text.Length == 1 && char.IsLetter(text[0]))
			{
				letter = text[0];
				return true;
			}

			letter = null;
			return false;
		}

		private void CommitLetter()
		{
			if (_suppressLetterEvents)
			{
				return;
			}

			LetterCommitted?.Invoke(this, new TileCellLetterEventArgs
			{
				CellIndex = CellIndex,
				Text = LetterBox.Text
			});
		}

		private void LetterBox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (_suppressLetterEvents)
			{
				return;
			}

			_suppressLetterEvents = true;
			LetterBox.Text = string.Empty;
			_suppressLetterEvents = false;
		}

		private void LetterBox_LostFocus(object sender, RoutedEventArgs e) => CommitLetter();

		private void LetterBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				CommitLetter();
				e.Handled = true;
			}
		}

		private void TileBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (IsPointerOverLetterBox(e))
			{
				return;
			}

			StatusEditRequested?.Invoke(this, CellIndex);
			e.Handled = true;
		}

		private bool IsPointerOverLetterBox(PointerRoutedEventArgs e)
		{
			Windows.Foundation.Point point = e.GetCurrentPoint(LetterBox).Position;
			return point.X >= 0
				&& point.Y >= 0
				&& point.X <= LetterBox.ActualWidth
				&& point.Y <= LetterBox.ActualHeight;
		}

		private static Brush GetBrush(string key) => (Brush)Application.Current.Resources[key];

		private static Brush GetTileBackground(GridCell cell, bool highlighted)
		{
			if (highlighted)
			{
				return GetBrush("AccentFillColorDefaultBrush");
			}

			if (cell.IsEmpty)
			{
				return GetBrush("ControlFillColorSecondaryBrush");
			}

			if (cell.Gem != GemType.None)
			{
				return GemVisuals.GetBackgroundBrush(cell.Gem);
			}

			return GetBrush("ControlFillColorDefaultBrush");
		}

		private static Brush GetLetterForeground(GridCell cell, bool highlighted)
		{
			if (highlighted || cell.IsEmpty || cell.Gem == GemType.None)
			{
				return GetBrush("TextFillColorPrimaryBrush");
			}

			return GemVisuals.GetForegroundBrush(cell.Gem);
		}

		public async System.Threading.Tasks.Task PlayDropAnimationAsync(double fromYOffset)
		{
			TranslateTransform transform = new() { Y = fromYOffset };
			TileBorder.RenderTransform = transform;

			try
			{
				DoubleAnimation animation = new()
				{
					From = fromYOffset,
					To = 0,
					Duration = TimeSpan.FromMilliseconds(250),
					EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
				};

				Storyboard storyboard = new();
				storyboard.Children.Add(animation);
				Storyboard.SetTarget(animation, transform);
				Storyboard.SetTargetProperty(animation, "Y");

				TaskCompletionSource<bool> completion = new();
				storyboard.Completed += (_, _) => completion.TrySetResult(true);
				storyboard.Begin();
				await completion.Task;
			}
			finally
			{
				TileBorder.RenderTransform = null;
			}
		}
	}
}
