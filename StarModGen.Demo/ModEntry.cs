using StardewModdingAPI;
using StardModGen.Utils;
using StarModGen.Lib;

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
		}
	}
}
