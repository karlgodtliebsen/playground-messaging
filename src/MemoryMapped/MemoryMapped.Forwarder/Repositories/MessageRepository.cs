using Dapper;

using Messaging.Library;

using Microsoft.Extensions.Logging;

using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MemoryMapped.Forwarder.Repositories;

public abstract class MessageRepository(Microsoft.Extensions.Logging.ILogger logger) : IMessageRepository
{
    protected abstract string GetConnectionString();

    protected abstract DbConnection GetConnection();
    protected abstract string GetCreateTableStatement();

    private void PrintInformation(string message)
    {
        //SelfLog.WriteLine(message);
        Debug.Print(message);
        Trace.WriteLine(message);
        Console.WriteLine(message);
    }
    private void PrintError(Exception ex, string action)
    {
        //SelfLog.WriteLine($"Failed {action}\n Exception: {ex}\n In Database: {GetConnectionString()}");
        Debug.Print($"Failed {action}\n Exception: {ex}\n In Database: {GetConnectionString()}");
        Trace.WriteLine($"Failed {action}\n Exception: {ex}\n In Database: {GetConnectionString()}");
        Console.WriteLine($"Failed {action}\n Exception: {ex}\n In Database: {GetConnectionString()}");
    }

    public async Task CreateTable(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync(cancellationToken);
            await connection.ExecuteAsync(GetCreateTableStatement());
            PrintInformation($"Successfully Created message Table");
            logger.LogInformation("Successfully Created message Table");
        }
        catch (Exception ex)
        {
            PrintError(ex, "Error Creating message Table");
            logger.LogError(ex, "Error Creating message Table");
        }
    }

    public async IAsyncEnumerable<IMessageBase> Find<T>(object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken) where T : IMessageBase
    {
        var sqlStatement = "SELECT * FROM messages";
        await using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);
        IEnumerable<PersistMessage> reader = await connection.QueryAsync<PersistMessage>(sqlStatement, parameters);
        foreach (var message in reader)
        {
            yield return System.Text.Json.JsonSerializer.Deserialize<T>(message.Message);
        }
    }


    public async Task Add(IEnumerable<IMessageBase> entries, CancellationToken cancellationToken)
    {
        var entriesList = entries.ToList();
        if (!entriesList.Any()) return;

        await using var connection = GetConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                var parameters = entriesList.Select(entity => new
                {
                    id = entity.Id,
                    timestamp = entity.TimeStamp,
                    correlationId = entity.CorrelationId,
                    typeFullName = entity.GetType().FullName!,
                    message = System.Text.Json.JsonSerializer.Serialize(entity),

                }).ToArray();

                var sqlStatement =
                    @"INSERT INTO message (id, timestamp, correlationId,typeFullName,message) 
                            VALUES (@id,@timestamp, @correlationId,@typeFullName, @message);";

                var rowsAffected = await connection.ExecuteAsync(sqlStatement, parameters, transaction);

                await transaction.CommitAsync(cancellationToken);
                PrintInformation($"Successfully inserted {rowsAffected} entries into message table");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                PrintError(ex, "Unexpected error while forwarding message entries");
                logger.LogError(ex, "Failed to insert {Count} entries, transaction rolled back", entriesList.Count);
                throw;
            }
        }
        catch (Exception ex)
        {
            PrintError(ex, "Unexpected error while forwarding text_message entries");
            logger.LogError(ex, "Unexpected error while forwarding {Count} text_message entries", entriesList.Count);
            throw;
        }
    }
    public async Task<bool> TestConnection(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = GetConnection();
            await connection.OpenAsync(cancellationToken);

            // Simple query to test connection
            var result = await connection.QuerySingleAsync<int>("SELECT 1");

            PrintInformation("Connection test successful");
            return result == 1;
        }
        catch (Exception ex)
        {
            PrintError(ex, "Connection test failed");
            logger.LogError(ex, "Connection test failed");
            return false;
        }
    }


}