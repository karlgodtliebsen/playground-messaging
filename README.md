# playground-messaging

## Purpose
- Explore using WolverineFx as Messaging bus for Kafka, RabbitMq, Azure Service Bus, PostgreSql etc

- It also give an opportunity to use both MessagePack and MemoryPack
- And to se how far the Kafka messaging can bring us in that world


## Status

### Kafka
You now have a solid setup with (Claude):

Kafka with KRaft (no Zookeeper dependency)
WolverineFx publishing and consuming messages
Auto-topic creation working properly
Redpanda Console for easy management at http://localhost:8080

This gives you a great foundation for exploring more advanced messaging patterns with WolverineFx and Kafka. You can now experiment with:

- Different message types and handlers
- Batch processing
- Error handling and retry policies
- Saga patterns
- Event sourcing

### RabbitMq

- A solid setup using RabbitMQ



### Docker commands to run RabbitMq with Management UI
- Messaging.RabbitMq.WebApi/README.md


### Docker commands to run Kafka using docker-compose.yml
- Messaging.Kafka.WebApi/README.md
 

### Info:
- The Wolverine code generation mode is Dynamic. This is suitable for development, but you may want to opt into other options for production usage to reduce start up time and resource utilization.
- https://wolverine.netlify.app/guide/codegen.html

```csharp

    // The default behavior. Dynamically generate the
    // types on the first usage
    opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic;

    // Never generate types at runtime, but instead try to locate
    // the generated types from the main application assembly
    opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;

    // Hybrid approach that first tries to locate the types
    // from the application assembly, but falls back to
    // generating the code and dynamic type. Also writes the
    // generated source code file to disk
    opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Auto;

```
