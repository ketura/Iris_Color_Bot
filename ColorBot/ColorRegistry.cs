using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using DSharpPlus.Entities;

using static ColorBot.ColorAnalyzer;

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

	}
}
