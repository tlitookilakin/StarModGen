using StarModGen.Lib;

namespace StarModGen.Demo
{
	[Config(false)]
	public partial class Config
	{
		public enum Styles { Checkered, Striped, Plain, Spotted }

		[ConfigValue("Name")]
		public string ThingName { get; set; }

		[ConfigRange(Min = 0)]
		[ConfigValue(0, "Pricing")]
		public int ThingPrice { get; set; }

		[ConfigValue(1f)]
		[ConfigRange(Min = 1f, Step = .1f, Max = 5f)]
		public partial float StackMultiplier { get; set; }

		[ConfigValue(Styles.Checkered)]
		public Styles ThingStyle { get; set; }
	}
}
