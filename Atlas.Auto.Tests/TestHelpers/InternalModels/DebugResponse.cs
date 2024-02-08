namespace Atlas.Auto.Tests.TestHelpers.InternalModels
{
    /// <summary>
    /// Response to a debug request.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class DebugResponse<TResult>
    {
        /// <summary>
        /// Was the debug request successful?
        /// </summary>
        public bool WasSuccess { get; }

        /// <summary>
        /// Result of the debug request.
        /// </summary>
        public TResult? DebugResult { get; }

        /// <summary>
        /// Use to generate empty failed response.
        /// </summary>
        public DebugResponse() { }

        /// <summary>
        /// Use to set response and determine success by whether result is null (`false`) or not (`true`).
        /// </summary>
        public DebugResponse(TResult? debugResult)
        {
            WasSuccess = debugResult != null;
            DebugResult = debugResult;
        }

        public DebugResponse(bool wasSuccess, TResult? debugResult)
        {
            WasSuccess = wasSuccess;
            DebugResult = debugResult;
        }
    }
}
