// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Text;

namespace Sammo.Oeis.Tests;

public static class OeisIdTests
{
    static readonly OeisId A000796 = new(796);

    static readonly OeisId A001622 = new(1622);

    static readonly OeisId A1234567 = new(1234567);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public static void Ctor_LessThanOne_Throws(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OeisId(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OeisId(value));
    }

    [Fact]
    public static void CtorAndCast_PositiveInt_ValueMatches()
    {
        Assert.Equal(796, A000796.Value);
        Assert.Equal(1622, (int)A001622);
        Assert.Equal((OeisId)A000796.Value, new OeisId((int)A000796));

    }

    [Fact]
    public static void CompareTo_VariousValues_MatchesUnderlyingIntComparison()
    {
        Assert.Equal(0, A000796.CompareTo(A000796));
        Assert.Equal(0, A001622.CompareTo(A001622));
        Assert.True(A001622.CompareTo(A000796) > 0);
        Assert.True(A000796.CompareTo(A001622) < 0);
    }

    public static IEnumerable<object[]> OeisIdStrings =>
    [
        [A001622, nameof(A001622)],
        [A000796, nameof(A000796)],
        [A1234567, nameof(A1234567)]
    ];

    [Theory]
    [MemberData(nameof(OeisIdStrings))]
    public static void ToString_VariousValues_MatchesFormat(OeisId oeisId, string expected)
    {
        Assert.Equal(expected, oeisId.ToString());
    }

    [Theory]
    [MemberData(nameof(OeisIdStrings))]
    public static void Format_VariousValues_MatchesFormat(OeisId oeisId, string expected)
    {
        Span<char> destination = stackalloc char[OeisId.MaxStringLength];
        oeisId.TryFormat(destination, out var charsWritten);

        Assert.Equal(expected, destination[..charsWritten]);
    }

    [Theory]
    [MemberData(nameof(OeisIdStrings))]
    public static void FormatUtf8_VariousValues_MatchesFormat(OeisId oeisId, string expected)
    {
        Span<byte> destination = stackalloc byte[OeisId.MaxStringLength];
        oeisId.TryFormat(destination, out var charsWritten);

        Assert.Equal(GetUtf8(expected), destination[..charsWritten]);
    }

    public static IEnumerable<object[]> PaddedValues =>
    [
        [A001622, nameof(A001622)[1..]],
        [A000796, nameof(A000796)[1..]],
        [A1234567, nameof(A1234567)[1..]]
    ];

    [Theory]
    [MemberData(nameof(PaddedValues))]
    public static void GetPaddedValue_VariousValues_MatchesPaddedInt(OeisId oeisId, string expected)
    {
        Assert.Equal(expected, oeisId.GetPaddedValue());

        Assert.Equal(oeisId.Value, Int32.Parse(oeisId.GetPaddedValue()));
    }

    [Theory]
    [MemberData(nameof(PaddedValues))]
    public static void TryGetPaddedValue_VariousValues_MatchesPaddedInt(OeisId oeisId, string expected)
    {
        Span<char> destination = stackalloc char[OeisId.MaxStringLength];

        Assert.True(oeisId.TryGetPaddedValue(destination, out var bytesWritten));
        Assert.Equal(expected, destination[..bytesWritten]);
        Assert.Equal(oeisId.Value, Int32.Parse(destination[..bytesWritten]));
    }

    [Theory]
    [MemberData(nameof(PaddedValues))]
    public static void TryGetPaddedValueUtf8_VariousValues_MatchesPaddedInt(OeisId oeisId, string expected)
    {
        Span<byte> destination = stackalloc byte[OeisId.MaxStringLength];

        Assert.True(oeisId.TryGetPaddedValue(destination, out var bytesWritten));
        Assert.Equal(GetUtf8(expected), destination[..bytesWritten]);
        Assert.Equal(oeisId.Value, Int32.Parse(destination[..bytesWritten]));
    }

    public static IEnumerable<object[]> BadParseStrings =>
    [
        [""],
        ["B0023"],
        ["000796A", OeisId.ParseOption.Lax],
        ["000796A"],
        ["3747"],
    ];

    [Theory]
    [MemberData(nameof(BadParseStrings))]
    public static void Parse_Garbage_Throws(string badParseString, OeisId.ParseOption? parseOption = null)
    {
        if (parseOption is { } option)
        {
            Assert.Throws<FormatException>(() => OeisId.Parse(badParseString, option));
        }
        else
        {
            Assert.Throws<FormatException>(() => OeisId.Parse(badParseString));
        }
    }

    [Theory]
    [MemberData(nameof(BadParseStrings))]
    public static void ParseUtf8_Garbage_Throws(string badParseString, OeisId.ParseOption? parseOption = null)
    {
        if (parseOption is { } option)
        {
            Assert.Throws<FormatException>(() => OeisId.Parse(GetUtf8(badParseString), option));
        }
        else
        {
            Assert.Throws<FormatException>(() => OeisId.Parse(GetUtf8(badParseString)));
        }
    }

    [Theory]
    [MemberData(nameof(OeisIdStrings))]
    public static void Parse_VariousValues_GrabsValue(OeisId id, string stringValue)
    {
        Assert.Equal(id, OeisId.Parse(stringValue));

        Assert.Equal(id, OeisId.Parse(stringValue.ToLower(), OeisId.ParseOption.Lax));

        Assert.Equal(id, OeisId.Parse(id.Value.ToString(), OeisId.ParseOption.Lax));

        Assert.Equal(id, OeisId.Parse(id.GetPaddedValue(), OeisId.ParseOption.Lax));

        Assert.Equal(id.Value, OeisId.Parse(stringValue).Value);
    }

    [Theory]
    [MemberData(nameof(OeisIdStrings))]
    public static void ParseUtf8_VariousValues_GrabsValue(OeisId id, string stringValue)
    {
        var utf8Value = GetUtf8(stringValue);

        Assert.Equal(id, OeisId.Parse(utf8Value));

        Assert.Equal(id, OeisId.Parse(GetUtf8(stringValue.ToLower()), OeisId.ParseOption.Lax));

        Assert.Equal(id, OeisId.Parse(GetUtf8(id.Value.ToString()), OeisId.ParseOption.Lax));

        Assert.Equal(id, OeisId.Parse(GetUtf8(id.GetPaddedValue()), OeisId.ParseOption.Lax));

        Assert.Equal(id.Value, OeisId.Parse(utf8Value).Value);
    }

    static ReadOnlySpan<byte> GetUtf8(string value) =>
        Encoding.UTF8.GetBytes(value);
}
