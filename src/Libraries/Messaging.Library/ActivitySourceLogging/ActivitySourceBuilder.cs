using System.Diagnostics;

namespace Messaging.Library.ActivitySourceLogging;

public class ActivitySourceBuilder
{
    private readonly IActivitySourceFactory factory;
    private string serviceName = "Not Initialized";
    private ActivityKind actKind = ActivityKind.Internal;
    private readonly string actDescription;
    private string? activityDescriptionOverwrite = null;
    private readonly IDictionary<string, string> activityTags = new Dictionary<string, string>();

    internal ActivitySourceBuilder(IActivitySourceFactory factory, string activityDescription)
    {
        this.factory = factory;
        this.actDescription = activityDescription;
    }


    public ActivitySourceBuilder WithServiceName(string srvName)
    {
        this.serviceName = srvName;
        return this;
    }

    public ActivitySourceBuilder WithActivityKind(ActivityKind activityKind)
    {
        this.actKind = activityKind;
        return this;
    }

    public ActivitySourceBuilder WithActivityDescription(string activityDescription)
    {
        this.activityDescriptionOverwrite = activityDescription;
        return this;
    }

    public ActivitySourceBuilder WithTag(string key, string tag)
    {
        this.activityTags.Add(key, tag);
        return this;
    }

    public ActivityScope Start()
    {
        var activity = factory.CreateActivity(serviceName, actKind, actDescription, activityDescriptionOverwrite, activityTags);
        return activity;
    }
}