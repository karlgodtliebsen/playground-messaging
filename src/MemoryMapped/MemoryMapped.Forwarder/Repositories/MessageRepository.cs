using Dapper;

using Messaging.Domain.Library.DemoMessages;

using Serilog;
using Serilog.Debugging;

using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MemoryMapped.Forwarder.Repositories;

public abstract class MessageRepository(ILogger logger) : IMessageRepository
{
    protected abstract string GetConnectionString();

    protected abstract DbConnection GetConnection();
    protected abstract string GetCreateTableStatement();

    private void PrintInformation(string message)
    {
        SelfLog.WriteLine(message);
        Debug.Print(message);
        Trace.WriteLine(message);
        Console.WriteLine(message);
    }
    private void PrintError(Exception ex, string action)
    {
        SelfLog.WriteLine($"Failed {action}\n Exception: {ex}\n In Database: {GetConnectionString()}");
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
            PrintInformation($"Successfully Created text_message Table");
            logger.Information("Successfully Created text_message Table");
        }
        catch (Exception ex)
        {
            PrintError(ex, "Error Creating text_message Table");
            logger.Error(ex, "Error Creating logtext_message_event Table");
        }
    }

    public async IAsyncEnumerable<TextMessage> Find(object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sqlStatement = "SELECT * FROM text_messages";
        await using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken);
        IEnumerable<TextMessage> reader = await connection.QueryAsync<TextMessage>(sqlStatement, parameters);
        foreach (var message in reader)
        {
            yield return message;
        }
    }


    public async Task Add(IEnumerable<TextMessage> entries, CancellationToken cancellationToken)
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
                    //timestamp = entity.Timestamp.UtcDateTime,
                    //level = entity.Level.ToString(),
                    //exception = entity.Exception,
                    //rendered_message = entity.RenderedMessage,
                    //message_template = entity.MessageTemplate,
                    //trace_id = entity.TraceId,
                    //span_id = entity.SpanId,
                    //properties = entity.Properties,

                }).ToArray();

                var sqlStatement =
                    @" INSERT INTO text_message (timestamp, level, exception, rendered_message, message_template,trace_id, span_id,properties) 
                            VALUES (@timestamp, @level, @exception, @rendered_message, @message_template,@trace_id, @span_id, @properties);";

                var rowsAffected = await connection.ExecuteAsync(sqlStatement, parameters, transaction);

                await transaction.CommitAsync(cancellationToken);
                PrintInformation($"Successfully inserted {rowsAffected} entries into text_message table");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                PrintError(ex, "Unexpected error while forwarding text_message entries");
                logger.Error(ex, "Failed to insert {Count} entries, transaction rolled back", entriesList.Count);
                throw;
            }
        }
        catch (Exception ex)
        {
            PrintError(ex, "Unexpected error while forwarding text_message entries");
            logger.Error(ex, "Unexpected error while forwarding {Count} text_message entries", entriesList.Count);
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
            logger.Error(ex, "Connection test failed");
            return false;
        }
    }
}