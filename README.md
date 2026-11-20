# Atlas.Auto.Tests
Automated end-to-end test framework for the Atlas search algorithm - see the [main GitHub repository](http://github.com/Anthony-Nolan/Atlas?tab=readme-ov-file#atlas---donor-search-algorithm-as-a-service) for more information.

## Prerequisites
- The test haplotype frequency set, [autotest-HF-set.json](/Atlas.Auto.Tests/TestHelpers/SourceData/autotest-HF-set.json), must be loaded into the target Atlas instance prior to running search or repeat search tests.
  - For successful upload (and later, use during match prediction), the Atlas HLA metadata dictionary must hold the nomenclature version of the HF set.
  - [See Atlas documentation](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Integration.md#haplotype-frequency-sets) for more information.

## Architecture
The test project connects directly to Atlas infrastructure rather than through debug endpoints:
- **SQL Server** — reads donor data from the Atlas donor store and matching algorithm databases.
- **Azure Service Bus** — listens for donor import results, alerts, and search/repeat search notifications.
- **Azure Blob Storage** — uploads donor import files and downloads search/scoring result sets.
- **Azure App Insights** — queries HLA expansion failures via Log Analytics workspace.
- **Azure ARM** — reads Function App configuration settings (e.g., `AllowFullModeImport`).
- **Public API** — calls production endpoints (Search, RepeatSearch, Score, ScoreBatch, HealthCheck) directly via HTTP.

## Local

### Running Tests
- Default settings within `appsettings.json` can be overridden using .NET CLI User Secrets.
  - Either navigate to the root of the `Atlas.Auto.Tests` project and run the following command via the terminal:  
```dotnet user-secrets set "NameOfSecret" "ValueOfSecret"```
  - Or, in the Visual Studio IDE, right-click the project name, and select the context menu option: "Manage User Secrets".
  - All settings that must be overridden have the placeholder value of `"override-this"`.
  - Use `:` for nested settings, e.g. `dotnet user-secrets set "KeyVault:Name" "an-dev-atlas-kv"`. Unlike the pipeline variables, a user secret named with `.` (`KeyVault.Name`) overrides nothing.
- The tests sign in to Azure as you: run `az login` first. If your default tenant is not the one Atlas is in, also set `Azure:TenantId`.
  Your account needs:
  - read access to the secrets in the environment's Atlas Key Vault (`KeyVault:Name`, e.g. `an-dev-atlas-kv`), e.g. through the "Atlas Key Vault Officers - DEV" group;
  - the rights on the Atlas resource group to read and change function app settings and keys, and to query its App Insights workspace.
- Your IP address must be allowed by the firewall of the environment's Atlas SQL server.
- The secrets are not settings: at start-up, the tests read the connection strings from the Key Vault and the Public API key from the Public API function app (see `TestHelpers/Settings/StartupSecrets.cs`). If one can't be read, the run stops before the first test, listing every secret that failed with the Azure error. While the Public API app is starting, e.g. right after a deployment, reading its key is retried (`Retry:FunctionAppRestart`).
  A user secret with the same name, e.g. `Database:Sql` or `PublicApi:ApiKey`, still takes precedence.
- To leave the function apps as they are, set `Azure:ChangeFunctionAppSettings` to `false`: the tests that need a changed setting are then skipped.
- Tests can be run in parallel to reduce overall execution time.

### Local Development
- Set the Atlas artifacts feed.
- In Visual Studio 2022, this can be done via `Tools > NuGet Package Manager > Package Sources`.

## DevOps
- `test-pipeline.yml` is a template file for tests to be run in Azure DevOps.
- A new pipeline should be created for each instance of the Atlas API under test, e.g., Dev, UAT, PR, etc.
- The template file does not have any triggers or schedules: this should be set as needed for each copy of the pipeline.
- `test-pipeline.yml` lists the test runs and their test categories; `azure-test-run-template.yml` holds the build and the test job, and `pipeline/` the scripts they run.
- After the build, one job on one agent runs three test runs, one after another; a test run starts only when the one before it passed:
  1. **Health Check** — verifies infrastructure accessibility (SQL, ServiceBus, Blob Storage, Azure Auth, App Insights, Public API).
  2. **Donor Import** — runs donor import tests.
  3. **Search, Scoring & Repeat Search** — runs all search-related tests.
- A test run runs all its categories at once. The results are published as one test run per category, tagged with the tag the category is listed under (HealthCheck, DonorImport, Search, Scoring, RepeatSearch).
- The 'CPU and memory' step of each test run prints the agent's CPU and memory peaks and averages while it ran.
- 'Rerun failed jobs' runs only the categories that did not pass.
- Every test run starts by removing the test donors earlier runs left behind, so two runs must not target the same Atlas at the same time.
- Make sure to add a new Category of tests under its tag in `categories` of the appropriate test run.

### Pipeline Variables
- Each pipeline's settings live in a variable group, not in the pipeline itself, and its tests sign in to Azure as a service connection. `environments` in `test-pipeline.yml` names both, by pipeline name:
  ```yaml
  environments:
    '[DEV-ATLAS] Atlas E2E Tests':
      variableGroup: 'DEV-ATLAS Atlas E2E Tests'
      serviceConnection: 'Atlas Dev WIF'
  ```
  - A new pipeline needs its entry, in each branch it runs (e.g. before a `release/x.y.z` branch is cut for it). Without one, the run fails at once with "Variable group  was not found or is not authorized for use", with the name blank: Azure DevOps doesn't name a group it can't find. Renaming a pipeline needs its entry renamed too.
  - Allow the pipeline to use both, in the group's and the service connection's "Pipeline permissions". Looking them up by the pipeline's own name means a run can only target its pipeline's environment.
  - Why the service connection is in the repo and not in the group: Azure DevOps only issues tokens for a service connection that a task of the stage names at compile time, and a variable group is read at run time. The 'Azure identity of the tests' step (AzureCLI@2) names it and passes its IDs to the test steps, where `AzurePipelinesCredential` asks for tokens (`AzureCredentialFactory.cs`). On an agent (`TF_BUILD` set), a test step without those variables (its `env:`) fails at once, naming the missing ones.
- The group must have variables that match those within `appsettings.json`.
  - Use `.` for nested settings, e.g., var name `Azure.FunctionApps.Search` would be used for setting:
	```json
	{
		"Azure": {
			"FunctionApps": {
				"Search": "value"
			}
		}
	}
	```
- The pipeline holds no secrets, and `appsettings.json` has no secret settings, so none can end up in the published `dropTestSuite` artifact.
  The tests read the connection strings from the environment's Atlas Key Vault and the Public API key through ARM, as the service connection.
- Variables (none of them secret):
  - `Azure.SubscriptionId`, `Azure.ResourceGroupName`
  - `Azure.FunctionApps.DonorImport`, `Azure.FunctionApps.Search`, `Azure.FunctionApps.RepeatSearch`, `Azure.FunctionApps.MatchingDonorManagement`, `Azure.FunctionApps.SearchTracking`, `Azure.FunctionApps.PublicApi`
  - `KeyVault.Name`, the environment's Atlas Key Vault, e.g. `an-dev-atlas-kv`
  - `AppInsights.WorkspaceId`
  - `PublicApi.BaseUrl`
- The service connection is a workload identity federation (WIF) one, e.g. "Atlas Dev WIF". Its service principal needs Key Vault Secrets User on the Key Vault, and the rights to read and change function app settings and keys and to query the App Insights workspace.
- The name/id of the Atlas artifacts feed must be set using the variable, `ATLAS_AZURE_ARTIFACTS_FEED_NAME_OR_ID`.
  - String should be format of either `projectName/feedName` e.g., `Atlas/atlas-packages` or just `feedName`, as appropriate.

## Versioning & Dependencies
The E2E test project does not have its own version at present.
It depends on `Atlas.Client.Models` and `Atlas.DonorImport.FileSchema.Models` packages for shared DTOs, which are versioned in line with the Atlas API (see [Atlas README](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Contribution_Versioning.md)).
The E2E tests should be updated to use the latest version of these packages whenever a new Atlas version is released.

This should be done by:
1. First reading the [Atlas changelog](https://github.com/Anthony-Nolan/Atlas/blob/master/Atlas.Functions.PublicApi/CHANGELOG_Atlas.md) to check for API-level and/or functional breaking changes.
2. Creating a new branch of the E2E tests repo named after the version of Atlas being tested, i.e., `release/x.y.z`, where "x.y.z" is the Atlas version number under test. Branching follows the same `release/x.y.z` (and, for patches to an already-released version, `hotfix/x.y.z`) model as the [main Atlas repository](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Contribution_Versioning.md).
3. Updating the package references in the `Atlas.Auto.Tests.csproj` file to version `x.y.z` (stable, not pre-release).
4. Running the health check tests locally as a build check.
5. Push the new branch to the remote repository.
6. On DevOps, run the `release/x.y.z` version of the tests pipeline against the Atlas instance of the same version - most likely, this will be Atlas UAT.
7. If all tests are green, merge the branch into `main`.
8. The `release/x.y.z` branch is not deleted or tagged - it is kept in the remote so a `hotfix/x.y.z` branch can be cut from it later if that specific released version needs a test fix.

## Contributing
Please refer to the [contribution guidelines on the main Atlas repository](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Contribution_Versioning.md).

### Writing Tests

#### Parallelisation
- Tests should be written in a way that they can be run in parallel without interference.
- Add the `[Parallelizable]` attribute to new test classes/ methods to allow tests to run in parallel locally.
- Each test `Category` is executed parallel on the DevOps build pipeline, however the tests within a category are run sequentially.
  - Long running tests (over 20 mins each) should therefore be placed in their own `[Category]` to prevent timeouts. See `RepeatSearch_HappyPathTests` for an example.

#### Search-related Tests
- To simplify the process of building and initiating searches, test search requests have been saved as json files to `Atlas.Auto.Tests\TestHelpers\SourceData\`.
- Snapshot testing, via the `Verify.NUnit` package, is used to assert that the expected search or repeat search result has been returned.
  - Approval files have been saved under `Atlas.Auto.Tests\TestHelpers\Assertions\Approvals\` and should be updated when the expected result changes.
  - Properties that are expected to differ between requests, such as search request ID and matching donor ID, are purposefully excluded from the snapshot comparison.
