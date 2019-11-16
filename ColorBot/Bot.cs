using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ColorBot
{
	public class Bot
	{
		private Settings CurrentSettings { get; set; }

		public async Task Initialize()
		{
			var discord = new DiscordClient(new DiscordConfiguration
			{
				Token = CurrentSettings.Token,
				TokenType = TokenType.Bot,
				UseInternalLogHandler = true,
				LogLevel = LogLevel.Debug

			});



			var commands = discord.UseCommandsNext(new CommandsNextConfiguration
			{
				StringPrefixes = new string[] { "/" },
				EnableDefaultHelp = false,
				IgnoreExtraArguments = true
			});


			commands.RegisterCommands<ColorCommands>();
			commands.RegisterCommands<ModCommands>();

			ColorCommands.CurrentSettings = CurrentSettings;
			ModCommands.CurrentSettings = CurrentSettings;
			ColorRegistry.CurrentSettings = CurrentSettings;


			await discord.ConnectAsync();


		}

		public Bot(Settings settings)
		{
			CurrentSettings = settings;
		}
	}
}
