using StarModGen.Lib;

namespace StarModGen.Demo
{
	[Config(false)]
	public partial class Config
	{
		public enum Styles { Checkered, Striped, Plain, Spotted }

		[ConfigValue("Name")]
		public string ThingName { get; set; }

		[ConfigValue(0, "Pricing")]
		public int ThingPrice { get; set; }

		[ConfigValue(Styles.Checkered, null)]
		public Styles ThingStyle { get; set; }
	}
}
