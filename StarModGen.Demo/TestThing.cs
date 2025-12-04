using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StarModGen.Lib;

namespace StarModGen.Demo
{
	internal class TestThing
	{
		private static IMonitor monitor;

		[ModEvent]
		public static void Init(object? s, InitEventArgs ev)
		{
			monitor = ev.Monitor;
		}

		[ModEvent]
		public static void Launched(object? s, GameLaunchedEventArgs ev)
		{
			monitor.Log("Hello World!");
		}
	}
}
