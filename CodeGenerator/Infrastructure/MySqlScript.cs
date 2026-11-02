using System;
using System.Collections.Generic;
using System.Text;
using MySqlConnector;

namespace NoCodeAppGenerator.Infrastructure;

public delegate void MySqlScriptErrorEventHandler(object sender, MySqlScriptErrorEventArgs args);

public sealed class MySqlScriptErrorEventArgs(Exception exception, string statement) : EventArgs
{
    public Exception Exception { get; } = exception;
    public string Statement { get; } = statement;
    /// <summary>Set to true in a handler to keep executing the remaining statements.</summary>
    public bool Ignore { get; set; }
}

/// <summary>
/// Minimal replacement for MySql.Data's MySqlScript (MySqlConnector has no equivalent).
/// Splits a script into statements, honouring quotes, comments and DELIMITER directives,
/// then executes them one by one.
/// </summary>
public sealed class MySqlScript(MySqlConnection connection, string query)
{
    public string Delimiter { get; set; } = ";";
    public event MySqlScriptErrorEventHandler? Error;
    public event EventHandler? ScriptCompleted;

    public int Execute()
    {
        var count = 0;
        foreach (var statement in Split(query, Delimiter))
        {
            try
            {
                using var cmd = new MySqlCommand(statement, connection);
                cmd.ExecuteNonQuery();
                count++;
            }
            catch (Exception ex)
            {
                var args = new MySqlScriptErrorEventArgs(ex, statement) { Ignore = true };
                if (Error is null) throw;
                Error.Invoke(this, args);
                if (!args.Ignore) throw;
            }
        }
        ScriptCompleted?.Invoke(this, EventArgs.Empty);
        return count;
    }

    public static IReadOnlyList<string> Split(string sql, string delimiter = ";")
    {
        var statements = new List<string>();
        var current = new StringBuilder();
        var i = 0;
        var atLineStart = true;

        void Flush()
        {
            var s = current.ToString().Trim();
            if (s.Length > 0) statements.Add(s);
            current.Clear();
        }

        while (i < sql.Length)
        {
            // DELIMITER directive (client-side only, never sent to the server)
            if (atLineStart && current.ToString().Trim().Length == 0 &&
                string.Compare(sql, i, "DELIMITER", 0, 9, StringComparison.OrdinalIgnoreCase) == 0 &&
                i + 9 < sql.Length && char.IsWhiteSpace(sql[i + 9]))
            {
                var eol = sql.IndexOf('\n', i);
                if (eol < 0) eol = sql.Length;
                var d = sql.Substring(i + 9, eol - i - 9).Trim();
                if (d.Length > 0) delimiter = d;
                current.Clear();
                i = eol + 1;
                atLineStart = true;
                continue;
            }

            var c = sql[i];

            // comments
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-' && (i + 2 >= sql.Length || char.IsWhiteSpace(sql[i + 2])) || c == '#')
            {
                var eol = sql.IndexOf('\n', i);
                i = eol < 0 ? sql.Length : eol + 1;
                current.Append('\n');
                atLineStart = true;
                continue;
            }
            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*' && !(i + 2 < sql.Length && sql[i + 2] == '!'))
            {
                var end = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? sql.Length : end + 2;
                current.Append(' ');
                continue;
            }

            // quoted strings / identifiers
            if (c is '\'' or '"' or '`')
            {
                var start = i++;
                while (i < sql.Length)
                {
                    if (sql[i] == '\\' && c != '`') { i += 2; continue; }
                    if (sql[i] == c)
                    {
                        if (i + 1 < sql.Length && sql[i + 1] == c) { i += 2; continue; }
                        i++;
                        break;
                    }
                    i++;
                }
                current.Append(sql, start, Math.Min(i, sql.Length) - start);
                atLineStart = false;
                continue;
            }

            if (string.CompareOrdinal(sql, i, delimiter, 0, delimiter.Length) == 0)
            {
                Flush();
                i += delimiter.Length;
                continue;
            }

            current.Append(c);
            if (c == '\n') atLineStart = true;
            else if (!char.IsWhiteSpace(c)) atLineStart = false;
            i++;
        }
        Flush();
        return statements;
    }
}
