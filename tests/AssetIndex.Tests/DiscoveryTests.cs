using CUE4Parse.UE4.Exceptions;

namespace AssetIndex.Tests;

public sealed class DiscoveryTests
{
    [Fact]
    public void WrappedParserErrorRetainsThePropertyContextAndCauseWithoutAStackTrace()
    {
        var cause = Assert.Throws<InvalidDataException>((Action)(() => throw new InvalidDataException("Unknown AITemplate property.")));
        var error = new ParserException("Could not deserialize AITemplateDataAsset.",
            new ParserException("Could not read BoolProperty Enabled.", cause));

        Assert.Equal("Could not deserialize AITemplateDataAsset. Cause: Could not read BoolProperty Enabled. Cause: Unknown AITemplate property.",
            AssetDiscovery.DescribeError(error));
    }

    [Fact]
    public void RepeatedParserMessageIsNotDuplicated()
    {
        var error = new ParserException("Invalid property.", new InvalidDataException("Invalid property."));

        Assert.Equal("Invalid property.", AssetDiscovery.DescribeError(error));
    }

    [Fact]
    public void UnwrappedErrorKeepsItsMessage()
    {
        Assert.Equal("Missing package.", AssetDiscovery.DescribeError(new FileNotFoundException("Missing package.")));
    }
}
