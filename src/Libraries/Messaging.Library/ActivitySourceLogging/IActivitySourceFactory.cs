using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Messaging.Library.ActivitySourceLogging;

public interface IActivitySourceFactory
{
    /// <summary>
    ///  Usage pattern:
    /// <example>
    /// await using var _ = activitySourceFactory.StartActivity(ServiceName, activityDescriptionOverwrite: " name to overwrite [CallerMemberName] method");
    /// </example>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="activityKind"></param>
    /// <param name="activityDescription"></param>
    /// <param name="activityDescriptionOverwrite"></param>
    /// <param name="activityTags"></param>
    /// <returns></returns>
    ActivityScope CreateActivity(string name, ActivityKind activityKind = ActivityKind.Internal, string activityDescription = null!,
        string? activityDescriptionOverwrite = null, IDictionary<string, string>? activityTags = null);

    /// <summary>
    /// Usage pattern:
    /// <example>
    /// await using var _ = activitySourceFactory
    /// .CreateActivity()
    /// .WithServiceName(ServiceName).WithActivityKind(ActivityKind.Internal)
    /// .WithActivityDescription("name to overwrite [CallerMemberName] method")
    /// .Start();
    /// </example>
    /// </summary>
    /// <param name="activityDescription"></param>
    /// <returns></returns>
    ActivitySourceBuilder CreateBuilder(string activityDescription = null!);

    Meter Meter { get; }
}