# playground-messaging

## Purpose
- Explore using WolverineFx as Messaging abstraction for Kafka, RabbitMq, Azure Service Bus, PostgreSql etc

- It also give an opportunity to use both MessagePack and MemoryPack
- And to se how far the Kafka messaging can bring us in that world


### Docker commands to run RabbitMq with Management UI

```shell
docker pull rabbitmq
docker pull rabbitmq:3-management

docker run -d --hostname RabbitMq --name rabbitmq-5673 -p 5673:5672 -p 15673:15672 rabbitmq:3-management


-e RABBITMQ_DEFAULT_USER=guest -e RABBITMQ_DEFAULT_PASS=guest 

docker start rabbitmq-5673

add this if relevant:

-v /temp/rabbitmq/rabbitmq-data:/var/lib/rabbitmq 

After it’s up, you can point your browser to http://localhost:15673 

docker stop rabbitmq-5673
docker stop rabbitmq:3-management

(login is guest/guest by default)
```


### Look into:
//The Wolverine code generation mode is Dynamic. This is suitable for development, but you may want to opt into other options for production usage to reduce start up time and resource utilization.
// https://wolverine.netlify.app/guide/codegen.html

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
