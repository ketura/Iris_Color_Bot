using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace ColorBot
{
	public static class ColorRegistry
	{
		public static Settings CurrentSettings { get; set; }
		public static List<DiscordRole> GetCustomColorRoles(DiscordGuild server)
		{
			var roles = new List<DiscordRole>();

			foreach(var role in server.Roles.Values)
			{
				if(role.Name.StartsWith("#"))
				{
					string name = role.Name.Replace("#", "").ToLower();
					if (CurrentSettings.ApprovedColors.ContainsKey(name))
						continue;

					roles.Add(role);
				}
			}

			return roles;
		}

		public static async Task<DiscordRole> CreateColorRole(CommandContext context, DiscordColor newColor,
			string colorname)
		{
			if (!colorname.StartsWith("#"))
			{
				colorname = "#" + colorname;
			}

			return await context.Guild.CreateRoleAsync(colorname, color: newColor,
				reason: $"Created by Iris bot upon {context.Member.Mention}'s request.",
				permissions: Permissions.None,
				hoist: false,
				mentionable: false);
		}
	}
}
