using System;

using Bookworm_Bot_Class;

using Microsoft.UI.Xaml;

namespace Bookworm_Bot_GUI
{
	public partial class App : Application
	{
		public static GameSession? Session { get; private set; }
		public static WordDictionary? Dictionary { get; private set; }
		public static string? LoadError { get; private set; }
		private Window? _window;
		public App()
		{
			InitializeComponent();
			UnhandledException += OnUnhandledException;
		}

		private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			LoadError = e.Exception.Message;
			e.Handled = true;
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			try
			{
				if (!WordBankPaths.TryGetWordBanksDirectory(out string wordBanksPath))
				{
					LoadError = $"Word Banks folder not found at {WordBankPaths.GetWordBanksDirectory()}";
				}
				else
				{
					Dictionary = WordDictionary.Load(wordBanksPath);
					Session = new GameSession(Dictionary, new AbilityProfile());
				}
			}
			catch (Exception ex)
			{
				LoadError = ex.Message;
			}

			_window = new MainWindow();
			_window.Activate();
		}
	}
}
