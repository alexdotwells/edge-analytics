namespace EdgeAnalytics.Domain.Common;

public enum PipelineFailureType
{
    Transient,   // retryable (timeouts, 429s, temp IO)
    Permanent,   // data/schema issues
    Fatal        // logic bugs, invariants violated
}
