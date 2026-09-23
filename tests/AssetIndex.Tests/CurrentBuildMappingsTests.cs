using CUE4Parse.GameTypes.Theia.FileProvider;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace AssetIndex.Tests;

public sealed class CurrentBuildMappingsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SkillTreeVersionKeepsInheritedPersistenceReferenceAligned(bool zeroVersion)
    {
        using var bytes = new MemoryStream();
        using var writer = new BinaryWriter(bytes);
        // Slots 3 and 4: the new version field, then the inherited persistence reference.
        writer.Write((ushort)(0x0503 | (zeroVersion ? 0x80 : 0)));
        if (zeroVersion) writer.Write((byte)1);
        else writer.Write(7);
        writer.Write(37);

        var value = Read("CharacterSkillDataAsset", bytes.ToArray());

        Assert.Equal(zeroVersion ? 0 : 7, value.Get<int>("BelongsToSkillTreeVersion"));
        Assert.Equal(37, value.Get<FPackageIndex>("PersistenceDataAsset").Index);
    }

    [Theory]
    [InlineData("NPCTradeScreen", 12, 26, false)]
    [InlineData("NPCTradeScreen", 12, 26, true)]
    [InlineData("BlackMarketScreen", 9, 24, false)]
    [InlineData("BlackMarketScreen", 9, 24, true)]
    public void WarningWidgetKeepsInheritedInputMappingAndPriorityAligned(
        string className, int warningSlot, int inputSlot, bool nullWarning)
    {
        using var bytes = new MemoryStream();
        using var writer = new BinaryWriter(bytes);
        // One warning reference, then two consecutive inherited input properties.
        writer.Write((ushort)(warningSlot | 0x0200 | (nullWarning ? 0x80 : 0)));
        writer.Write((ushort)(inputSlot - warningSlot - 1 | 0x0500));
        if (nullWarning) writer.Write((byte)1);
        else writer.Write(11);
        writer.Write(23);
        writer.Write(123);

        var value = Read(className, bytes.ToArray());

        Assert.Equal(nullWarning ? 0 : 11, value.Get<FPackageIndex>("CredSunsetWarning").Index);
        Assert.Equal(23, value.Get<FPackageIndex>("InputMapping").Index);
        Assert.Equal(123, value.Get<int>("InputMappingPriority"));
    }

    private static FStructFallback Read(string className, byte[] bytes)
    {
        using var provider = new TheiaFileProvider(AppContext.BaseDirectory, SearchOption.TopDirectoryOnly,
            new VersionContainer(EGame.GAME_ArcRaiders));
        provider.MappingsContainer = new FileUsmapTypeMappingsProvider(
            Path.Combine(AppContext.BaseDirectory, "mappings", "ArcRaiders.usmap"));
        using var archive = new FAssetArchive(new FByteArchive("synthetic-current-mapping", bytes, provider.Versions),
            new FixturePackage(provider));
        var value = new FStructFallback(archive, className);
        Assert.Equal(archive.Length, archive.Position);
        return value;
    }

    private sealed class FixturePackage(TheiaFileProvider provider) : AbstractUePackage("Fixture", provider)
    {
        public override FPackageFileSummary Summary { get; } = new() { PackageFlags = EPackageFlags.PKG_UnversionedProperties };
        public override FNameEntrySerialized[] NameMap => [];
        public override int ImportMapLength => 0;
        public override int ExportMapLength => 0;
        public override int GetExportIndex(string name, StringComparison comparisonType = StringComparison.Ordinal) => -1;
        public override ResolvedObject? ResolvePackageIndex(FPackageIndex? index) => null;
    }
}
