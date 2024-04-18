# Atlas.Auto.Tests
Automated end-to-end test framework for the Atlas search algorithm - see the [main GitHub repository](http://github.com/Anthony-Nolan/Atlas?tab=readme-ov-file#atlas---donor-search-algorithm-as-a-service) for more information.

## Prerequisites
- The target Atlas instance under test must be at least version 2.1.0, as this is the first version to support the debug endpoints referenced in `Atlas.Debug.Client`.
- The test haplotype frequency set, [autotest-HF-set.json](/Atlas.Auto.Tests/TestHelpers/SourceData/autotest-HF-set.json), must be loaded into the target Atlas instance prior to running search or repeat search tests.
  - For successful upload (and later, use during match prediction), the Atlas HLA metadata dictionary must hold the nomenclature version of the HF set.
  - [See Atlas documentation](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Integration.md#haplotype-frequency-sets) for more information.

## Local

### Running Tests
- Default settings within `appsettings.json` can be overridden using .NET CLI User Secrets.
  - Either navigate to the root of the `Atlas.Auto.Tests` project and run the following command via the terminal:  
```dotnet user-secrets set "NameOfSecret" "ValueOfSecret"```
  - Or, in the Visual Studio IDE, right-click the project name, and select the context menu option: "Manage User Secrets".
  - All settings that must be overridden have the placeholder value of `"override-this"`.
- Tests can be run in parallel to reduce overall execution time.

### Local Development
- Set the Atlas artifacts feed.
- In Visual Studio 2022, this can be done via `Tools > NuGet Package Manager > Package Sources`.

## DevOps
- `test-pipeline.yml` is a template file for tests to be run in Azure DevOps.
- A new pipeline should be created for each instance of the Atlas API under test, e.g., Dev, UAT, PR, etc.
- The template file does not have any triggers or schedules: this should be set as needed for each copy of the pipeline.
- Make sure to extend the list of `testCategoryJobs` whenever a new Category of tests is added.

### Pipeline Variables
- Each pipeline instance must have pipeline variables that match those within `appsettings.json`.
  - Use `.` for nested settings, e.g., var name `DonorImport.ApiKey` would be used for setting:
	```json
	{
		"DonorImport": { 
			"ApiKey": "value" 
		}
	}
	```
- The name/id of the Atlas artifacts feed must be set using the variable, `ATLAS_AZURE_ARTIFACTS_FEED_NAME_OR_ID`.
  - String should be format of either `projectName/feedName` e.g., `Atlas/atlas-packages` or just `feedName`, as appropriate.

## Contributing
Please refer to the [contribution guidelines on the main Atlas repository](https://github.com/Anthony-Nolan/Atlas/blob/master/README_Contribution_Versioning.md).

## Versioning
The E2E test project does not have its own version at present.

## Dependencies
- `Atlas.Debug.Client` and `Atlas.Debug.Client.Models` are used to interact with the Atlas API.

## Writing Tests

### Parallelisation
- Tests should be written in a way that they can be run in parallel without interference.
- Add the `[Parallelizable]` attribute to new test classes/ methods to allow tests to run in parallel locally.
- Each test `Category` is executed parallel on the DevOps build pipeline, however the tests within a category are run sequentially.
  - Long running tests (over 20 mins each) should therefore be placed in their own `[Category]` to prevent timeouts. See `RepeatSearch_HappyPathTests` for an example.

### Search-related Tests
- To simplify the process of building and initiating searches, test search requests have been saved as json files to `Atlas.Auto.Tests\TestHelpers\SourceData\`.
- Snapshot testing, via the `Verify.NUnit` package, is used to assert that the expected search or repeat search result has been returned.
  - Approval files have been saved under `Atlas.Auto.Tests\TestHelpers\Assertions\Approvals\` and should be updated when the expected result changes.
  - Properties that are expected to differ between requests, such as search request ID and matching donor ID, are purposefully excluded from the snapshot comparison.