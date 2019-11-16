using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace ColorBot
{
	public class Settings
	{
		public string Token { get; set; }
		public long ClientID { get; set; }
		public long Permissions { get; set; }
		public Dictionary<string, string> ApprovedColors { get; set; }
		public string ImageURL { get; set; }
		public string ColorRolePositionName { get; set; }
		public DateTime LastRolePurge { get; set; }
		public int PurgeCooldownInSeconds { get; set; }

		public void StoreSettings(string path="settings.json")
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, json);
		}

		public static Settings FromFile(string path="settings.json")
		{
			return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
		}
	}
}
