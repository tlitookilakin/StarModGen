using Fluid;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StarModGen.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace StarModGen.Generators
{
	// TODO generics???
	[Generator]
	public class AssetGenerator : IIncrementalGenerator
	{
		private static IFluidTemplate? Template;
		private static IFluidTemplate? HelperTemplate;
		private static readonly TemplateOptions options = new();
		private static readonly FluidParser parser = new();

		public void Initialize(IncrementalGeneratorInitializationContext ctx)
		{
			options.MemberAccessStrategy.Register<TemplateData>();
			options.MemberAccessStrategy.Register<AssetEntryMethod>();
			options.MemberAccessStrategy.Register<AssetProperty>();
			options.MemberAccessStrategy.Register<AssetGroup>();
			options.MemberAccessStrategy.Register<AssetEdit>();
			options.MemberAccessStrategy.Register<AssetLoad>();
			options.MemberAccessStrategy.Register<DirectAsset>();
			options.Filters.AddFilter("VarName", Utilities.VarNameFilter);
			options.Filters.AddFilter("MakeLocal", Utilities.MakeLocalFilter);

			options.ValueConverters.Add(
				static v => v is IGrouping<string, DirectAsset> g ? new StringGroupConverter<DirectAsset>(g) : null
			);

			var assetEntries = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetEntryMethod>("StarModGen.Lib.AssetEntryAttribute",
				static (node, cancel) => node is MethodDeclarationSyntax m && m.Modifiers.Any(SyntaxKind.PartialKeyword),
				static (ctx, cancel) => new(
					ctx.TargetSymbol.ContainingType.ContainingNamespace?.ToDisplayString(), 
					ctx.TargetSymbol.ContainingType.Name, ctx.TargetSymbol.Name,
					ctx.TargetSymbol.ContainingType.AllInterfaces.Any(static i => i.Name == nameof(INotifyPropertyChanged)),
					ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					ctx.TargetSymbol.DeclaredAccessibility.ToSyntax()
				)
			);

			var assetProps = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetProperty>("StarModGen.Lib.AssetAttribute", 
				static (node, cancel) => node is PropertyDeclarationSyntax p && p.Modifiers.Any(SyntaxKind.PartialKeyword), 
				static (ctx, cancel) => {
					var args = ctx.Attributes[0].ConstructorArguments;
					return new(
						ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						ctx.TargetSymbol.Name,
						((IPropertySymbol)ctx.TargetSymbol).Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						((string)args[0].Value!), 
						args.Length > 1 ? (string?)args[1].Value : null,
						ctx.TargetSymbol.DeclaredAccessibility.ToSyntax(),
						((string)args[0].Value!).MakeLocal()
					);
				}
			);

			var assetEdits = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetEdit>("StarModGen.Lib.AssetEditAttribute",
				static (node, cancel) => node is MethodDeclarationSyntax m && m.ParameterList.Parameters.Count is 1,
				static (ctx, cancel) =>
				{
					var args = ctx.Attributes[0].ConstructorArguments;
					return new(
						ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						ctx.TargetSymbol.Name,
						((string)args[0].Value!)
					);
				}
			);

			var assetLoads = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetLoad>("StarModGen.Lib.AssetLoadAttribute",
				static (node, cancel) => node is MethodDeclarationSyntax m && m.ParameterList.Parameters.Count is 0,
				static (ctx, cancel) =>
				{
					var args = ctx.Attributes[0].ConstructorArguments;
					return new(
						ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						ctx.TargetSymbol.Name,
						((string)args[0].Value!)
					);
				}
			);

			var DirectEdits = ctx.AdditionalTextsProvider
				.Combine(ctx.AnalyzerConfigOptionsProvider)
				.Select(static (p, c) => {
					var opts = p.Right.GetOptions(p.Left);
					p.Right.GlobalOptions.TryGetValue("build_property.projectdir", out var root);

					bool isMerge = false;
					string? target = null;

					if (opts.TryGetValue("build_metadata.AdditionalFiles.GameAssetLoad", out var load) && load.Length != 0)
					{
						isMerge = false;
						target = load;
					}
					if (opts.TryGetValue("build_metadata.AdditionalFiles.GameAssetMerge", out var merge) && merge.Length != 0)
					{
						isMerge = true;
						target = merge;
					}

					return new DirectAsset(
						target, p.Left.Path.WithoutPrefix(root).Replace('\\', '/'), isMerge,
						opts.TryGetValue("GameAssetPriority", out var priority) && priority.Length != 0 ? ProcessPriority(priority) : null,
						p.Left.Path.GuessTypeByName()
					);
				})
				.Where(static p => p.Target != null);

			var grouped = assetEntries
				.Combine(assetProps.Collect())
				.Combine(assetEdits.Collect())
				.Combine(assetLoads.Collect());

			ctx.RegisterPostInitializationOutput(static ctx => 
			{
				Utilities.AddIncludes(ctx.AddSource, "AssetHelper");
			});

			ctx.RegisterImplementationSourceOutput(grouped, static (ctx, val) => 
			{
				Write(val.Left.Left.Left, val.Left.Left.Right, val.Left.Right, val.Right, ctx);
			});

			ctx.RegisterImplementationSourceOutput(DirectEdits.Collect(), static (ctx, edits) => 
			{
				WriteHelper(edits, ctx);
			});

			if(!parser.TryParse(Utilities.GetTemplate("AssetManager"), out Template, out string error))
				throw new Exception(error);

			if (!parser.TryParse(Utilities.GetTemplate("AssetHelper"), out HelperTemplate, out error))
				throw new Exception(error);
		}

		private record struct AssetProperty(string Type, string Prop, string PropType, string Asset, string? Local, string access, string RawAsset);
		private record struct AssetEntryMethod(string? Space, string Type, string Method, bool IsNotify, string fullName, string access);
		private record struct TemplateData(
			AssetEntryMethod Entry, IReadOnlyList<AssetProperty> LocalProps, 
			IEnumerable<AssetGroup> Assets
		);
		private record struct AssetEdit(string Owner, string Method, string Target);
		private record struct AssetLoad(string Owner, string Method, string Target);
		private record struct DirectAsset(string? Target, string Source, bool Merge, string? Priority, string LoadType);
		private record class HelperData(IReadOnlyList<IGrouping<string, DirectAsset>> Assets, bool DumpLang);

		private class AssetGroup
		{
			public string Target;
			public AssetProperty? Prop;
			public readonly IList<AssetEdit> Edits = [];
			public AssetLoad? Load;
			public string VarName;

			private AssetGroup(string Target)
			{
				this.Target = Target.MakeLocal();
				this.VarName = Target.ToVarname();
			}

			public AssetGroup(AssetProperty Prop) : this(Prop.Asset)
			{
				this.Prop = Prop;
			}

			public AssetGroup(AssetLoad Load) : this(Load.Target)
			{
				this.Load = Load;
			}

			public AssetGroup(string Target, IList<AssetEdit> Edits) : this(Target)
			{
				this.Edits = Edits;
			}

			public bool HasAnyHandlers
				=> Prop?.Local is not null || Load is not null || Edits.Count > 0;
		}

		private static string ProcessPriority(string original)
		{
			if (int.TryParse(original, out int n))
				return $"({{0}}){n}";
			return $"{{0}}.{original}";
		}

		private static void WriteHelper(IReadOnlyList<DirectAsset> directs, SourceProductionContext ctx)
		{
			if (HelperTemplate is null)
				return;

			var grouped = directs.GroupBy(static l => l.Target).ToList();
			var data = new HelperData(grouped!, true);
			ctx.AddSource("AssetHelper.impl", HelperTemplate.Render(new(data, options)));
		}

		private static void Write(
			AssetEntryMethod entry, IReadOnlyList<AssetProperty> props, IReadOnlyList<AssetEdit> edits, 
			IReadOnlyList<AssetLoad> loads,  SourceProductionContext ctx
		)
		{
			if (Template is null)
				return;

			var file = $"{entry.Space}.{entry.Type}.impl";
			string fullName = entry.fullName;
			List<AssetProperty> LocalProps = [];
			Dictionary<string, AssetGroup> Groups = [];

			foreach (var p in props)
			{
				if (p.Type != fullName)
					continue;

				LocalProps.Add(p);
				if (Groups.TryGetValue(p.Asset, out var g))
				{
					g.Prop = p;
					continue;
				}

				Groups.Add(p.Asset, new(p));
			}

			foreach (var e in edits)
			{
				if (e.Owner != fullName)
					continue;

				if (!Groups.TryGetValue(e.Target, out var g))
					Groups.Add(e.Target, new(e.Target, [e]));
				else
					g.Edits.Add(e);
			}

			foreach (var l in loads)
			{
				if (l.Owner != fullName)
					continue;

				if (!Groups.TryGetValue(l.Target, out var g))
					Groups.Add(l.Target, new(l));
				else
					g.Load = l;
			}

			var data = new TemplateData(entry, props, Groups.Values);
			ctx.AddSource(file, Template.Render(new(data, options)));
		}
	}
}
