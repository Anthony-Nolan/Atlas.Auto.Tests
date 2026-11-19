# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Automated end-to-end test suite for the Atlas donor search algorithm service. Tests run against a real, deployed Atlas instance (Dev/UAT/PR/etc.) — there is no mocking of Atlas itself. See the [main Atlas repository](http://github.com/Anthony-Nolan/Atlas) for the system under test.

**Prerequisites for running tests locally:**
- Target Atlas instance must be version 2.1.0+.
- The haplotype frequency set `Atlas.Auto.Tests/TestHelpers/SourceData/autotest-HF-set.json` must already be loaded into the target Atlas instance, and the Atlas HLA metadata dictionary must hold that set's nomenclature version.

## Commands

```
dotnet build
dotnet test
dotnet test --filter TestCategory=Search_HappyPathTests   # run one category (see [Category] on each fixture)
dotnet test --filter FullyQualifiedName~Search_HappyPathTests.Search_Cord_4_8_ReturnsExpectedSearchResult   # single test
```

Configuration (`Atlas.Auto.Tests/appsettings.json`) holds no secrets: the Public API `BaseUrl`, the Azure resources (subscription, resource group, function app names, App Insights workspace) and `KeyVault.Name`, the environment's Atlas Key Vault. Values that must be set default to `"override-this"`. Override locally with .NET user secrets from the `Atlas.Auto.Tests` project directory, nesting with `:` (a user secret named with `.` overrides nothing):
```
dotnet user-secrets set "KeyVault:Name" "an-dev-atlas-kv"
```
A local run also needs your IP allowed by the firewall of the environment's Atlas SQL server.
In the pipeline, the equivalent variables use `.` for nesting (e.g. `KeyVault.Name`) and are injected via `FileTransform@2` into `appsettings.json`. They live in a variable group named like the pipeline without its square brackets (pipeline "[DEV-ATLAS] Atlas E2E Tests", group "DEV-ATLAS Atlas E2E Tests"), which `test-pipeline.yml` links through `Build.DefinitionName`, so each pipeline only ever gets its own environment's settings. The NuGet feed for Atlas packages is set via `ATLAS_AZURE_ARTIFACTS_FEED_NAME_OR_ID`.

Secrets are never settings, pipeline variables or part of the build artifact:
- The connection strings (SQL, Service Bus, Blob) are read from the Atlas Key Vault at start-up (`TestHelpers/Settings/KeyVaultSecrets.cs` maps each setting to its secret). The Public API key is the Public API app's default host key, read through ARM (`PublicApiKeyProvider`). A user secret with the setting's name (e.g. `Database:Sql`, `PublicApi:ApiKey`) still wins.
- Azure access (ARM, App Insights, Key Vault) uses `AzureCredentialFactory`: in the pipeline `AzurePipelinesCredential` for the pipeline's WIF service connection (`azureServiceConnections` in `test-pipeline.yml`, by pipeline name: Azure DevOps only issues tokens for a connection a task of the stage names at compile time; the 'Azure identity of the tests' AzureCLI@2 step names it and passes its IDs to the test steps), locally `AzureCliCredential` (`az login`; `Azure:TenantId` picks the tenant).
- Don't add a secret to `appsettings.json` or a pipeline variable: FileTransform would write it into `appsettings.json`, which is published in the `dropTestSuite` artifact. Add it to the Key Vault mapping instead.

## Architecture

Single project:
- **`Atlas.Auto.Tests`** — the NUnit test suite. Depends on `Atlas.Client.Models` and `Atlas.DonorImport.FileSchema.Models` NuGet packages for shared DTOs, and connects directly to Atlas infrastructure (SQL, blob storage, Service Bus, Azure ARM, App Insights).

### Layered test structure

Each functional area (Search, RepeatSearch, Scoring, DonorImport) follows the same layered pattern, wired through `Microsoft.Extensions.DependencyInjection`:

1. **Services** (`TestHelpers/Services/**`) — thin wrappers around one infrastructure call each. `DonorStoreSqlHelper` queries the donor databases via EF Core, `BlobStorageHelper` uploads donor import files and downloads result sets, `PublicApiClient` calls the Atlas public API, `FunctionAppHelper` reads Azure Function App settings via ARM, `AppInsightsHelper` queries App Insights for HLA expansion failures.
2. **Workflows** (`TestHelpers/Workflows/*.cs`) — compose several Services into one logical operation for a feature area (e.g. `DonorImportWorkflow.ImportDonorFile` → `FetchResultMessage` → `CheckDonorsInDonorStore`). Workflows are the boundary that test steps talk to; they never call Services directly across areas.
3. **TestSteps** (`TestHelpers/TestSteps/*.cs`) — Gherkin-style, human-readable step methods (e.g. `CreateDonor`, `SubmitSearchRequest`, `MatchingShouldReturnExpectedDonor`) that combine one or more Workflow calls plus assertions. This is what test methods actually call. `DonorImportStepsForSearchTests` composes donor-import steps as a helper reused by search/scoring/repeat-search tests that need a donor set up first.
4. **Tests** (`Tests/<Area>/*.cs`) — NUnit fixtures. Each area has a `*TestBase` (e.g. `DonorImportTestBase` extends `TestBase`) that wires up the relevant TestSteps, plus `*_HappyPathTests` / `*_ExceptionPathTests` fixtures containing the actual `[Test]`/`[TestCaseSource]` methods.

Wiring: `ServiceConfiguration.CreateProvider()` (`DependencyInjection/ServiceConfiguration.cs`) builds the DI container — loads `appsettings.json` + user secrets, then the secrets from the Key Vault, registers EF Core context factories (`AtlasContext`, `AtlasMatchingContextFactory`), Azure clients (Service Bus, Blob Storage, ARM), and all Service/Workflow helpers as singletons. `TestBase` exposes the singleton provider.

### Search-family tests (Search, RepeatSearch, Scoring)

- Request bodies live as JSON files under `TestHelpers/SourceData/` (embedded resources), keeping test methods free of inline HLA data.
- Snapshot testing via `Verify.NUnit` checks that Atlas returns the expected search/matching result. Snapshots live in `TestHelpers/Assertions/Approvals/*.verified.txt` and must be updated (and reviewed) when expected results legitimately change. Fields expected to vary per run (request IDs, generated donor IDs) are excluded from the comparison — see `TestHelpers/Extensions/VerifySettingsExtensions.cs`.
- Search-type tests take a `bool? parallelMatchPrediction` (`SearchTestBase.Cases()` — null/true/false) to exercise the default, new, and old match-prediction flows.

### Parallelism and CI categories

- Add `[Parallelizable]`/`ParallelScope.All` to new fixtures — tests must be written so they can run in parallel locally without interfering (each test typically imports its own donor).
- Every fixture carries an NUnit `[Category]` matching its class name; a test method can have its own (`RepeatSearchHappyPathTests_Adult`/`_Cord`). In Azure DevOps, `test-pipeline.yml` lists the test runs (Health Check, Donor Import, Search) and their categories; `azure-test-run-template.yml` holds the pool, build and steps. All test runs are steps of one job on one agent, one after another; a test run starts only when the one before it passed. A test run is one `dotnet test --filter "TestCategory=A|TestCategory=B"` call, its tests run in parallel in the process.
- The job publishes one test run per category (`pipeline/Publish-TestRunsByCategory.ps1`, from the TRX and the NUnit result XML) and tags each with the tag its category is listed under in `test-pipeline.yml` (`pipeline/Add-TestRunTags.ps1`; Search, Scoring, RepeatSearch, ...). The job also samples the agent's CPU and memory during each test run (`pipeline/Measure-AgentResources.ps1`, the 'CPU and memory' step). 'Rerun failed jobs' runs only the categories that did not pass (`pipeline/Select-TestCategories.ps1`, from the NUnit XML of the earlier attempts).
- When adding a new test Category, also add it under its tag in `categories` of its test run in `test-pipeline.yml`.
- **Tests that need a function app setting** (a value the environment does not deploy, e.g. a short repeat search wait):
  - Never change the setting inside the test: changing an app setting restarts the function app and breaks other tests running at that time.
  - Add the setting and its test value to `FunctionAppTestSettings` (`TestHelpers/Settings`). The [OneTimeSetUp] of its area sets it before any test of the area and its [OneTimeTearDown] restores the deployed value: `AssemblySetup` for DonorImport (every area imports donors, so before all tests), `RepeatSearchSetup` (a `[SetUpFixture]` in the `Tests.RepeatSearch` namespace) for RepeatSearch. Both go through `FunctionAppTestSettingsSwitcher`, one restart per function app. The deployed values are kept on the function app in the `AutoTests:OriginalSettings` app setting, so a run stopped before its teardown is restored by the next run; a Terraform deployment removes it.
  - Only add a setting that changes nothing other tests rely on: all tests of the area run with it, in parallel. While it is set, it applies to everyone using that environment.
  - A set-up that cannot set them (ARM error, no restart in time) does not fail the run: only the tests marked `[RequiresAppSetting]` for that function app fail, at once, with the reason.
  - Mark the test `[RequiresAppSetting(FunctionAppTestSettings.<Setting>)]`, one per setting (filter: `--filter "RequiresAppSetting=<setting key>"`), and name in its `<remarks>` each setting, its test value, the deployed value and the [OneTimeSetUp] that sets it.
  - `Azure:ChangeFunctionAppSettings` (default true): false changes nothing and skips the tests marked `[RequiresAppSetting]`. In the pipeline it is the run checkbox "Change Atlas function app settings…" (`changeFunctionAppSettings` parameter of `test-pipeline.yml`), which the build job writes into `appsettings.json`.
- **Cleanup strategy**: every test run, a pipeline rerun or a local run included, starts by removing all stale test donors (`TestDonorDeleter` in `AssemblySetup`'s `[OneTimeSetUp]`). It removes every donor of the test registry, so two test runs must not target the same Atlas at the same time (the pipeline runs them one after another): a local run against an Atlas while its pipeline runs deletes the pipeline's donors.

## Versioning against Atlas

This test project tracks the Atlas API version via its `Atlas.Client.Models`/`Atlas.DonorImport.FileSchema.Models` package references (`Atlas.Auto.Tests.csproj`), not its own version number. When a new Atlas version ships:
1. Read the Atlas changelog for breaking changes.
2. Branch as `release/x.y.z` (matching the Atlas version under test), bump the package references to that stable version, run health-check tests locally.
3. Push, run the `release/x.y.z` pipeline against the matching Atlas instance (usually UAT); if green, merge to `main`.
4. The `release/x.y.z` branch is kept (not deleted, not tagged) so a `hotfix/x.y.z` branch can be cut from it later for a fix targeting that specific released version — mirroring the Atlas repo's `release`/`hotfix` branching model.

Existing branches follow this pattern, e.g. `release/3.4.1`, `release/4.0.0`, `hotfix/3.0.1`. Older `atlas/x.y.z` git tags (up to `atlas/2.5.0`) predate this scheme and are no longer created.
