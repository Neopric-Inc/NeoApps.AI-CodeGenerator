using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Built-in tables whose generated class names are referenced by the static template code
    /// (e.g. PermissionMatrixModel). With lower_case_table_names=1 (Windows, or the bundled Docker MySQL)
    /// the server reports them lower-cased, so restore the canonical casing; SQL still matches either way.
    /// </summary>
    private static readonly Dictionary<string, string> CanonicalNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["users"] = "Users",
        ["roles"] = "Roles",
        ["entities"] = "Entities",
        ["permissionmatrix"] = "PermissionMatrix",
    };

    public static DataTable GetTables(MySqlConnection connection)
    {
        var dt = Query(connection,
            // ai_* tables belong to the generated API's AI assistant (created at runtime) and VersionInfo to the
            // migration runner (FluentMigrator); they get no generated CRUD.
            "SELECT * FROM information_schema.tables WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE' AND LOWER(table_name) NOT IN ('ai_files','ai_chunks','ai_conversations','ai_messages','ai_runs','user_tenants','login_attempts','user_security','number_sequences') AND LOWER(table_name) <> 'versioninfo' ORDER BY table_name",
            null);
        if (IsCaseInsensitive(connection))
        {
            foreach (DataRow row in dt.Rows)
                if (CanonicalNames.TryGetValue(row["TABLE_NAME"].ToString()!, out var canonical))
                    row["TABLE_NAME"] = canonical;
        }
        return dt;
    }

    private static bool IsCaseInsensitive(MySqlConnection connection)
    {
        using var cmd = new MySqlCommand("SELECT @@lower_case_table_names", connection);
        return Convert.ToInt32(cmd.ExecuteScalar()) != 0;
    }

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
