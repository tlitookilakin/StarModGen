using System;
using System.Diagnostics;

namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Method)]
	public class AssetEditAttribute : Attribute
	{
		public string Name { get; set; }
		public AssetEditAttribute(string Name)
		{
			this.Name = Name;
		}
	}
}
