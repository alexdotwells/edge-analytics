using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EdgeAnalytics.Infrastructure.Load.File;

public sealed class DedupingNdjsonSink<T>
{
    private readonly DataRoot _dataRoot;
    private readonly ILogger<DedupingNdjsonSink<T>> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions IndexJsonOpts = new()
    {
        WriteIndented = true
    };

    public DedupingNdjsonSink(
        ILogger<DedupingNdjsonSink<T>> logger,
        DataRoot dataRoot)
    {
        _logger = logger;
        _dataRoot = dataRoot;
    }

    /// <summary>
    /// Appends NDJSON records only when the identity's fingerprint changed since last write.
    /// Also maintains an index file mapping identityKey -> fingerprint.
    /// Paths are relative to DataRoot (repo-root /data).
    /// </summary>
    public async Task<int> AppendChangesAsync(
        string ndjsonRelativePath,
        string indexRelativePath,
        IEnumerable<T> items,
        Func<T, string> identityKey,
        Func<T, string> changeFingerprint,
        CancellationToken ct,
        bool ensureFileExistsEvenIfNoChanges = false)
    {
        if (string.IsNullOrWhiteSpace(ndjsonRelativePath))
            throw new ArgumentException("ndjsonRelativePath is required.", nameof(ndjsonRelativePath));
        if (string.IsNullOrWhiteSpace(indexRelativePath))
            throw new ArgumentException("indexRelativePath is required.", nameof(indexRelativePath));
        if (items is null) throw new ArgumentNullException(nameof(items));
        if (identityKey is null) throw new ArgumentNullException(nameof(identityKey));
        if (changeFingerprint is null) throw new ArgumentNullException(nameof(changeFingerprint));

        // Resolve to absolute paths under repo-root /data (DataRoot)
        var ndjsonPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(_dataRoot.Path, ndjsonRelativePath));
        var indexPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(_dataRoot.Path, indexRelativePath));

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ndjsonPath)!);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(indexPath)!);

        // Materialize once so we can log counts and avoid multi-enumeration surprises
        var itemList = items as IList<T> ?? items.ToList();

        _logger.LogInformation(
            "DedupingNdjsonSink starting. DataRoot={DataRoot} Ndjson={NdjsonPath} Index={IndexPath} Items={Count}",
            _dataRoot.Path,
            ndjsonPath,
            indexPath,
            itemList.Count);

        var index = await LoadIndexAsync(indexPath, ct);

        // If you want a breadcrumb file even when nothing changes, create it now
        if (ensureFileExistsEvenIfNoChanges && !System.IO.File.Exists(ndjsonPath))
        {
            await System.IO.File.WriteAllTextAsync(ndjsonPath, string.Empty, ct);
            _logger.LogInformation("Created empty NDJSON file at {NdjsonPath}", ndjsonPath);
        }

        var appended = 0;

        // Open append stream once for the whole run (fast, and creates file if missing)
        await using var stream = new FileStream(
            ndjsonPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read);

        await using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        foreach (var item in itemList)
        {
            ct.ThrowIfCancellationRequested();

            var key = identityKey(item);
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("identityKey produced a null/empty key.");

            var fingerprint = changeFingerprint(item) ?? string.Empty;

            if (index.TryGetValue(key, out var last) && last == fingerprint)
                continue; // unchanged -> skip

            var json = JsonSerializer.Serialize(item, JsonOpts);
            await writer.WriteLineAsync(json.AsMemory(), ct);

            index[key] = fingerprint;
            appended++;
        }

        await writer.FlushAsync(); // make sure content hits disk

        if (appended > 0)
        {
            await SaveIndexAsync(indexPath, index, ct);

            _logger.LogInformation(
                "DedupingNdjsonSink appended {Count} row(s). RelativeNdjson={RelNdjson} RelativeIndex={RelIndex}",
                appended,
                ndjsonRelativePath,
                indexRelativePath);
        }
        else
        {
            _logger.LogInformation(
                "DedupingNdjsonSink detected no changes. Nothing appended. RelativeNdjson={RelNdjson}",
                ndjsonRelativePath);
        }

        // Extra confirmation: does the file exist/how big is it?
        try
        {
            var fi = new FileInfo(ndjsonPath);
            _logger.LogInformation("NDJSON file exists={Exists} sizeBytes={Size}", fi.Exists, fi.Exists ? fi.Length : 0);
        }
        catch { /* no-op */ }

        return appended;
    }

    private static async Task<Dictionary<string, string>> LoadIndexAsync(string indexPath, CancellationToken ct)
    {
        if (!System.IO.File.Exists(indexPath))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var json = await System.IO.File.ReadAllTextAsync(indexPath, ct);

        // Deserialize may return null; also normalize to Ordinal comparer for consistent keying
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return parsed is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(parsed, StringComparer.Ordinal);
    }

    private static async Task SaveIndexAsync(string indexPath, Dictionary<string, string> index, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(index, IndexJsonOpts);
        await System.IO.File.WriteAllTextAsync(indexPath, json, ct);
    }

    public static string Sha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
