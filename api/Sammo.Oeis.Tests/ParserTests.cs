// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

namespace Sammo.Oeis.Tests;

public class SeachResultParserTests : IClassFixture<TestDataProvider>
{
    readonly TestDataProvider _testDataProvider;

    public SeachResultParserTests(TestDataProvider testDataProvider)
    {
        _testDataProvider = testDataProvider;
    }

    [Fact]
    public async Task SearchResultParser_ParseFound_ReturnsResult()
    {
        await using var searchResponse = _testDataProvider.GetTestData("oeis-search-item-found");
        var result = await SeachResultParser.ParseAsync(searchResponse);

        Assert.Equal(1571, result.Index);
        Assert.Equal(12643, result.TotalCount);
        Assert.Equal(OeisId.Parse("A019798"), result.Sequence!.Id);
        Assert.Equal("Decimal expansion of sqrt(2*e).", result.Sequence.Name);
        Assert.Equal(1, result.Sequence.Offset);
    }

    [Fact]
    public async Task SearchResultParser_ParseNotFound_ReturnsEmptyResult()
    {
        await using var searchResponse = _testDataProvider.GetTestData("oeis-search-not-found");
        var result = await SeachResultParser.ParseAsync(searchResponse);

        Assert.Equal(OeisSearchResult.Empty, result);
    }

    [Fact]
    public async Task BFileParserParser_ParseUnlimited_ReturnsFullResult()
    {
        await using var bFile = _testDataProvider.GetTestData("A021575-bfile");
        var terms = await BFileParser.ParseAsync(OeisId.Parse("A021575"), bFile, null);

        Assert.Equal(99, terms.Count);
        Assert.True(terms is [0, 0, 1, 7, 5, .., 0, 1, 9, 2, 6]);
    }

    [Fact]
    public async Task BFileParserParser_ParseWithLimit_ReturnsCappedResult()
    {
        await using var bFile = _testDataProvider.GetTestData("A021575-bfile");
        var terms = await BFileParser.ParseAsync(OeisId.Parse("A021575"), bFile, 20);

        Assert.Equal(20, terms.Count);
    }

    [Fact]
    public async Task BFileParserParser_ParseWithDoubleDigitTerm_ThrowsException()
    {
        await using var bFile = _testDataProvider.GetTestData("A105309-bfile");

        var ex = await Assert.ThrowsAsync<OeisClientException>(() =>
            BFileParser.ParseAsync(OeisId.Parse("A105309"), bFile, null));
        Assert.Equal(ClientExceptionCause.InvalidSequence, ex.Cause);
        Assert.Contains("single decimal digit", ex.Message);
    }

    [Fact]
    public async Task BFileParserParser_ParseWithNegativeTerm_ThrowsException()
    {
        await using var bFile = _testDataProvider.GetTestData("A290737-bfile");

        var ex = await Assert.ThrowsAsync<OeisClientException>(() =>
            BFileParser.ParseAsync(OeisId.Parse("A290737"), bFile, null));
        Assert.Equal(ClientExceptionCause.InvalidSequence, ex.Cause);
        Assert.Contains("single decimal digit", ex.Message);
    }
}
