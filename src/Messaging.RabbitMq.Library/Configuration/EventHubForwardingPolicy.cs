using Wolverine;
using Wolverine.Configuration;
using Wolverine.Runtime;

namespace Messaging.RabbitMq.Library.Configuration;

public class EventHubForwardingPolicy : IWolverinePolicy
{
    public void Apply(WolverineOptions options, IServiceContainer container)
    {
        // Apply to all message types that implement IMessage (or whatever your base interface is)
        options.Policies.ForMessagesOfType<Messaging.Library.IMessage>().AddMiddleware(typeof(GenericMessageForwarder<>));

        // Or apply to ALL message types
        options.Discovery.IncludeType(typeof(GenericMessageForwarder<>));
    }
}