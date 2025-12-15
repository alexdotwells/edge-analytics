namespace EdgeAnalytics.Domain.Common;

public sealed class PipelineFailureException : Exception
{
    public PipelineFailureType FailureType { get; }

    public PipelineFailureException(
        PipelineFailureType failureType,
        string message,
        Exception? inner = null)
        : base(message, inner)
    {
        FailureType = failureType;
    }
}
