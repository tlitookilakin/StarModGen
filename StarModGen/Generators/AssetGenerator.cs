using Fluid;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
		private IFluidTemplate? Template;
		private IFluidTemplate? Constants;
		private readonly TemplateOptions options = new();
		private readonly FluidParser parser = new();

		public void Initialize(IncrementalGeneratorInitializationContext ctx)
		{
			options.MemberAccessStrategy.Register<TemplateData>();
			options.MemberAccessStrategy.Register<AssetEntryMethod>();
			options.MemberAccessStrategy.Register<AssetProperty>();
			options.MemberAccessStrategy.Register<ConstantData>();
			options.MemberAccessStrategy.Register<AssetGroup>();
			options.MemberAccessStrategy.Register<AssetEdit>();
			options.MemberAccessStrategy.Register<AssetLoad>();
			options.MemberAccessStrategy.Register<AssetInclude>();
			options.MemberAccessStrategy.Register<KeyValuePair<string, string>>();
			options.MemberAccessStrategy.Register(typeof(IList<>));

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

			var assetIncludes = ctx.SyntaxProvider.ForAttributeWithMetadataName<AssetInclude>("StarModGen.Lib.AssetIncludeAttribute",
				static (node, cancel) => node is MethodDeclarationSyntax m,
				static (ctx, cancel) =>
				{
					var args = ctx.Attributes[0].ConstructorArguments;
					return new(
						ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
						((string)args[0].Value!),
						((string)args[1].Value!),
						((string)args[0].Value!).ToVarname()
					);
				}
			);

			var grouped = assetEntries
				.Combine(assetProps.Collect())
				.Combine(assetIncludes.Collect())
				.Combine(assetEdits.Collect())
				.Combine(assetLoads.Collect());

			var cfg = ctx.AnalyzerConfigOptionsProvider.Select(static (cfg, cancel) =>
				cfg.GlobalOptions.TryGetValue("build_property.UniqueId", out var v) ? v : ""
			);

			ctx.RegisterPostInitializationOutput(static ctx => 
			{
				ctx.AddIncludes("AssetHelper");
			});

			ctx.RegisterSourceOutput(cfg, (ctx, cfg) =>
			{
				var data = new ConstantData(cfg);
				ctx.AddSource("Constants", Constants.Render(new(data, options)));
			});

			ctx.RegisterImplementationSourceOutput(grouped, (ctx, val) => 
			{
				Write(val.Left.Left.Left.Left, val.Left.Left.Left.Right, val.Left.Left.Right, val.Left.Right, val.Right, ctx);
			});

			if(!parser.TryParse(Utilities.GetTemplate("AssetManager"), out Template, out string error))
				throw new Exception(error);

			if(!parser.TryParse(Utilities.GetTemplate("Constants"), out Constants, out error))
				throw new Exception(error);
		}

		private record struct AssetProperty(string Type, string Prop, string PropType, string Asset, string? Local, string access, string RawAsset);
		private record struct AssetEntryMethod(string? Space, string Type, string Method, bool IsNotify, string fullName, string access);
		private record struct TemplateData(
			AssetEntryMethod Entry, IReadOnlyList<AssetProperty> LocalProps, 
			IEnumerable<AssetGroup> Assets, IEnumerable<KeyValuePair<string, string>> IncludeSources
		);
		private record struct AssetInclude(string Owner, string Target, string Source, string SourceVar);
		private record struct AssetEdit(string Owner, string Method, string Target);
		private record struct AssetLoad(string Owner, string Method, string Target);
		private record struct ConstantData(string ModId);
		private record struct AssetGroup(
			string Target, AssetProperty? Prop, IList<AssetEdit> Edits, 
			AssetLoad? Load, IList<AssetInclude> Includes, string VarName
		);

		private void Write(
			AssetEntryMethod entry, IReadOnlyList<AssetProperty> props, IReadOnlyList<AssetInclude> includes, 
			IReadOnlyList<AssetEdit> edits, IReadOnlyList<AssetLoad> loads, SourceProductionContext ctx
		)
		{
			if (Template is null)
				return;

			var file = $"{entry.Space}.{entry.Type}.impl";
			string fullName = entry.fullName;
			List<AssetProperty> LocalProps = [];
			Dictionary<string, string> IncludeSources = [];
			Dictionary<string, AssetGroup> Groups = [];

			foreach (var p in props)
			{
				if (p.Type != fullName)
					continue;

				LocalProps.Add(p);
				if (!Groups.TryGetValue(p.Asset, out var g))
					Groups.Add(p.Asset, new(p.Asset.MakeLocal(), p, [], null, [], p.Asset.ToVarname()));
				else
					g.Prop = p;
			}

			foreach (var e in edits)
			{
				if (e.Owner != fullName)
					continue;

				if (!Groups.TryGetValue(e.Target, out var g))
					Groups.Add(e.Target, g = new(e.Target.MakeLocal(), null, [], null, [], e.Target.ToVarname()));
				g.Edits.Add(e);
			}

			foreach (var l in loads)
			{
				if (l.Owner != fullName)
					continue;

				if (!Groups.TryGetValue(l.Target, out var g))
					Groups.Add(l.Target, new(l.Target.MakeLocal(), null, [], l, [], l.Target.ToVarname()));
				else
					g.Load = l;
			}

			foreach (var i in includes)
			{
				if (i.Owner != fullName)
					continue;

				IncludeSources[i.Source] = i.SourceVar;
				if (!Groups.TryGetValue(i.Target, out var g))
					Groups.Add(i.Target, g = new(i.Target.MakeLocal(), null, [], null, [], i.Target.ToVarname()));
				g.Includes.Add(i);
			}

			var data = new TemplateData(entry, props, Groups.Values, IncludeSources);
			ctx.AddSource(file, Template.Render(new(data, options)));
		}
	}
}
