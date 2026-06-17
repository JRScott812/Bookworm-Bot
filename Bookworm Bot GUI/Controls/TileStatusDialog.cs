using System;

using System.Linq;



using Bookworm_Bot_Class;



using Microsoft.UI.Xaml;

using Microsoft.UI.Xaml.Controls;

using Microsoft.UI.Xaml.Controls.Primitives;

using Microsoft.UI.Xaml.Media;



namespace Bookworm_Bot_GUI.Controls

{

	public sealed class TileStatusDialog : ContentDialog

	{

		private readonly GridCell _initial;

		private GemType _selectedGem = GemType.None;

		private TileModifier _selectedModifier = TileModifier.None;



		public GridCell Result { get; private set; } = GridCell.Empty;



		public TileStatusDialog(GridCell initial)

		{

			_initial = initial;

			Title = "Tile status";

			PrimaryButtonText = "OK";

			CloseButtonText = "Cancel";

			DefaultButton = ContentDialogButton.Primary;



			_selectedGem = initial.Gem;

			_selectedModifier = initial.Modifier;



			string letterLabel = initial.IsEmpty

				? "— type a letter in the tile first —"

				: initial.Letter == 'q' ? "qu" : initial.Letter!.Value.ToString().ToUpperInvariant();



			StackPanel content = new() { Spacing = 12, MinWidth = 320 };

			content.Children.Add(new TextBlock

			{

				Text = $"Letter: {letterLabel}",

				Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],

				Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]

			});



			if (!initial.IsEmpty)

			{

				StackPanel gemRow = new() { Orientation = Orientation.Horizontal, Spacing = 4 };

				AddGemButton(gemRow, GemType.None);

				foreach (GemType gem in Enum.GetValues<GemType>())

				{

					if (gem == GemType.None)

					{

						continue;

					}



					AddGemButton(gemRow, gem);

				}



				StackPanel modifierRow = new() { Orientation = Orientation.Horizontal, Spacing = 4 };

				AddModifierButton(modifierRow, TileModifier.None, "—");

				AddModifierButton(modifierRow, TileModifier.Locked, EmojiCatalog.ForModifier(TileModifier.Locked));

				AddModifierButton(modifierRow, TileModifier.Cracked, EmojiCatalog.ForModifier(TileModifier.Cracked));

				AddModifierButton(modifierRow, TileModifier.Burning, EmojiCatalog.ForModifier(TileModifier.Burning));



				content.Children.Add(new TextBlock { Text = "Gem", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"], Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });

				content.Children.Add(gemRow);

				content.Children.Add(new TextBlock { Text = "Status", Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"], Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] });

				content.Children.Add(modifierRow);

			}



			Content = content;



			PrimaryButtonClick += (_, _) => Result = BuildResult();

		}



		private void AddGemButton(StackPanel row, GemType gem)

		{

			UIElement content = gem == GemType.None

				? new TextBlock

				{

					Text = "—",

					HorizontalAlignment = HorizontalAlignment.Center,

					VerticalAlignment = VerticalAlignment.Center

				}

				: new Border

				{

					Width = 24,

					Height = 24,

					CornerRadius = new CornerRadius(4),

					Background = GemVisuals.GetBackgroundBrush(gem),

					BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],

					BorderThickness = new Thickness(1)

				};



			ToggleButton button = new()

			{

				Content = content,

				IsChecked = _selectedGem == gem,

				MinWidth = 36,

				MinHeight = 36,

				Padding = new Thickness(4)

			};



			if (gem != GemType.None)

			{

				ToolTipService.SetToolTip(button, GemVisuals.Label(gem));

			}

			button.Click += (_, _) =>

			{

				_selectedGem = gem;

				foreach (ToggleButton sibling in row.Children.OfType<ToggleButton>())

				{

					sibling.IsChecked = ReferenceEquals(sibling, button);

				}

			};

			row.Children.Add(button);

		}



		private void AddModifierButton(StackPanel row, TileModifier modifier, string label)

		{

			ToggleButton button = new()

			{

				Content = label,

				IsChecked = _selectedModifier == modifier,

				MinWidth = 36

			};

			button.Click += (_, _) =>

			{

				_selectedModifier = modifier;

				foreach (ToggleButton sibling in row.Children.OfType<ToggleButton>())

				{

					sibling.IsChecked = ReferenceEquals(sibling, button);

				}

			};

			row.Children.Add(button);

		}



		private GridCell BuildResult()

		{

			if (_initial.IsEmpty)

			{

				return GridCell.Empty;

			}



			return new GridCell(_initial.Letter!.Value, _selectedGem, _selectedModifier);

		}

	}

}


