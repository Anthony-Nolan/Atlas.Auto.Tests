# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Automated end-to-end test suite for the Atlas donor search algorithm service. Tests run against a real, deployed Atlas instance (Dev/UAT/PR/etc.) via its public and debug HTTP APIs — there is no mocking of Atlas itself. See the [main Atlas repository](http://github.com/Anthony-Nolan/Atlas) for the system under test.

**Prerequisites for running tests locally:**
- Target Atlas instance must be version 2.1.0+ (needs the debug endpoints from `Atlas.Debug.Client`).
- The haplotype frequency set `Atlas.Auto.Tests/TestHelpers/SourceData/autotest-HF-set.json` must already be loaded into the target Atlas instance, and the Atlas HLA metadata dictionary must hold that set's nomenclature version.

## Commands

```
dotnet build
dotnet test
dotnet test --filter TestCategory=Search_HappyPathTests   # run one category (see [Category] on each fixture)
dotnet test --filter FullyQualifiedName~Search_HappyPathTests.Search_Cord_4_8_ReturnsExpectedSearchResult   # single test
```

Configuration (`Atlas.Auto.Tests/appsettings.json`) holds per-API `BaseUrl`/`ApiKey` sections (`DonorImport`, `MatchingAlgorithm`, `PublicApi`, `RepeatSearch`, `TopLevel`), all defaulted to `"override-this"`. Override locally with .NET user secrets from the `Atlas.Auto.Tests` project directory:
```
dotnet user-secrets set "DonorImport.ApiKey" "..."
```
In the pipeline, the equivalent pipeline variables use `.` for nesting (e.g. `DonorImport.ApiKey`) and are injected via `FileTransform@2` into `appsettings.json`. The NuGet feed for `Atlas.Debug.Client`/`Atlas.Debug.Client.Models` is set via `ATLAS_AZURE_ARTIFACTS_FEED_NAME_OR_ID`.

## Architecture

Two projects:
- **`Atlas.Auto.Utils`** — standalone reporting library (ExtentReports wrapper: `ExtentManager`, `ExtentService`). No dependency on the test project.
- **`Atlas.Auto.Tests`** — the NUnit test suite. Depends on `Atlas.Debug.Client` (talks to Atlas's HTTP APIs, including debug-only endpoints for polling internal state) and `Atlas.Client.Models`/domain types from the Atlas repo.

### Layered test structure

Each functional area (Search, RepeatSearch, Scoring, DonorImport) follows the same layered pattern, wired through `Microsoft.Extensions.DependencyInjection`:

1. **Services** (`TestHelpers/Services/**`) — thin wrappers around one `Atlas.Debug.Client` call each (e.g. `SearchRequester`, `MatchingNotificationFetcher`, `DonorCodeFetcher`). One interface + implementation per HTTP interaction.
2. **Workflows** (`TestHelpers/Workflows/*.cs`) — compose several Services into one logical operation for a feature area (e.g. `ISearchWorkflow.SubmitSearchRequest` → `FetchMatchingResultsNotification` → `FetchMatchingResultSet`). Workflows are the boundary that test steps talk to; they never call Services directly across areas.
3. **TestSteps** (`TestHelpers/TestSteps/*.cs`) — Gherkin-style, human-readable step methods (e.g. `CreateDonor`, `SubmitSearchRequest`, `MatchingShouldReturnExpectedDonor`) that combine one or more Workflow calls plus assertions. This is what test methods actually call. `DonorImportStepsForSearchTests` composes donor-import steps as a helper reused by search/scoring/repeat-search tests that need a donor set up first.
4. **Tests** (`Tests/<Area>/*.cs`) — NUnit fixtures. Each area has a `*TestBase` (e.g. `SearchTestBase` extends `TestBase`) that resolves the wired-up `ITestSteps` for that area via `Provider.ResolveServiceOrThrow<T>()`, plus `*_HappyPathTests` / `*_ExceptionPathTests` fixtures containing the actual `[Test]`/`[TestCaseSource]` methods.

Wiring: `ServiceConfiguration.CreateProvider()` (`DependencyInjection/ServiceConfiguration.cs`) builds the DI container per test-base instance — loads `appsettings.json` + user secrets, registers debug-client settings as Options, and registers every Service/Workflow/TestSteps interface. `TestBase` constructs this provider and an `ExtentTest` fixture logger in its constructor.

### Search-family tests (Search, RepeatSearch, Scoring)

- Request bodies live as JSON files under `TestHelpers/SourceData/` (embedded resources), keeping test methods free of inline HLA data.
- Snapshot testing via `Verify.NUnit` checks that Atlas returns the expected search/matching result. Snapshots live in `TestHelpers/Assertions/Approvals/*.verified.txt` and must be updated (and reviewed) when expected results legitimately change. Fields expected to vary per run (request IDs, generated donor IDs) are excluded from the comparison — see `TestHelpers/Extensions/VerifySettingsExtensions.cs`.
- Search-type tests take a `bool? parallelMatchPrediction` (`SearchTestBase.Cases()` — null/true/false) to exercise the default, new, and old match-prediction flows.
- Flaky/transient failures against a live remote system are retried once via `SearchTestBase.ExecuteWithRetry` (Polly), not by re-running the whole test.

### Parallelism and CI categories

- Add `[Parallelizable]`/`ParallelScope.All` to new fixtures — tests must be written so they can run in parallel locally without interfering (each test typically imports its own donor).
- Every fixture carries an NUnit `[Category]` matching its class name. In Azure DevOps (`test-pipeline.yml` + `azure-test-run-template.yml`), each Category runs as its own parallel pipeline job via `dotnet test --filter TestCategory=<name>`, but tests *within* a category run sequentially in CI. Long-running categories (>20 min, e.g. `RepeatSearch_HappyPathTests`) should be split into their own Category to avoid job timeouts.
- When adding a new test Category, also add a corresponding job entry to `testCategoryJobs` in `test-pipeline.yml`.

## Versioning against Atlas

This test project tracks the Atlas API version via its `Atlas.Debug.Client`/`Atlas.Debug.Client.Models` package references (`Atlas.Auto.Tests.csproj`), not its own version number. When a new Atlas version ships:
1. Read the Atlas and Debug Client changelogs for breaking changes.
2. Branch as `release/x.y.z` (matching the Atlas version under test), bump the package references to that stable version, run health-check tests locally.
3. Push, run the `release/x.y.z` pipeline against the matching Atlas instance (usually UAT); if green, merge to `main`.
4. The `release/x.y.z` branch is kept (not deleted, not tagged) so a `hotfix/x.y.z` branch can be cut from it later for a fix targeting that specific released version — mirroring the Atlas repo's `release`/`hotfix` branching model.

Existing branches follow this pattern, e.g. `release/3.4.1`, `release/4.0.0`, `hotfix/3.0.1`. Older `atlas/x.y.z` git tags (up to `atlas/2.5.0`) predate this scheme and are no longer created.
