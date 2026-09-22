using static PublishSnapshot.Preview;

namespace PublishSnapshot;

// Original game-object evidence is checked before export. This handoff binds the
// public files to that trusted job's digest and checks their public contract again.
internal sealed record PublicExport(SnapshotFiles Snapshot, Metadata Metadata, string ExportSha256) : IDisposable
{
    public IReadOnlyDictionary<string, SnapshotFile> Files => Snapshot.Files;

    public static PublicExport Read(string directory, string? expectedExportSha256 = null)
    {
        var snapshot = SnapshotFiles.CapturePublic(directory);
        try
        {
            foreach (var (path, file) in snapshot.Files)
                Require(file.Length <= DataSnapshot.MaximumBlobBytes, $"Published file exceeds 100 MiB: {path}");
            var exportSha256 = ContentDigest.ExportFiles(snapshot.Files);
            Require(expectedExportSha256 is null || exportSha256 == expectedExportSha256,
                "Export digest differs from the trusted extraction result.");
            var metadata = Metadata.Parse(File.ReadAllText(snapshot.Files["metadata.json"].Path));
            Require(ContentDigest.Files(snapshot.Files) == metadata.ContentSha256, "Export content digest differs from its metadata.");
            using var assets = ReadJson(snapshot.Files, "assets.json");
            var localizations = ResourceEvidence.ReadLocalizations(snapshot.Files);
            var resources = ResourceEvidence.ReadResources(snapshot.Files, static _ => true);
            // Only the upstream full preview can establish original object existence.
            ValidateCatalog(assets.RootElement, localizations, resources, static _ => true);
            CsvChecks.Validate(assets.RootElement, snapshot.Files);
            return new(snapshot, metadata, exportSha256);
        }
        catch { snapshot.Dispose(); throw; }
    }

    public void Dispose() => Snapshot.Dispose();
}
