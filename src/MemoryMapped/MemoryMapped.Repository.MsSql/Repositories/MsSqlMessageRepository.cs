using MemoryMapped.Forwarder.Configuration;
using MemoryMapped.Forwarder.Repositories;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Data.Common;

namespace MemoryMapped.Repository.MsSql.Repositories;

public sealed class MsSqlMessageRepository(IOptions<DatabaseConnectionOptions> options, ILogger<MsSqlMessageRepository> logger)
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
IF OBJECT_ID('dbo.message', 'U') IS NULL
BEGIN
    CREATE TABLE message (    
        id uniqueidentifier PRIMARY KEY, 
        [timestamp] datetimeoffset(7) NOT NULL,
        correlationId uniqueidentifier NOT NULL,
        typeFullName nvarchar(255) NOT NULL,    
        message nvarchar(max) NOT NULL,    
    );  
  
END;
";
    }

    protected override DbConnection GetConnection()
    {
        return new SqlConnection(connectionString);
    }

}