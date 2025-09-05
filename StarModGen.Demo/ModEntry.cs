using StardewModdingAPI;

namespace StarModGen.Demo
{
	public class ModEntry : Mod
	{
		internal static Assets Assets;

		public override void Entry(IModHelper helper)
		{
			Assets = new();
			Assets.Setup(helper);

			Enum.TryParse<Config.Styles>("", true, out var p);
		}
	}
}
