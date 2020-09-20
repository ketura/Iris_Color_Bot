using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ColorBot
{
	public class ColorCommands : BaseCommandModule
	{
		public static Settings CurrentSettings;

		[Command("colourclear")]
		public async Task ClearColourUser(CommandContext context)
		{
			await ClearColorUser(context);
		}
		[Command("colorclear")]
		public async Task ClearColorUser(CommandContext context)
		{
			await context.TriggerTypingAsync();
			bool removed = await RemoveColorRolesFromUser(context);
			if (removed)
			{
				await context.RespondAsync($"All back to normal, {context.User.Mention}!");
			}
			else
			{
				await context.RespondAsync($"You don't have any roles for me to remove, {context.User.Mention}! Stop pestering me.");
			}

			//this is not awaited on purpose, since it's a long operation.  Let it run and the connection wither.
			ColorCommands.PurgeRoles(context);
		}

		public async Task<bool> RemoveColorRolesFromUser(CommandContext context)
		{
			bool removed = false;
			foreach (var role in context.Member.Roles.ToList())
			{
				if (role.Name.StartsWith("#"))
				{
					try
					{
						await context.Member.RevokeRoleAsync(role);
						removed = true;
					}
					catch
					{
						await context.RespondAsync($"Oops!  I couldn't remove your role, {context.User.Mention}!  This is probably because the moderators forgot to put my role above all color roles.  Get them to fix it, tee hee!");
						return false;
					}
				}
			}

			return removed;
		}



		[Command("colourme")]
		public async Task ColourUser(CommandContext context, string colorname=null)
		{
			await Color(context, colorname, false);
		}
		[Command("colorme")]
		public async Task ColorUser(CommandContext context, string colorname = null)
		{
			await Color(context, colorname, true);
		}

		public async Task ColorError(CommandContext context, bool american = true)
		{
			string color = american ? "color" : "colour";
			await context.RespondAsync($"You need to list a valid {color}, {context.User.Mention}! Use a vetted {color} name (darkred) or hex coding (#RRGGBB), so `/{color}me darkred`, or `/{color}me #00FF00`.  ");
		}

		public async Task Color(CommandContext context, string colorname, bool american=true)
		{
			string color = american ? "color" : "colour";

			if (String.IsNullOrWhiteSpace(colorname))
			{
				await ColorError(context, american);
				return;
			}

			colorname = colorname.ToLower();

			bool foundColor = false;
			DiscordColor newColor = new DiscordColor();

			if (colorname == "rainbow")
			{
				int rand = new Random((int)DateTime.Now.Ticks).Next(0, CurrentSettings.ApprovedColors.Count());
				colorname = CurrentSettings.ApprovedColors.Keys.ElementAt(rand);
				newColor = new DiscordColor(CurrentSettings.ApprovedColors[colorname]);
				foundColor = true;
			}
			else if (colorname == "random")
			{
				var customRoles = ColorRegistry.GetCustomColorRoles(context.Guild);
				int rand = new Random((int)DateTime.Now.Ticks).Next(0, customRoles.Count());
				colorname = customRoles[rand].Name;
				foundColor = true;
			}
			else if (CurrentSettings.ApprovedColors.ContainsKey(colorname.ToLower()))
			{
				colorname = colorname.ToLower();
				newColor = new DiscordColor(CurrentSettings.ApprovedColors[colorname]);
				foundColor = true;
			}
			else
			{
				AnalysisResult result = new AnalysisResult();
				try
				{
					result = ColorAnalyzer.AnalyzeColor(ColorAnalyzer.FromHex(colorname));

					if (result.Passes)
					{
						foundColor = true;
						newColor = new DiscordColor(colorname);
					}
					else
					{
						string message = $"D: Hmm, that {color} won't work, {context.User.Mention}!  It has a dark theme contrast of {result.DarkRatio} (needs to be >= {ColorAnalyzer.MinimumDarkContrast}), and a light theme ratio of {result.LightRatio} (needs to be >= {ColorAnalyzer.MinimumLightContrast}).";
						await context.RespondAsync(message);
						return;
					}
				}
				catch
				{
					await ColorError(context, american);
					return;
				}
				
				
			}

			if(!foundColor)
			{
				await ColorError(context, american);
				return;
			}

			await RemoveColorRolesFromUser(context);

			var roles = context.Guild.Roles;

			if(CurrentSettings.ApprovedColors.ContainsKey(colorname) && roles.Values.Any(x => x.Name.ToLower().Contains(colorname)))
			{
				var newrole = roles.Values.Where(x => x.Name.Contains(colorname)).First();
				await context.Member.GrantRoleAsync(newrole, "Added by Iris bot upon user's request.");
			}
			else if(roles.Values.Any(x => x.Name.ToLower().Contains(colorname)))
			{
				var newrole = roles.Values.Where(x => x.Name.ToLower().Contains(colorname)).First();
				await context.Member.GrantRoleAsync(newrole, "Added by Iris bot upon user's request.");
			}
			else
			{
				var newrole = await ColorRegistry.CreateColorRole(context, newColor, colorname);
				await context.Member.GrantRoleAsync(newrole, "Added by Iris bot upon user's request.");
			}

			await context.RespondAsync($"One paint job coming right up, {context.User.Mention}!");

			//this is not awaited on purpose, since it's a long operation.  Let it run and the connection wither.
			ColorCommands.PurgeRoles(context);
		}


		[Command("colourlist")]
		public async Task ListColours(CommandContext context)
		{
			await ListColors(context, false, false);
		}
		[Command("colorlist")]
		public async Task ListColors(CommandContext context)
		{
			await ListColors(context, false, true);
		}

		[Command("colourlistfull")]
		public async Task ListColoursFull(CommandContext context)
		{
			await ListColors(context, true, false);
		}
		[Command("colorlistfull")]
		public async Task ListColorsFull(CommandContext context)
		{
			await ListColors(context, true, true);
		}

		private async Task ListColors(CommandContext context, bool includeCustomRoles, bool american=true)
		{
			string color = american ? "color" : "colour";

			string message = $"Here you go, {context.User.Mention}! Here are all the pre-vetted {color} options:\n";

			var embed = new DiscordEmbedBuilder()
			{
				Color = new DiscordColor("#FF0000")
			};

			if (!String.IsNullOrWhiteSpace(CurrentSettings.ImageURL))
			{
				message += $"\n{CurrentSettings.ImageURL}";
			}
			else
			{
				int columns = 0;
				foreach (var pair in CurrentSettings.ApprovedColors)
				{

					embed.AddField(pair.Key, pair.Value, true);
					columns++;
				}
			}

			await context.RespondAsync(message, embed:embed.Build());

			if (includeCustomRoles)
			{
				var roles = ColorRegistry.GetCustomColorRoles(context.Guild);
				message = $"\n\nAnd here are the custom {color} roles currently in use by other members of the server:\n";
				embed = new DiscordEmbedBuilder()
				{
					Color = new DiscordColor("#FF0000")
				};
				for (int i = 0; i < roles.Count(); i++)
				{
					embed.AddField((i+1).ToString(), $"<@&{roles[i].Id}>", true);
				}
				
				await context.RespondAsync(message, embed: embed.Build());
			}

			
		}



		[Command("colourtest")]
		public async Task TestColour(CommandContext context, string colorcode=null)
		{
			await TestColor(context, colorcode, false);
		}

		[Command("colortest")]
		public async Task TestColor(CommandContext context, string colorcode = null)
		{
			await TestColor(context, colorcode, true);
		}

		public async Task TestColor(CommandContext context, string colorcode, bool american=true)
		{
			string color = american ? "color" : "colour";

			if(String.IsNullOrWhiteSpace(colorcode))
			{
				var embed = new DiscordEmbedBuilder()
				{
					Color = new DiscordColor("#FF0000")
				};
				embed.AddField($"Usage", $"`/{color}test #RRGGBB`.  If you don't provide a hex {color} code, this message will show.");
				embed.AddField($"Summary", $"Dark background is `#2C2F33`, and light background is `#FFFFFF`.  Chosen {color} roles must have a contrast ratio of at least {ColorAnalyzer.MinimumDarkContrast} : 1 on the dark mode background, and at least {ColorAnalyzer.MinimumLightContrast} : 1 on the light.");
				embed.AddField($"Contrast", $@"
It's very easy to pick a {color} that isn't readable on one or the other of the two themes that Discord provides.  To help keep our eyes from melting any more than they already are from looking at discord 12 hours a day, this bot will enforce contrast standards when selecting {color} roles.

Official guidelines suggest a ratio of 4.5 : 1 for text : background, but in our experience this is overkill for usernames.  We expect names to have at least a {ColorAnalyzer.MinimumDarkContrast} : 1 ratio on the dark mode background, and at least {ColorAnalyzer.MinimumLightContrast} : 1 on the (second-hand citizen) light mode background.

If you're having trouble finding a {color} that meets this bot's standards, try using the following online utility to search for suitable colors: https://webaim.org/resources/contrastchecker/ .");
				embed.AddField($"Further Reading", $"See this link for more details on contrast ratios: https://www.boia.org/blog/how-contrast-ratio-pertains-to-website-accessibility");

				await context.RespondAsync($"I don't see a {color}, {context.User.Mention}! Here is some more information on how to use `/{color}test`:", embed:embed.Build());
				return;
			}

			Color parsedColor = new Color();
			try
			{
				parsedColor = ColorAnalyzer.FromHex(colorcode);

				var result = ColorAnalyzer.AnalyzeColor(parsedColor);
				string message = "";
				if (result.Passes)
				{
					message = $":D That {color} would work, {context.User.Mention}!  It has a dark theme contrast of {result.DarkRatio}, and a light theme ratio of {result.LightRatio}.";
				}
				else
				{
					message = $"D: Hmm, that {color} won't work, {context.User.Mention}!  It has a dark theme contrast of {result.DarkRatio} (needs to be >= {ColorAnalyzer.MinimumDarkContrast}), and a light theme ratio of {result.LightRatio} (needs to be >= {ColorAnalyzer.MinimumLightContrast}).";
				}

				await context.RespondAsync(message);
			}
			catch
			{
				await context.RespondAsync($"I'm sorry {context.User.Mention}, but I can't parse '`{colorcode}`' as a valid color!  It should be a hex code in the format `#RRGGBB`.");
				return;
			}

			
		}


		[Command("colour")]
		public async Task BritHelpError(CommandContext context)
		{
			await BritHelp(context);
		}
		[Command("color")]
		public async Task HelpError(CommandContext context)
		{
			await Help(context);
		}

		[Command("colour")]
		public async Task BritHelpErrorArgs(CommandContext context, string _)
		{
			await BritHelp(context);
		}
		[Command("color")]
		public async Task HelpErrorArgs(CommandContext context, string _)
		{
			await Help(context);
		}

		[Command("colourhelp")]
		public async Task BritHelp(CommandContext context)
		{
			await HelpMessage(context, false, context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.ManageRoles));
		}

		[Command("colorhelp")]
		[Aliases("help")]
		public async Task Help(CommandContext context)
		{
			await HelpMessage(context, true, context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.ManageRoles));
		}

		private async Task HelpMessage(CommandContext context, bool american=true, bool mod=false)
		{
			string color = "color";
			if(!american)
			{
				color = "colour";
			}

			var embed = new DiscordEmbedBuilder()
			{
				Color = new DiscordColor("#FF0000")
			};
			embed.AddField($"`/{color}list`", $"Displays a list of all contrast-vetted default {color}s.");
			embed.AddField($"`/{color}listfull`", $"Displays a list of all contrast-vetted default {color}s, plus all {color}s that all users have chosen on this server.");
			embed.AddField($"`/{color}test #RRGGBB`", $"Checks to see if the provided {color} will pass the contrast requirements. Use this command by itself for more information on contrast requirements.");
			embed.AddField($"`\n\n/{color}me new{color}`", $"Grants you the {color} you request, if it passes the contrast tests.  `new{color}` can either be the name of a {color} as shown in `/{color}list`, or it can be a hex value in the format #RRGGBB.  For example, `/{color}me darkred`, or `/{color}me #00FF00`.");
			embed.AddField($"`/{color}me rainbow` / `/{color}me random` ", $"`rainbow` selects a random color from all the pre-vetted colors, and `random` selects a random color from among all custom user-submitted colors.");
			embed.AddField($"`/{color}clear`", $"Removes your {color} and returns you to the default grey.");
			embed.AddField($"`\n\n/{color}help`", $"Shows this help message.");

			if(mod)
			{
				embed.AddField($"`/{color}add {color}name hexcode`", $"(Mod Only) add a new {color} to the default vetted list");
				embed.AddField($"`/{color}delete {color}name`", $"(Mod Only) deletes a {color} from the default vetted list");
				embed.AddField($"`/{color}purge`", $"(Mod Only) kicks off the purge process to clear the role list of all roles starting with # that are not currently in use.");
				embed.AddField($"`/{color}analyze`", $"(Mod Only) analyzes all {color} roles on the server (that start with #) and gives a readout indicating whether they meet current contrast requirements.");
			}

			embed.AddField($"\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_", $@"
And yes, {(american ? "British" : "American")} spelling is tolerated.  Barely.

Please message ketura in #rules_and_inquiries if you are having trouble.

Happy {color}ing!");


			var finalEmbed = embed.Build();
			await context.RespondAsync($"Hi, {context.User.Mention}! Here is a list of available commands relating to {color} configuration:", embed:finalEmbed);
		}


		public static async Task PurgeRoles(CommandContext context, bool triggered = false)
		{
			if (!triggered && DateTime.Now - CurrentSettings.LastRolePurge < TimeSpan.FromSeconds(CurrentSettings.PurgeCooldownInSeconds))
			{
				Log(context, "Skipping purge due to cooldown.");
				return;
			}

			string suspicious = null;
			CurrentSettings.LastRolePurge = DateTime.Now;
			CurrentSettings.StoreSettings();

			var colorRoles = context.Guild.Roles.Values.Where(x => x.Name.StartsWith("#")).ToDictionary(x => x.Id, y => 0);

			if(colorRoles.Count == 0)
			{
				Log(context, "No roles found!  Skipping purge.");
				return;
			}

			Log(context, "Beginning purge....");
			var members = context.Guild.Members.Values.ToList();
			if (members.Count < 10)
			{
				Log(context, "Suspiciously small members list, skipping this purge.");
				suspicious = $"Member list too small: {members.Count}";
			}
			
			foreach (var member in members)
			{
				foreach(var role in member.Roles)
				{
					if(colorRoles.ContainsKey(role.Id))
					{
						colorRoles[role.Id] += 1;
					}
				}
			}
			Log(context, "Purge analysis complete.");

			var colorsToPurge = colorRoles.Keys.Where(x => colorRoles[x] == 0).ToList();
			if (colorsToPurge.Count > 10)
			{
				Log(context, "Suspiciously high number of colours to purge, skipping this purge");
				suspicious = $"Too many colours to purge {colorsToPurge.Count}";
			}
			
			List<string> colorNames = new List<string>();

			foreach(var roleID in colorsToPurge.ToList())
			{
				var role = context.Guild.GetRole(roleID);
				Log(context, $"Purging {role.Name}...");
				if (suspicious != null)
				{
					continue;
				}
				
				try
				{
					await role.DeleteAsync("Iris Bot purging unused color roles.");
					colorNames.Add(role.Name);
				}
				catch
				{
					if(triggered)
					{
						await context.RespondAsync($"An error occured while attempting to purge {role.Name}, {context.User.Mention}! Is it correctly positioned beneath my own role?  If not, you'll have to have ketura look at the logs.");
					}
					else
					{
						await context.RespondAsync($"An error occured while attempting to purge {role.Name}, <@224253035788894218>! Fix it!");
					}
				}
				
			}

			Log(context, $"Purge complete. Suspicious: {suspicious}");

			if(triggered)
			{
				if (suspicious != null)
				{
					await context.RespondAsync(
						$"Purge failed, {context.User.Mention}! Purged {colorNames.Count} roles ({String.Join(',', colorNames)}).\nSuspicious: {suspicious}");
				}
				else
				{
					await context.RespondAsync(
						$"Purge complete, {context.User.Mention}! Purged {colorNames.Count} roles ({String.Join(',', colorNames)}).");
				}
			}

		}

		[Command("ispy")]
		public async Task EasterEgg(CommandContext context, string _=null)
		{
			foreach (var role in context.Member.Roles.ToList())
			{
				if (role.Name.StartsWith("#"))
				{
					await context.RespondAsync($"I spy with my rainbow eye, something...<@&{role.Id}>!");
					return;
				}
			}

			await context.RespondAsync($"I spy with my rainbow eye, something...drab and grey!");

		}

		public static void Log(CommandContext context, string message, LogLevel level=LogLevel.Info)
		{
			context.Client.DebugLogger.LogMessage(level, "IrisBot", message, DateTime.Now);
		}

		

	}

	[RequirePermissions(Permissions.ManageRoles)]
	public class ModCommands : BaseCommandModule
	{
		public static Settings CurrentSettings { get; set; }

		[Command("colouradd")]
		public async Task ModColourAdd(CommandContext context, string name, string hex)
		{
			await ModColorAdd(context, name, hex);
		}
		[Command("coloradd")]
		public async Task ModColorAdd(CommandContext context, string name, string hex)
		{
			if (CurrentSettings.ApprovedColors.ContainsKey(name))
			{
				await context.RespondAsync($"That name '{name}' is already in the list, {context.User.Mention}!");
				return;
			}

			if(!ColorAnalyzer.IsValid(hex))
			{
				await context.RespondAsync($"That hex '{hex}' isn't a valid code, {context.User.Mention}! Should be in the format `#RRGGBB`.");
				return;
			}

			if (CurrentSettings.ApprovedColors.Values.Contains(hex))
			{
				await context.RespondAsync($"That hex '{hex}' is already in the list, {context.User.Mention}!");
				return;
			}

			CurrentSettings.ApprovedColors.Add(name, hex);
			CurrentSettings.StoreSettings();

			await context.RespondAsync($"Adding `{hex}` to the approved list as '{name}', {context.User.Mention}!  You might want to ask ketura to update the preview image.");
		}

		[Command("colourdelete")]
		public async Task ModColourDelete(CommandContext context, string name)
		{
			await ModColorDelete(context, name);
		}
		[Command("colordelete")]
		public async Task ModColorDelete(CommandContext context, string name)
		{
			if (!CurrentSettings.ApprovedColors.ContainsKey(name))
			{
				await context.RespondAsync($"That name '{name}' isn't in the list, {context.User.Mention}!");
				return;
			}

			CurrentSettings.ApprovedColors.Remove(name);
			CurrentSettings.StoreSettings();

			await context.RespondAsync($"Deleting '{name}' from the approved list, {context.User.Mention}!");
		}

		[Command("colourpurge")]
		public async Task ModColourPurge(CommandContext context)
		{
			await ModColorPurge(context);
		}
		[Command("colorpurge")]
		public async Task ModColorPurge(CommandContext context)
		{
			await context.RespondAsync($"Running a purge, {context.User.Mention}!");
			await ColorCommands.PurgeRoles(context, true);
		}

		[Command("colouranalyse")]
		public async Task ModColourAnalyze(CommandContext context)
		{
			await ModColorAnalyze(context);
		}
		[Command("coloranalyze")]
		public async Task ModColorAnalyze(CommandContext context)
		{

			var colorRoles = context.Guild.Roles.Values.Where(x => x.Name.StartsWith("#")).ToDictionary(x => x.Id, y => y);

			var colorResults = new Dictionary<bool, List<ulong>>();
			colorResults[false] = new List<ulong>();
			colorResults[true] = new List<ulong>();

			foreach (var pair in colorRoles)
			{
				var dColor = pair.Value.Color;
				var color = Color.FromArgb(dColor.R, dColor.G, dColor.B);
				var result = ColorAnalyzer.AnalyzeColor(color);
				colorResults[result.Passes].Add(pair.Value.Id);
			}

			var embed = new DiscordEmbedBuilder()
			{
				Color = new DiscordColor("#FF0000")
			};
			List<string> passed = colorResults[true].Select(x => $"<@&{x}>").ToList();
			List<string> failed = colorResults[false].Select(x => $"<@&{x}>").ToList();

			if(passed.Any())
			{
				embed.AddField($"Meets Contrast Requirements", String.Join(',', passed));
			}
			
			if(failed.Any())
			{
				embed.AddField($"Does Not Meet Contrast Requirements", String.Join(',', failed));
			}
			
			if(!passed.Any() && !failed.Any())
			{
				await context.RespondAsync($"Looks like there's no roles for me to look at, {context.User.Mention}!");
			}
			await context.RespondAsync($"Analysis results of existing roles, {context.User.Mention}:", embed:embed.Build());
		}

		[Command("colordebugadd")]
		public async Task ModColorDebug(CommandContext context)
		{
			var roles = context.Guild.Roles;

			foreach(var pair in CurrentSettings.ApprovedColors)
			{
				string colorname = pair.Key;

				if (roles.Values.Any(x => x.Name.ToLower().Contains(colorname)))
					continue;

				
				var newColor = new DiscordColor(CurrentSettings.ApprovedColors[colorname]);
				await ColorRegistry.CreateColorRole(context, newColor, colorname);
			}

			await context.RespondAsync($"Added roles, {context.User.Mention}!");
		}
	}
}
