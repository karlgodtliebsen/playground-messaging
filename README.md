# playground-messaging

## Purpose
- Explore using WolverineFx as Messaging bus for Kafka, RabbitMq, Azure Service Bus, PostgreSql etc

- It also give an opportunity to use both MessagePack and MemoryPack
- And to se how far the Kafka messaging can bring us in that world
- Combined with Aspire, Docker, Docker Compose project, Docker Swarm and K3s



## Status

### Kafka
- Kafka (no Zookeeper dependency)
- WolverineFx publishing and consuming messages using RabbitMq and Kafka
- Redpanda Console for easy management at http://localhost:8080
- Seq for logging at http://localhost:5341

This gives a great foundation for exploring more advanced messaging patterns with WolverineFx and Kafka. 
Experiment with:

- Different message types and handlers
- Batch processing
- Error handling and retry policies
- Saga patterns
- Event sourcing


### RabbitMq

- A solid setup using WolverineFx and RabbitMQ
- An extended setup building included: 
  - a legacy type mapper that supports holding typeinfo in headers
  - a mapping from type to queues to enable smoother setup building
  - a separation of Consumer and Producer setup building
  - Extension of the Message base type to include relevant data for header population
  - A Monitoring Service as a .Net hosted Background Service
  - Multiple Hosts for Consumer and Producer and setup, using the Console App
  - An resilient EventHub based on .Net Channels, to abstract away the WolverineFx MessageBus
  - Tests of EventHub and some RabbitMq settings


### Docker commands to run RabbitMq with Management UI
- Messaging.RabbitMq.WebApi/README.md


### Docker commands to run Kafka using docker-compose.yml
- Messaging.Kafka.WebApi/README.md

### Aspire project to run debugging from inside Visual Studio

### Docker Compose project to run debugging from inside Visual Studio

 
### Swarm
A complete setup for Kafka Console Apps Producer/Consumer
Use WSL or Linux
Note: some paths needs adjusting



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
