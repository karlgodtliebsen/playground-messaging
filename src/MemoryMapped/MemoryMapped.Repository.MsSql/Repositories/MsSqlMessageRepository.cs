using MemoryMapped.Forwarder.Configuration;
using MemoryMapped.Forwarder.Repositories;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

using Serilog;

using System.Data.Common;

namespace MemoryMapped.Repository.MsSql.Repositories;

public sealed class MsSqlMessageRepository(IOptions<DatabaseConnectionOptions> options, ILogger logger)
    : MessageRepository(logger)
{
    private readonly string connectionString = options.Value.ConnectionString;

    protected override string GetConnectionString()
    {
        return connectionString;
    }

    protected override string GetCreateTableStatement()
    {
        return @"
IF OBJECT_ID('dbo.text_message', 'U') IS NULL
BEGIN
    CREATE TABLE text_message (    
        id bigint IDENTITY(1,1) PRIMARY KEY, 
        [created_at] datetimeoffset(7) NOT NULL DEFAULT getdate(),
        [timestamp] datetimeoffset(7) NOT NULL,
        [level] nvarchar(25) NOT NULL,
        [exception] nvarchar(max) NULL,
        rendered_message nvarchar(max) NOT NULL,    
        message_template nvarchar(max) NOT NULL,    
        trace_id nvarchar(255) NULL,
        span_id nvarchar(255) NULL,
        properties nvarchar(max) NULL
    );  
  
END;
";
    }

    protected override DbConnection GetConnection()
    {
        return new SqlConnection(connectionString);
    }

}