using System;
using System.IO;
using System.Net.Http;

namespace NoCodeAppGenerator.Infrastructure;

/// <summary>
/// Copies a resource to a local file. Accepts a local path, a file:// URI or an http(s) URL,
/// so local runs no longer need the SQL script served from XAMPP/htdocs.
/// </summary>
public static class SourceFetcher
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(2) };

    public static void Fetch(string source, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var fullDest = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullDest)!);

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            using var response = Http.GetAsync(uri).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            using var fs = File.Create(fullDest);
            response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
            return;
        }

        var localPath = uri is { IsFile: true } ? uri.LocalPath : source;
        if (!File.Exists(localPath))
            throw new FileNotFoundException($"Source not found: {source}", localPath);

        if (!string.Equals(Path.GetFullPath(localPath), fullDest, StringComparison.Ordinal))
            File.Copy(localPath, fullDest, overwrite: true);
    }
}
