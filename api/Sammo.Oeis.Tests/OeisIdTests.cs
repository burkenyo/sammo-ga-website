// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Text;

namespace Sammo.Oeis.Tests;

public static class OeisIdTests
{
    static readonly OeisId A000796 = (OeisId) 796;

    static readonly OeisId A001622 = (OeisId) 1622;

    static readonly OeisId A1234567 = (OeisId) 1234567;

    [Fact]
    public static void Ctor_LessThanOne_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OeisId(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OeisId(-1));
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

    [Fact]
    public static void ToString_VariousValues_MatchesFormat()
    {
        Assert.Equal(nameof(A000796), A000796.ToString());
        Assert.Equal(nameof(A001622), A001622.ToString());
        Assert.Equal(nameof(A1234567), A1234567.ToString());
    }

    [Fact]
    public static void Format_VariousValues_MatchesFormat()
    {
        Span<char> destination = stackalloc char[OeisId.MaxStringLength];
        int charsWritten;

        A000796.TryFormat(destination, out charsWritten);
        Assert.Equal(nameof(A000796), destination[..charsWritten]);

        A001622.TryFormat(destination, out charsWritten);
        Assert.Equal(nameof(A001622), destination[..charsWritten]);

        A1234567.TryFormat(destination, out charsWritten);
        Assert.Equal(nameof(A1234567), destination[..charsWritten]);
    }

    [Fact]
    public static void FormatUtf8_VariousValues_MatchesFormat()
    {
        Span<byte> destination = stackalloc byte[OeisId.MaxStringLength];
        int charsWritten;

        A000796.TryFormat(destination, out charsWritten);
        Assert.Equal(GetUtf8(nameof(A000796)), destination[..charsWritten]);

        A001622.TryFormat(destination, out charsWritten);
        Assert.Equal(GetUtf8(nameof(A001622)), destination[..charsWritten]);

        A1234567.TryFormat(destination, out charsWritten);
        Assert.Equal(GetUtf8(nameof(A1234567)), destination[..charsWritten]);
    }

    [Fact]
    public static void GetPaddedValue_VariousValues_MatchesPaddedInt()
    {
        Assert.Equal(nameof(A000796)[1..], A000796.GetPaddedValue());
        Assert.Equal(nameof(A001622)[1..], A001622.GetPaddedValue());
        Assert.Equal(nameof(A1234567)[1..], A1234567.GetPaddedValue());

        Assert.Equal(A000796.Value, Int32.Parse(A000796.GetPaddedValue()));
        Assert.Equal(A001622.Value, Int32.Parse(A001622.GetPaddedValue()));
        Assert.Equal(A1234567.Value, Int32.Parse(A1234567.GetPaddedValue()));
    }

    [Fact]
    public static void TryGetPaddedValue_VariousValues_MatchesPaddedInt()
    {
        Span<char> destination = stackalloc char[OeisId.MaxStringLength];
        int bytesWritten;

        Assert.True(A000796.TryGetPaddedValue(destination, out bytesWritten));
        Assert.Equal(nameof(A000796)[1..], destination[..bytesWritten]);
        Assert.Equal(A000796.Value, Int32.Parse(destination[..bytesWritten]));

        Assert.True(A001622.TryGetPaddedValue(destination, out bytesWritten));
        Assert.Equal(nameof(A001622)[1..], destination[..bytesWritten]);
        Assert.Equal(A001622.Value, Int32.Parse(destination[..bytesWritten]));

        Assert.True(A1234567.TryGetPaddedValue(destination, out bytesWritten));
        Assert.Equal(nameof(A1234567)[1..], destination[..bytesWritten]);
        Assert.Equal(A1234567.Value, Int32.Parse(destination[..bytesWritten]));
    }

    [Fact]
    public static void TryGetPaddedValueUtf8_VariousValues_MatchesPaddedInt()
    {
        Span<byte> destination = stackalloc byte[OeisId.MaxStringLength];
        int bytesWritten;

        Assert.True(A000796.TryGetPaddedValue(destination, out bytesWritten));
        Assert.Equal(GetUtf8(nameof(A000796)[1..]), destination[..bytesWritten]);
        Assert.Equal(A000796.Value, Int32.Parse(destination[..bytesWritten]));

        Assert.True(A001622.TryGetPaddedValue(destination, out bytesWritten));
        Assert.Equal(GetUtf8(nameof(A001622)[1..]), destination[..bytesWritten]);
        Assert.Equal(A001622.Value, Int32.Parse(destination[..bytesWritten]));

        Assert.True(A1234567.TryGetPaddedValue(destination, out bytesWritten));
        Assert.Equal(GetUtf8(nameof(A1234567)[1..]), destination[..bytesWritten]);
        Assert.Equal(A1234567.Value, Int32.Parse(destination[..bytesWritten]));
    }

    [Fact]
    public static void Parse_Garbage_Throws()
    {
        Assert.Throws<FormatException>(() => OeisId.Parse(""));
        Assert.Throws<FormatException>(() => OeisId.Parse("B0023"));
        Assert.Throws<FormatException>(() => OeisId.Parse("000796A"));
        Assert.Throws<FormatException>(() => OeisId.Parse("000796A", OeisId.ParseOption.Lax));
        Assert.Throws<FormatException>(() => OeisId.Parse(A000796.Value.ToString()));
    }

    [Fact]
    public static void ParseUtf8_Garbage_Throws()
    {
        Assert.Throws<FormatException>(() => OeisId.Parse(""u8));
        Assert.Throws<FormatException>(() => OeisId.Parse("B0023"u8));
        Assert.Throws<FormatException>(() => OeisId.Parse("000796A"u8));
        Assert.Throws<FormatException>(() => OeisId.Parse("000796A"u8, OeisId.ParseOption.Lax));
        Assert.Throws<FormatException>(() => OeisId.Parse(GetUtf8(A000796.Value)));
    }

    [Fact]
    public static void Parse_VariousValues_GrabsValue()
    {
        Assert.Equal(A000796, OeisId.Parse(nameof(A000796)));
        Assert.Equal(A001622, OeisId.Parse(nameof(A001622)));
        Assert.Equal(A1234567, OeisId.Parse(nameof(A1234567)));

        Assert.Equal(A000796, OeisId.Parse(nameof(A000796).ToLower(), OeisId.ParseOption.Lax));
        Assert.Equal(A001622, OeisId.Parse(nameof(A001622).ToLower(), OeisId.ParseOption.Lax));
        Assert.Equal(A1234567, OeisId.Parse(nameof(A1234567).ToLower(), OeisId.ParseOption.Lax));

        Assert.Equal(A000796, OeisId.Parse(A000796.Value.ToString(), OeisId.ParseOption.Lax));
        Assert.Equal(A001622, OeisId.Parse(A001622.Value.ToString(), OeisId.ParseOption.Lax));
        Assert.Equal(A1234567, OeisId.Parse(A1234567.Value.ToString(), OeisId.ParseOption.Lax));

        Assert.Equal(A000796, OeisId.Parse(A000796.GetPaddedValue(), OeisId.ParseOption.Lax));
        Assert.Equal(A001622, OeisId.Parse(A001622.GetPaddedValue(), OeisId.ParseOption.Lax));
        Assert.Equal(A1234567, OeisId.Parse(A1234567.GetPaddedValue(), OeisId.ParseOption.Lax));

        Assert.Equal(A000796.Value, OeisId.Parse(nameof(A000796)).Value);
        Assert.Equal(A001622.Value, OeisId.Parse(nameof(A001622)).Value);
        Assert.Equal(A1234567.Value, OeisId.Parse(nameof(A1234567)).Value);
    }

    [Fact]
    public static void ParseUtf8_VariousValues_GrabsValue()
    {
        Assert.Equal(A000796, OeisId.Parse(GetUtf8(nameof(A000796))));
        Assert.Equal(A001622, OeisId.Parse(GetUtf8(nameof(A001622))));
        Assert.Equal(A1234567, OeisId.Parse(GetUtf8(nameof(A1234567))));

        Assert.Equal(A000796, OeisId.Parse(GetUtf8(nameof(A000796).ToLower()), OeisId.ParseOption.Lax));
        Assert.Equal(A001622, OeisId.Parse(GetUtf8(nameof(A001622).ToLower()), OeisId.ParseOption.Lax));
        Assert.Equal(A1234567, OeisId.Parse(GetUtf8(nameof(A1234567).ToLower()), OeisId.ParseOption.Lax));

        Assert.Equal(A000796, OeisId.Parse(GetUtf8(A000796.Value), OeisId.ParseOption.Lax));
        Assert.Equal(A001622, OeisId.Parse(GetUtf8(A001622.Value), OeisId.ParseOption.Lax));
        Assert.Equal(A1234567, OeisId.Parse(GetUtf8(A1234567.Value), OeisId.ParseOption.Lax));

        Assert.Equal(A000796, OeisId.Parse(GetUtf8(A000796.GetPaddedValue()), OeisId.ParseOption.Lax));
        Assert.Equal(A001622, OeisId.Parse(GetUtf8(A001622.GetPaddedValue()), OeisId.ParseOption.Lax));
        Assert.Equal(A1234567, OeisId.Parse(GetUtf8(A1234567.GetPaddedValue()), OeisId.ParseOption.Lax));

        Assert.Equal(A000796.Value, OeisId.Parse(GetUtf8(nameof(A000796))).Value);
        Assert.Equal(A001622.Value, OeisId.Parse(GetUtf8(nameof(A001622))).Value);
    }

    static ReadOnlySpan<byte> GetUtf8(object o) =>
        Encoding.UTF8.GetBytes(o.ToString()!);
}
