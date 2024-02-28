using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal interface IHealthChecker<T> where T : ICommonAtlasFunctions
{
    // ReSharper disable once UnusedMember.Global - it is used in the HealthCheckTests
    Task<bool> HealthCheck();
}

internal class HealthChecker<T> : IHealthChecker<T> where T : ICommonAtlasFunctions
{
    private readonly IDebugRequester debugRequester;
    private readonly T client;

    public HealthChecker(IDebugRequester debugRequester, T client)
    {
        this.debugRequester = debugRequester;
        this.client = client;
    }

    public async Task<bool> HealthCheck()
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(3, 20, RunHealthCheck);
    }

    private async Task<bool> RunHealthCheck()
    {
        var result = await client.HealthCheck();
        return result?.Contains("successful") ?? false;
    }
}