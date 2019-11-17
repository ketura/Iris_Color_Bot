using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace ColorBot
{
	public class Settings
	{
		public const string DefaultPath = "app/settings.json";

		public string Token { get; set; } = "SET TOKEN HERE";
		public long ClientID { get; set; } = 643998202823180307;
		public long Permissions { get; set; } = 268528640;
		public Dictionary<string, string> ApprovedColors { get; set; }
		public string ImageURL { get; set; }
		public DateTime LastRolePurge { get; set; }
		public int PurgeCooldownInSeconds { get; set; } = 30;

		public void StoreSettings(string path= DefaultPath)
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, json);
		}

		public static Settings FromFile(string path= DefaultPath)
		{
			if(!File.Exists(path))
			{
				Settings settings = new Settings();

				settings.StoreSettings(path);
			}
			return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
		}
	}
}
