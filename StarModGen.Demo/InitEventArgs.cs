using StardewModdingAPI;

namespace StarModGen.Demo
{
	internal class InitEventArgs(IMonitor monitor, IModHelper helper)
	{
		public IMonitor Monitor => monitor;
		public IModHelper Helper => helper;
	}
}
