# StarModGen
StarModGen is a source generator library designed to automate some common, tedious tasks associated with modding Stardew Valley.

## Installing
To add StarModGen to your project, simply add the following line to your .csproj inside an `ItemGroup`:
```xml
<PackageReference Include="tlitookilakin.StarModGen" Version="0.1.2" ExcludeAssets="runtime"/>
```
And make sure to specify your mod's `UniqueId` in a `PropertyGroup` as well.
Though it's not required, StarModGen also works well with the [ModManifestBuilder Package](https://www.nuget.org/packages/Leclair.Stardew.ModManifestBuilder/).

## Examples
TODO

## Features
### Assets
StarModGen provides several new tools to help manage game assets. The first of these are asset management attributes. Inside a partial class, choose a partial method that returns `void` and accepts only an `IModHelper`, and mark it with the `AssetEntry` attribute to mark it as the entrypoint for asset management on that class. Once you've done that, you can then use the other attributes. This method should be called to intialize the class before use.

- `Asset` can be used to create a lazy-loading property backed by a game asset. You can also include the path to a local file to automatically load that asset from.
- `AssetEdit` defines a method as an editor for a specific asset.
- `AssetLoad` defines a method as a provider/loader for a specific asset.

In addition to the management attributes, it also offers a way to load mod assets into the game without a property, and merge local mod jsons into dictionary-type assets, via build properties. Simply use these properties on `AdditionalFiles` elements in your `.csproj`.

- `GameAssetMerge` will merge a json dictionary into an existing dictionary game asset.
- `GameAssetLoad` will load a local file to a specific game asset automatically.
- `GameAssetPriority` is optional and can be used with either `GameAssetLoad` or `GameAssetMerge` to change the priority of the merge/load.

Finally, StarModGen will automatically dump your translation keys into a new asset at `Mods/<mod id>/Strings` for convenient access from tokenized strings and other game content. If you are not using an asset manager class, or are not always initializing it, some features will require manually calling `StarModGen.Utils.AssetHelper.Init()`.

### Configs
StarModGen provides a new `Config` attribute, which can be used to designate a class as a config. StarModGen will handle Generic Mod Config Menu registration, reading/writing the config using SMAPI, and the various GMCM lifecycle events. Any visible properties should be annotated with `ConfigValue`, providing a default value for the property and optionally a page name. Then, the config can be read/initialized by calling the generated static method `Create` from your config type. (Ex. `MyConfig.Create(Helper)`).

For `float` and `int` properties, a `ConfigRange` attribute can also be supplied to define minimum, maximum, and step/interval values for the option. These will be provided to GMCM, and if the property is `partial`, an implementation enforcing these rules will also be generated.

StarModGen will also generate several static events to allow easily modifying behavior.
- Registering: An `EventHandler<IGMCM>` that runs after the config is initialized with GMCM but before options are added to the menu.
- Registered: An `EventHandler<IGMCM>` that runs after all options are added to the menu.
- Applied: An `Action<T>` where `T` is your config type. This runs when changes made in GMCM are applied to your config, after the file is written to disk.
- Reset: An `Action<T>` where `T` is your config type. This runs when a user resets your config to default values, after the values have been changed.

StarModGen also automatically exports all of your mod's translations to a string asset, which can be used with tokenized text, among other things. It is located at `Mods/YourModUniqueId/Strings`.

### Events
StarModGen adds a new `EventBus` class and an associated `ModEvent` attribute for event management. The attribute can be placed either on a static event, or on a method that is a valid event handler. To use, simply add the Attribute where needed, and then call `EventBus.Register`. Events are expected to be `EventHandler`-based and and connected based on the even type.

### Other
StarModGen adds some global constants that can be used anywhere, for your convenience.
- `MOD_ID` is a string set to your mod's unique id.
- `LANG_PATH` is a string set to the name of your mod's strings asset.

If harmony is enabled, it also adds a harmony wrapper called `HarmonyHelper` that can simplify the most common types of harmony patch.