using System.Data;
using Microsoft.Data.SqlClient;

namespace PensionPortal.Web.Services;

/// <summary>
/// Shared database service — wraps SqlConnection and provides
/// parameterised sproc execution. Equivalent of conn.asp in the
/// Classic ASP architecture.
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PensionPortal")
            ?? throw new InvalidOperationException("Connection string 'PensionPortal' not configured.");
    }

    /// <summary>
    /// Executes a stored procedure and returns results as a DataTable.
    /// All user-supplied values must be passed as SqlParameter instances.
    /// </summary>
    public DataTable ExecuteSproc(string sprocName, params SqlParameter[] parameters)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sprocName, conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddRange(parameters);
        conn.Open();

        var table = new DataTable();
        using var reader = cmd.ExecuteReader();
        table.Load(reader);
        return table;
    }

    /// <summary>
    /// Executes a stored procedure that does not return results (INSERT/UPDATE/DELETE).
    /// </summary>
    public void ExecuteNonQuery(string sprocName, params SqlParameter[] parameters)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sprocName, conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddRange(parameters);
        conn.Open();
        cmd.ExecuteNonQuery();
    }
}
