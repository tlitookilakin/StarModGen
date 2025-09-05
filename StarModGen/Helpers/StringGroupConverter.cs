using Fluid;
using Fluid.Values;
using System.Collections.Generic;
using System.Linq;

namespace StarModGen.Helpers
{
	public class StringGroupConverter<T> : ObjectValueBase
	{
		public StringGroupConverter(IGrouping<string?, T> value) : base(value)
		{
		}

		public override string ToStringValue()
		{
			return ((IGrouping<string?, T>)Value).Key ?? "";
		}

		public override bool ToBooleanValue()
		{
			return ToStringValue().Length is not 0;
		}

		public override IEnumerable<FluidValue> Enumerate(TemplateContext context)
		{
			return ((IGrouping<string?, T>)Value).Select(static v => new ObjectValue(v));
		}
	}
}
