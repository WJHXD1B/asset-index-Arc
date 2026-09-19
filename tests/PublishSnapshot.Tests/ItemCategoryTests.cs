using System.Text.Json.Nodes;
using static PublishSnapshot.Tests.IdentityFixture;

namespace PublishSnapshot.Tests;

public sealed class ItemCategoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CategoryMustMatchTheDecodedItemOrItsTemplate(bool inherited)
    {
        using var fixture = Fixture();
        var parent = inherited ? fixture.Object("/Game/Template.Template", "ItemDataAsset") : null;
        var item = Item(fixture, parent?["path"]!.GetValue<string>());
        Scalar(parent ?? item, "ItemCategory", "EnumProperty", "name", "EItemCategory::Currency");
        var catalog = Catalog("42", [item]);
        catalog["definitions"]![0]!["itemCategory"] = "EItemCategory::Currency";
        var context = fixture.Read().Context;
        Validate(context, catalog);

        catalog["definitions"]![0]!["itemCategory"] = "EItemCategory::Weapon";
        Assert.Throws<InvalidDataException>(() => Validate(context, catalog));
        catalog["definitions"]![0]!.AsObject().Remove("itemCategory");
        Assert.Throws<InvalidDataException>(() => Validate(context, catalog));
    }

    [Fact]
    public void MissingCategoryIsNotInferredFromAnItemName()
    {
        using var fixture = Fixture();
        var item = Item(fixture);
        var catalog = Catalog("42", [item]);
        var context = fixture.Read().Context;
        Validate(context, catalog);
        catalog["definitions"]![0]!["itemCategory"] = "EItemCategory::Currency";
        Assert.Throws<InvalidDataException>(() => Validate(context, catalog));
    }

    [Theory]
    [InlineData("NameProperty", "name")]
    [InlineData("EnumProperty", "integer")]
    public void NonEnumEvidenceCannotSupplyCurrencyCategory(string type, string kind)
    {
        using var fixture = Fixture();
        var item = Item(fixture);
        Scalar(item, "ItemCategory", type, kind, "EItemCategory::Currency");
        var context = fixture.Read().Context;
        Assert.Throws<InvalidDataException>(() => context.ItemCategory("/Game/Currency.Currency"));
    }

    [Fact]
    public void AnUnrelatedTemplateCannotSupplyTheCategory()
    {
        using var fixture = Fixture();
        fixture.AddClass("Other", "DataAsset", ("ItemCategory", "EnumProperty"));
        var parent = fixture.Object("/Game/Other.Other", "Other");
        Scalar(parent, "ItemCategory", "EnumProperty", "name", "EItemCategory::Currency");
        Item(fixture, "/Game/Other.Other");
        var context = fixture.Read().Context;
        Assert.Throws<InvalidDataException>(() => context.ItemCategory("/Game/Currency.Currency"));
    }

    [Fact]
    public void TruncatedEnumEvidenceCannotBecomeAnAbsentCategory()
    {
        using var fixture = Fixture();
        var item = Item(fixture);
        Scalar(item, "ItemCategory", "EnumProperty", "name", "EItemCategory::Currency");
        var values = item["values"]!.AsArray();
        values.RemoveAt(values.Count - 1);
        var context = fixture.Read().Context;
        var error = Assert.Throws<InvalidDataException>(() => Validate(context, Catalog("42", [item])));
        Assert.Contains("Missing or ambiguous scalar", error.Message);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("array")]
    [InlineData("skipped")]
    public void MalformedCategoryEvidenceIsRejected(string mutation)
    {
        using var fixture = Fixture();
        var item = Item(fixture);
        Scalar(item, "ItemCategory", "EnumProperty", "name", "EItemCategory::Currency");
        var header = item["properties"]!.AsArray().Last()!;
        if (mutation == "duplicate") item["values"]!.AsArray().Add(item["values"]!.AsArray().Last()!.DeepClone());
        else if (mutation == "array") header["arraySize"] = 2;
        else header["serializeType"] = "Skipped";
        var context = fixture.Read().Context;
        Assert.Throws<InvalidDataException>(() => context.ItemCategory("/Game/Currency.Currency"));
    }

    private static IdentityFixture Fixture()
    {
        var fixture = new IdentityFixture();
        fixture.AddClass("ItemDataAsset", "ItemDataAssetBase", ("ItemCategory", "EnumProperty"));
        return fixture;
    }

    private static JsonObject Item(IdentityFixture fixture, string? template = null)
    {
        var item = fixture.Object("/Game/Currency.Currency", "ItemDataAsset", template);
        Boolean(item, "bOverrideItemAssetId", true);
        Number(item, "OverrideItemAssetId", "42");
        return item;
    }
}

public sealed partial class PublisherTests
{
    [Fact]
    public void ForgingCurrencyCategoryCannotPublishOrAdvanceTheDataBranch()
    {
        ChangeJson("assets.json", node => node[0]!["definitions"]![0]!["itemCategory"] = "EItemCategory::Currency");
        var error = Assert.Throws<InvalidDataException>(() => Publisher.Publish(preview, remote, NextExtractor, "456"));
        Assert.Contains("item category differs from typed object evidence", error.Message);
        Assert.Equal(initialCommit, RemoteRef("refs/heads/data"));
    }

    [Fact]
    public void CategoryIsNotAnAllowedMetadataField()
    {
        ChangeJson("assets.json", node => node[0]!["metadata"]![0]!["itemCategory"] = "EItemCategory::Currency");
        Assert.Throws<InvalidDataException>(() => Publisher.Publish(preview, remote, NextExtractor, "456"));
        Assert.Equal(initialCommit, RemoteRef("refs/heads/data"));
    }
}
