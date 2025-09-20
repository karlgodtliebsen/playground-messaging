using Messaging.Library;
using Messaging.RabbitMq.Library;

using Microsoft.Extensions.Logging;

namespace Messaging.Console.App.Configuration;

public class SignalListener(ISignalChannel channel, ILogger<SignalListener> logger)
{
    public void SetupSubscriptions()
    {
        logger.LogInformation("SignalListener Started");

        channel.Subscribe("Alive", (ct) =>
        {
            logger.LogInformation("Channel Received Alive Signal");
            return Task.CompletedTask;
        });

        channel.Subscribe<TextMessage>("TextMessage", (m, ct) =>
        {
            logger.LogInformation("Channel using 'TextMessage' Received TextMessage: {@message}", m);
            return Task.CompletedTask;
        });

        channel.Subscribe<TextMessage>((m, ct) =>
        {
            logger.LogInformation("Channel Received TextMessage: {@message}", m);
            return Task.CompletedTask;
        });

        channel.Subscribe<PingMessage>((m, ct) =>
        {
            logger.LogInformation("Channel Received PingMessage: {@message}", m);
            return Task.CompletedTask;
        });
        channel.Subscribe<HeartbeatMessage>((m, ct) =>
        {
            logger.LogInformation("Channel Received HeartbeatMessage: {@message}", m);
            return Task.CompletedTask;
        });
    }



}