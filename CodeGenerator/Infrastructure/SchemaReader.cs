using System.Data;
using MySqlConnector;

namespace NoCodeAppGenerator.Infrastructure;

/// <summary>
/// Replacement for MySqlConnection.GetSchema("Tables"/"Columns"). MySql.Data scoped those to the current
/// database; MySqlConnector returns every schema on the server (mysql.*, sys.*), which made the generator
/// emit controllers for MySQL's own system tables. These queries are always scoped to DATABASE().
/// </summary>
public static class SchemaReader
{
    public static DataTable GetTables(MySqlConnection connection) =>
        Query(connection,
            "SELECT * FROM information_schema.tables WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE' ORDER BY table_name",
            null);

    public static DataTable GetColumns(MySqlConnection connection, string table) =>
        Query(connection,
            "SELECT * FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @t ORDER BY ordinal_position",
            table);

    private static DataTable Query(MySqlConnection connection, string sql, string table)
    {
        using var cmd = new MySqlCommand(sql, connection);
        if (table != null) cmd.Parameters.AddWithValue("@t", table);
        using var reader = cmd.ExecuteReader();
        var dt = new DataTable();
        dt.Load(reader);
        return dt;
    }
}
