using Fluid;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StarModGen.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarModGen.Generators;

[Generator]
public class EventGenerator : IIncrementalGenerator
{
	private static IFluidTemplate? Template;
	private static readonly TemplateOptions options = new();
	private static readonly FluidParser parser = new();

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		options.MemberAccessStrategy.Register<EventTarget>();
		options.MemberAccessStrategy.Register<EventSource>();
		options.MemberAccessStrategy.Register<TemplateData>();
		options.ValueConverters.Add(
			static o => o is IGrouping<string?, EventTarget> g ? new StringGroupConverter<EventTarget>(g) : null
		);

		var targets = context.SyntaxProvider.ForAttributeWithMetadataName<EventTarget>("StarModGen.Lib.ModEventAttribute",
			static (node, c) => node is MethodDeclarationSyntax m && m.ParameterList.Parameters.Count is 2,
			static (ctx, c) => new(
				ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				ctx.TargetSymbol.Name,
				((IMethodSymbol)ctx.TargetSymbol).Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
			)
		);

		var sources = context.SyntaxProvider.ForAttributeWithMetadataName<EventSource>("StarModGen.Lib.ModEventAttribute",
			static (node, c) => 
			{
				var parent = node.Parent?.Parent;
				if (parent is EventFieldDeclarationSyntax or EventDeclarationSyntax)
				{
					var mods = ((MemberDeclarationSyntax)parent).Modifiers;
					return mods.Any(SyntaxKind.StaticKeyword) && (mods.Any(SyntaxKind.PublicKeyword) || mods.Any(SyntaxKind.InternalKeyword));
				}
				return false;
			},
			static (ctx, c) => new(
				ctx.TargetSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + '.' +
				ctx.TargetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				ProcessEventType((ctx.TargetSymbol as IEventSymbol)?.Type)
			)
		);

		var combined = 
			targets.Collect()
			.Combine(sources.Collect());


		context.RegisterPostInitializationOutput(static ctx =>
		{
			Utilities.AddIncludes(ctx.AddSource, "EventBusStub");
		});

		context.RegisterImplementationSourceOutput(combined, (ctx, g) =>
		{
			ctx.AddSource($"EventBus", Render(g.Left, g.Right));
		});

		if (!parser.TryParse(Utilities.GetTemplate("EventBus"), out Template, out string error))
			throw new Exception(error);
	}

	private record struct EventTarget(string owner, string method, string type);
	private record struct EventSource(string source, string? type);
	private class TemplateData(IList<IGrouping<string, EventTarget>> handlers, IList<EventTarget> targets, IList<EventSource> sources)
	{
		public IList<IGrouping<string, EventTarget>> Handlers => handlers;
		public IList<EventTarget> Targets => targets;
		public IList<EventSource> Sources => sources;
	}

	private static string Render(IList<EventTarget> targets, IList<EventSource> modSources)
	{
		for (int i = modSources.Count - 1; i >= 0; i--)
			if (modSources[i].type is null)
				modSources.RemoveAt(i);

		var groups = targets.ToLookup(
			t => 
			{
				if (t.type.StartsWith("global::StardewModdingAPI.Events."))
					return TryMapSMAPIEvent(t.type[33..]);

				foreach (var source in modSources)
					if (source.type == t.type)
						return source.source;

				return null;
			}
		).ToList();

		return Template.Render(new(new TemplateData(groups!, targets, modSources), options));
	}

	private static string? ProcessEventType(ITypeSymbol? type)
	{
		if (type is not INamedTypeSymbol named)
			return null;

		if (named.Name is not "EventHandler")
			return null;

		return named.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
	}

	private static string? TryMapSMAPIEvent(string type)
	{
		return "Helper.Events." + type switch
		{
			"AssetRequestedEventArgs" => "Content.AssetRequested",
			"AssetsInvalidatedEventArgs" => "Content.AssetsInvalidated",
			"AssetReadyEventArgs" => "Content.AssetReady",
			"LocaleChangedEventArgs" => "Content.LocaleChanged",
			"MenuChangedEventArgs" => "Display.MenuChanged",
			"RenderingStepEventArgs" => "Display.RenderingStep",
			"RenderedStepEventArgs" => "Display.RenderedStep",
			"RenderingEventArgs" => "Display.Rendering",
			"RenderedEventArgs" => "Display.Rendered",
			"RenderingWorldEventArgs" => "Display.RenderingWorld",
			"RenderedWorldEventArgs" => "Display.RenderedWorld",
			"RenderingActiveMenuEventArgs" => "Display.RenderingActiveMenu",
			"RenderedActiveMenuEventArgs" => "Display.RenderedActiveMenu",
			"RenderingHudEventArgs" => "Display.RenderingHud",
			"RenderedHudEventArgs" => "Display.RenderedHud",
			"WindowResizedEventArgs" => "Display.WindowResized",
			"GameLaunchedEventArgs" => "GameLoop.GameLaunched",
			"UpdateTickingEventArgs" => "GameLoop.UpdateTicking",
			"UpdateTickedEventArgs" => "GameLoop.UpdateTicked",
			"OneSecondUpdateTickingEventArgs" => "GameLoop.OneSecondUpdateTicking",
			"OneSecondUpdateTickedEventArgs" => "GameLoop.OneSecondUpdateTicked",
			"SaveCreatingEventArgs" => "GameLoop.SaveCreating",
			"SaveCreatedEventArgs" => "GameLoop.SaveCreated",
			"SavingEventArgs" => "GameLoop.Saving",
			"SavedEventArgs" => "GameLoop.Saved",
			"SaveLoadedEventArgs" => "GameLoop.SaveLoaded",
			"DayStartedEventArgs" => "GameLoop.DayStarted",
			"DayEndingEventArgs" => "GameLoop.DayEnding",
			"TimeChangedEventArgs" => "GameLoop.TimeChanged",
			"ReturnedToTitleEventArgs" => "GameLoop.ReturnedToTitle",
			"ButtonsChangedEventArgs" => "Input.ButtonsChanged",
			"ButtonPressedEventArgs" => "Input.ButtonPressed",
			"ButtonReleasedEventArgs" => "Input.ButtonReleased",
			"CursorMovedEventArgs" => "Input.CursorMoved",
			"MouseWheelScrolledEventArgs" => "Input.MouseWheelScrolled",
			"PeerContextReceivedEventArgs" => "Multiplayer.PeerContextReceived",
			"PeerConnectedEventArgs" => "Multiplayer.PeerConnected",
			"ModMessageReceivedEventArgs" => "Multiplayer.ModMessageReceived",
			"PeerDisconnectedEventArgs" => "Multiplayer.PeerDisconnected",
			"InventoryChangedEventArgs" => "Player.InventoryChanged",
			"LevelChangedEventArgs" => "Player.LevelChanged",
			"WarpedEventArgs" => "Player.Warped",
			"LoadStageChangedEventArgs" => "Specialized.LoadStageChanged",
			"UnvalidatedUpdateTickingEventArgs" => "Specialized.UnvalidatedUpdateTicking",
			"UnvalidatedUpdateTickedEventArgs" => "Specialized.UnvalidatedUpdateTicked",
			"LocationListChangedEventArgs" => "World.LocationListChanged",
			"BuildingListChangedEventArgs" => "World.BuildingListChanged",
			"DebrisListChangedEventArgs" => "World.DebrisListChanged",
			"LargeTerrainFeatureListChangedEventArgs" => "World.LargeTerrainFeatureListChanged",
			"NpcListChangedEventArgs" => "World.NpcListChanged",
			"ObjectListChangedEventArgs" => "World.ObjectListChanged",
			"ChestInventoryChangedEventArgs" => "World.ChestInventoryChanged",
			"TerrainFeatureListChangedEventArgs" => "World.TerrainFeatureListChanged",
			"FurnitureListChangedEventArgs" => "World.FurnitureListChanged",
			_ => null
		};
	}
}
