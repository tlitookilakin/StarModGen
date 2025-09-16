using Fluid;
using Microsoft.CodeAnalysis;
using System;

namespace StarModGen.Generators;

[Generator]
public class StaticGenerator : IIncrementalGenerator
{
	private static IFluidTemplate? Constants;
	private static readonly TemplateOptions options = new();
	private static readonly FluidParser parser = new();

	public void Initialize(IncrementalGeneratorInitializationContext ctx)
	{
		options.MemberAccessStrategy.Register<ProjectFlags>();

		var flags = ctx.AnalyzerConfigOptionsProvider.Select(
			static (cfg, cancel) => new ProjectFlags(
				cfg.GlobalOptions.TryGetValue("build_property.UniqueId", out var v) ? v : "",
				cfg.GlobalOptions.TryGetValue("build_property.EnableHarmony", out v) && v.Equals("true", StringComparison.OrdinalIgnoreCase)
			)
		);

		ctx.RegisterSourceOutput(flags, static (ctx, cfg) =>
		{
			ctx.AddSource("Constants", Constants.Render(new(cfg, options)));
			if (cfg.harmony)
				Utilities.AddIncludes(ctx.AddSource, "HarmonyHelper");
		});

		if (!parser.TryParse(Utilities.GetTemplate("Constants"), out Constants, out var error))
			throw new Exception(error);
	}

	private record struct ProjectFlags(string id, bool harmony);
}
