using StardewModdingAPI;
using StardewValley.GameData.Objects;
using StarModGen.Lib;
using System.ComponentModel;

namespace StarModGen.Demo
{
	internal partial class Assets : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		[Asset("/CustomObjects", "assets/CustomObjects.json")]
		public partial Dictionary<string, ObjectData> CustomObjects { get; }

		[AssetEntry]
		public partial void Setup(IModHelper helper);
	}
}
