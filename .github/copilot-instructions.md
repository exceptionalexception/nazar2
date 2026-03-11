# nopCommerce Copilot Instructions

## Build and test commands

Use the SDK pinned by `global.json` and the shared project settings in `src/Directory.Build.props` (`net10.0` in this working tree).

```powershell
dotnet restore .\src\NopCommerce.sln
dotnet build .\src\NopCommerce.sln
dotnet publish .\src\Presentation\Nop.Web\Nop.Web.csproj -c Release
dotnet run --project .\src\Presentation\Nop.Web\Nop.Web.csproj

dotnet test .\src\Tests\Nop.Tests\Nop.Tests.csproj
dotnet test .\src\Tests\Nop.Tests\Nop.Tests.csproj --filter "FullyQualifiedName~Nop.Tests.Nop.Services.Tests.Catalog.ProductServiceTests"
dotnet test .\src\Tests\Nop.Tests\Nop.Tests.csproj --filter "FullyQualifiedName~Nop.Tests.Nop.Services.Tests.Catalog.ProductServiceTests.CanParseRequiredProductIds"

dotnet build .\src\Plugins\Nop.Plugin.Widgets.Swiper\Nop.Plugin.Widgets.Swiper.csproj
```

`src\deploy.cmd` is the reference publish flow for deployments: restore the solution, then publish `Presentation\Nop.Web\Nop.Web.csproj` in Release mode.

## High-level architecture

- `src\NopCommerce.sln` is one .NET solution split into four main areas: `Libraries`, `Presentation`, `Plugins`, and `Tests`.
- `src\Libraries\Nop.Core` contains shared primitives and infrastructure such as `BaseEntity`, configuration types, the engine, and type discovery.
- `src\Libraries\Nop.Data` holds repository and migration infrastructure. The main abstraction is `IRepository<TEntity>`, and data/migration work lives here rather than in controllers.
- `src\Libraries\Nop.Services` contains application/business services plus cross-cutting pieces such as plugins and events.
- `src\Presentation\Nop.Web.Framework` provides shared web infrastructure; `src\Presentation\Nop.Web` is the storefront/admin ASP.NET Core app.
- `src\Plugins` contains first-party extension projects. Plugin projects are built straight into `src\Presentation\Nop.Web\Plugins\{Group}.{SystemName}` and copy `plugin.json`, views, assets, and other content into that output folder.
- `src\Tests\Nop.Tests` is the main automated test project. It exercises services, web behaviors, and plugin/event interactions from a single NUnit-based test assembly.

### Startup and composition

- `src\Presentation\Nop.Web\Program.cs` uses the minimal hosting model, loads `appsettings*.json`, then calls `ConfigureApplicationSettings`, `ConfigureApplicationServices`, and `ConfigureRequestPipeline`.
- `src\Libraries\Nop.Core\Infrastructure\NopEngine.cs` discovers every `INopStartup` implementation via the type finder, orders them by `Order`, and runs them for both DI registration and middleware/pipeline setup.
- `src\Presentation\Nop.Web.Framework\Infrastructure\Extensions\ServiceCollectionExtensions.cs` initializes plugins before the engine configures services, so plugin application parts and settings are available during startup.
- Plugin projects can participate in startup too. Several plugins ship `Infrastructure\NopStartup.cs` or `Infrastructure\PluginNopStartup.cs` classes that implement `INopStartup`, so changes to plugin DI or middleware usually belong there instead of being wired manually in `Program.cs`.

### Web layer shape

- `src\Presentation\Nop.Web\Infrastructure\NopStartup.cs` registers a large set of admin and public `*ModelFactory` services.
- Controllers usually depend on services plus one or more model factories. For example, `Areas\Admin\Controllers\ProductController.cs` injects `IProductModelFactory`, and the heavy view-model preparation lives in `Areas\Admin\Factories\ProductModelFactory.cs`.
- When changing MVC flows, expect related logic to be split across controller, model factory, model, validator, and view files.

### Plugin shape

- Each plugin has a `plugin.json` manifest with the plugin group, system name, supported versions, assembly file name, and display metadata.
- The main plugin class typically inherits `BasePlugin` and one or more extension interfaces such as `IWidgetPlugin`.
- Plugin install/uninstall/update behavior belongs on the plugin class (`InstallAsync`, `UninstallAsync`, `UpdateAsync`), while DI registration, event consumers, and supporting services live beside it under `Infrastructure`, `Services`, `Domain`, `Controllers`, `Views`, and `Content`.
- Plugin `.csproj` files usually include a `NopTarget` MSBuild target that runs `Build\ClearPluginAssemblies.proj` after build.
- Plugin settings are stored via `ISettings`-implementing classes. Use `_settingService.LoadSettingAsync<TSettings>(storeId)` to read and `_settingService.SaveSettingAsync(settings)` to persist. Register locale strings in `InstallAsync` with `_localizationService.AddOrUpdateLocaleResourceAsync(...)` and remove them in `UninstallAsync`.

### Test shape

- `src\Tests\Nop.Tests\Nop.Tests.csproj` uses NUnit, FluentAssertions, and Moq.
- `BaseNopTest` builds a service provider, initializes the database/application state, and registers the same kinds of services used by the app.
- `Nop.Services.Tests\ServiceTest` extends that setup with in-memory plugin descriptors for common shipping, tax, payment, and discount test doubles.
- Test namespaces mirror the source area being exercised, so the fastest way to find related tests is usually to look for the matching namespace/folder under `src\Tests\Nop.Tests`.

## Key conventions

- Follow `.editorconfig`: C# uses 4-space indentation, file-scoped namespaces, `_camelCase` private/protected fields, `I`-prefixed interfaces, `UPPER_CASE` constants, and `Async` suffixes for async methods. Use `var` when the type is a built-in or is apparent from context.
- Prefer existing async APIs. The repository layer exposes both sync and async methods, but service, web, plugin, and test code in this repo is predominantly async-first.
- Keep the layer boundaries intact: entities/config primitives in `Nop.Core`, repository/migration concerns in `Nop.Data`, business logic in `Nop.Services`, and HTTP/view-model composition in `Presentation`.
- In admin and public MVC code, do not move large mapping/preparation logic into controllers if a model factory already owns that responsibility.
- For cross-cutting reactions, look for `IEventPublisher` and `IConsumer<TEvent>` before adding direct service-to-service coupling.
- When editing plugins, keep `plugin.json`, the plugin main class, startup/event wiring, and copied content/output conventions in sync; plugin changes often require touching more than one of those surfaces.
- `IRepository<TEntity>.GetByIdAsync` and `GetAllAsync` accept an optional cache key lambda (`getCacheKey`) — pass one in hot paths to benefit from the built-in cache layer.
- Entities that implement `ISoftDeletedEntity` are soft-deleted by default. Pass `includeDeleted: false` to repository queries when soft-deleted records should be excluded.
