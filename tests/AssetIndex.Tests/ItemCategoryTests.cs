using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.UObject;

namespace AssetIndex.Tests;

public sealed class ItemCategoryTests
{
    private static readonly Lazy<TypeMappings> Mappings = new(() =>
        new FileUsmapTypeMappingsProvider(Path.Combine(AppContext.BaseDirectory, "mappings", "ArcRaiders.usmap")).MappingsForGame!);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CapturesAuthoredCategoryFromItemOrTemplate(bool inherited)
    {
        var item = Item();
        var owner = inherited ? Item() : item;
        if (inherited) item.Template = new ResolvedLoadedObject(owner);
        owner.Properties.Add(new FPropertyTag { Name = "ItemCategory", Tag = new EnumProperty(new FName("EItemCategory::Currency")) });
        var source = Collect(item);
        Assert.Equal("EItemCategory::Currency", source.Reference.ItemCategory);
    }

    [Fact]
    public void MissingCategoryStaysAbsentAndAnAuthoredOverrideWins()
    {
        var item = Item();
        Assert.Null(Collect(item).Reference.ItemCategory);
        var template = Item();
        template.Properties.Add(new FPropertyTag { Name = "ItemCategory", Tag = new EnumProperty(new FName("EItemCategory::Currency")) });
        item.Template = new ResolvedLoadedObject(template);
        item.Properties.Add(new FPropertyTag { Name = "ItemCategory", Tag = new EnumProperty(new FName("EItemCategory::Weapon")) });
        Assert.Equal("EItemCategory::Weapon", Collect(item).Reference.ItemCategory);
    }

    [Fact]
    public void ANamePropertyCannotImpersonateTheCategoryEnum()
    {
        var item = Item();
        item.Properties.Add(new FPropertyTag { Name = "ItemCategory", Tag = new NameProperty(new FName("EItemCategory::Currency")) });
        var issues = new List<ExtractionIssue>();
        Assert.Empty(Assets.Collect([item], Mappings.Value, issues));
        Assert.Contains(issues, issue => issue.Message.Contains("not a decoded enum"));
    }

    private static CatalogSource Collect(UObject item)
    {
        var issues = new List<ExtractionIssue>();
        var asset = Assert.Single(Assets.Collect([item], Mappings.Value, issues));
        Assert.Empty(issues);
        return Assert.Single(asset.Definitions);
    }

    private static UObject Item() => new([
        new FPropertyTag { Name = "bOverrideItemAssetId", Tag = new BoolProperty(true) },
        new FPropertyTag { Name = "OverrideItemAssetId", Tag = new Int64Property(42) }
    ]) { Name = "UninformativeName", Class = new ResolvedLoadedObject(new UScriptClass("ItemDataAsset")) };
}
