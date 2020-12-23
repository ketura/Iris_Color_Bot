using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace ColorBot
{
    public class Bot
    {
        public Bot(Settings settings)
        {
            CurrentSettings = settings;
        }

        private Settings CurrentSettings { get; }

        public async Task Initialize()
        {
            var discord = new DiscordClient(new DiscordConfiguration
            {
                Token = CurrentSettings.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] {"/"},
                EnableDefaultHelp = false,
                IgnoreExtraArguments = true,
                EnableDms = false,
                EnableMentionPrefix = false
            });

            commands.RegisterCommands<ColorCommands>();
            commands.RegisterCommands<ModCommands>();


            discord.ClientErrored += Client_ClientError;
            commands.CommandExecuted += Commands_CommandExecuted;
            commands.CommandErrored += Commands_CommandErrored;

            ColorCommands.CurrentSettings = CurrentSettings;
            ModCommands.CurrentSettings = CurrentSettings;
            ColorRegistry.CurrentSettings = CurrentSettings;


            await discord.ConnectAsync();
        }

        private Task Client_ClientError(DiscordClient client, ClientErrorEventArgs e)
        {
            // let's log the details of the error that just 
            // occured in our client
            client.Logger.Log(LogLevel.Error, "ExampleBot",
                $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            // let's log the name of the command and user
            e.Context.Client.Logger.Log(LogLevel.Information, "ExampleBot",
                $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            // let's log the error details
            e.Context.Client.Logger.Log(LogLevel.Error, "ExampleBot",
                $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}",
                DateTime.Now);
            
            if (string.IsNullOrEmpty(e.Command?.QualifiedName))
            {
                // No need to log in channel when probably just no matching command (user said /s?)
                return;
            }

            var emoji = ":warning:";
            var message = $"An error occurred executing {e.Command?.Name}";
            if (e.Exception is ChecksFailedException cfe)
            {
                var check = cfe.FailedChecks.FirstOrDefault();
                if (check is RequireUserPermissionsAttribute rupe)
                {
                    emoji = ":no_entry:";
                    message = $"You do not have the permissions [{rupe.Permissions}] which are required to execute this command.";
                }
                else
                {
                    message = $"Check failed: {check?.GetType().Name}";
                }
            }

            // let's wrap the response into an embed
            var embed = new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = $"{DiscordEmoji.FromName(e.Context.Client, emoji)} {message}",
                Color = new DiscordColor(0xFF0000) // red
            };
            await e.Context.RespondAsync("", embed: embed);
        }
    }
}