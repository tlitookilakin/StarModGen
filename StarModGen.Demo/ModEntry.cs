using StardewModdingAPI;
using StardModGen.Utils;
using StarModGen.Lib;
using System.Reflection;

namespace StarModGen.Demo
{
	public class ModEntry : Mod
	{
		internal static Assets Assets;
		internal static Config config;

		[ModEvent]
		internal static event EventHandler<InitEventArgs>? OnInit;

		public override void Entry(IModHelper helper)
		{
			EventBus.Register(helper);

			Assets = new();
			Assets.Setup(helper);

			config = Config.Create(helper, ModManifest);
			OnInit?.Invoke(this, new(Monitor, Helper));

			var path = GetType().Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "ProjectPath")?.Value ?? "";
		}
	}
}
