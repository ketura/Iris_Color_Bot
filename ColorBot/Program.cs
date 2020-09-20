using System;
using System.Threading.Tasks;

namespace ColorBot
{
	public class Program
	{
		static void Main(string[] args)
		{
			MainAsync().GetAwaiter().GetResult();
		}

		public static async Task MainAsync()
		{
			Bot bot = new Bot(Settings.FromFile());
			await bot.Initialize();

			await Task.Delay(-1);
		}
	}
}
