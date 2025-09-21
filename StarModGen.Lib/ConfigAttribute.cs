using System;
using System.Diagnostics;

#pragma warning disable CS9113
namespace StarModGen.Lib
{
	/// <summary>Marks a class as a config.</summary>
	/// <param name="TitleOnly">Whether it should only be editable on the title screen</param>
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Class)]
	public class ConfigAttribute(bool TitleOnly) : Attribute
	{
	}
}
