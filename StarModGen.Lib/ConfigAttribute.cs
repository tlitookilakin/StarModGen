using System;
using System.Diagnostics;

#pragma warning disable CS9113
namespace StarModGen.Lib
{
	[Conditional("STARMOD_ATTRS")]
	[AttributeUsage(AttributeTargets.Class)]
	public class ConfigAttribute(bool TitleOnly) : Attribute
	{
	}
}
