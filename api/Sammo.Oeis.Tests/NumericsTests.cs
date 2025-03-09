// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Numerics;

namespace Sammo.Oeis.Tests;

public static class NumericsTests
{
    static void AssertFractional<T>(T value, int radix, string digits) where T : Fractional
    {
        digits = String.Concat(digits.Where(d => d != '_'));
        var digitEnumerator = digits
            .Where(d => d != ':')
            .Select(d => d <= '9' ? (byte)(d - '0') : (byte)(d - 'A' + 10));

        Assert.Same(typeof(T), value.GetType());
        Assert.Equal(radix, value.Radix);
        Assert.Equal(digitEnumerator, value.Digits);
        Assert.Equal(digits.IndexOf(':'), value.Offset);
    }

    public static IEnumerable<object[]> BigDecimalFactories =>
    [
        [() => BigDecimal.Create([3, 7, 8], 2), "37:8"],
        [() => BigDecimal.FromDecimal(89.6000m), "89:600_0"],
        [() => BigDecimal.FromDouble(23.3125), "23:3"],
        [() => BigDecimal.FromFractional(BigDecimal.FromDouble(23.3125)), "23:3"],
        [() => BigDecimal.FromInteger(16), "16:"],
        [() => BigDecimal.FromInteger(16_700_000_000_000_000_000UL), "16_700_000_000_000_000_000:"],
        [() => BigDecimal.FromInteger(BigInteger.One), "1:"],
        [() => BigDecimal.FromRatio(400, 8_361_293, 20), ":000_047_839_490_853_866_74"]
    ];

    [Theory]
    [MemberData(nameof(BigDecimalFactories))]
    public static void BigDecimalFactories_ValidInput_Works(Func<BigDecimal> factory, string digits)
    {
        AssertFractional(factory(), 10, digits);
    }

    public static IEnumerable<object[]> DozenalFactories =>
    [
        [() => Dozenal.Create([3, 7, 8], 2), "37:8"],
        [() => Dozenal.FromDecimal(89.6000m), "75:724"],
        [() => Dozenal.FromDouble(23.3125), "1B:3"],
        [() => Dozenal.FromFractional(Dozenal.FromDouble(23.3125)), "1B:3"],
        [() => Dozenal.FromInteger(16L), "14:"],
        [() => Dozenal.FromInteger(16_700_000_000_000_000_000UL), "763_B08_175_142_4B7_A28:"],
        [() => Dozenal.FromInteger(BigInteger.One), "1:"],
        [() => Dozenal.FromRatio(400, 8_361_293, 20), ":000_0BA_A21_321_A1A_903_76"]
    ];

    [Theory]
    [MemberData(nameof(DozenalFactories))]
    public static void DozenalFactories_ValidInput_Works(Func<Dozenal> factory, string digits)
    {
        AssertFractional(factory(), 12, digits);
    }

    public static IEnumerable<object[]> BadBigDecimalFactories =>
    [
        //negative numbers disallowed
        [() => BigDecimal.FromDecimal(-89.6m)],
        [() => BigDecimal.FromDouble(-23.5)],
        [() => BigDecimal.FromInteger(-6_700_000_000_000_000_000)],
        [() => BigDecimal.FromInteger(BigInteger.MinusOne)],

        // mismatched sign
        [() => BigDecimal.FromRatio(-400, 8_361_293, 20)],

        // 0 denominator
        [() => BigDecimal.FromRatio(718, 0, 20)]
    ];

    [Theory]
    [MemberData(nameof(BadBigDecimalFactories))]
    public static void BigDecimalFactories_Garbage_Throws(Func<BigDecimal> factory)
    {
        Assert.Throws<ArgumentOutOfRangeException>(factory);
    }

    [Fact]
    public static void BigDecimalCreate_DigitOutOfRange_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => BigDecimal.Create([3, 7, 18], 2));
    }

    public static IEnumerable<object[]> BadDozenalFactories =>
    [
        //negative numbers disallowed
        [() => Dozenal.FromDecimal(-89.6m)],
        [() => Dozenal.FromDouble(-23.5)],
        [() => Dozenal.FromInteger(-6_700_000_000_000_000_000)],
        [() => Dozenal.FromInteger(BigInteger.MinusOne)],

        // mismatched sign
        [() => Dozenal.FromRatio(-400, 8_361_293, 20)],

        // 0 denominator
        [() => Dozenal.FromRatio(718, 0, 20)]
    ];

    [Theory]
    [MemberData(nameof(BadDozenalFactories))]
    public static void DozenalFactories_Garbage_Throws(Func<Dozenal> factory)
    {
        Assert.Throws<ArgumentOutOfRangeException>(factory);
    }

    [Fact]
    public static void DozenalCreate_DigitOutOfRange_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Dozenal.Create([3, 7, 18], 2));
    }

    [Theory]
    [InlineData("00000.0123", ":0123")]
    [InlineData(".0123", ":0123")]
    [InlineData("4.50", "4:50")]
    [InlineData("06.7", "6:7")]
    [InlineData("89", "89:")]
    [InlineData("0089.", "89:")]
    public static void BigDecimalParse_ValidString_MatchesExpected(string toParse, string digits)
    {
        AssertFractional(BigDecimal.Parse(toParse), 10, digits);
    }

    [Theory]
    [InlineData("00000;0123", ":0123")]
    [InlineData(";045", ":045")]
    [InlineData("6;70", "6:70")]
    [InlineData("08;9", "8:9")]
    [InlineData("XE", "AB:")]
    [InlineData("00XE;", "AB:")]
    public static void DozenalParse_ValidString_MatchesExpected(string toParse, string digits)
    {
        AssertFractional(Dozenal.Parse(toParse), 12, digits);
    }

    [Theory]
    [InlineData("80.2378")]
    [InlineData("0.2378")]
    [InlineData("802358")]
    public static void DecimalToString_Default_MatchesExpected(string expected)
    {
        Assert.Equal(expected, BigDecimal.Parse(expected).ToString());
    }

    [Theory]
    [InlineData("80;2378")]
    [InlineData("0;2378")]
    [InlineData("8023X8")]
    public static void DozenalToString_Default_MatchesExpected(string expected)
    {
        Assert.Equal(expected, Dozenal.Parse(expected).ToString());
    }

    [Theory]
    [InlineData(-1, 8)]
    [InlineData(9, Fractional.MinRadix - 1)]
    [InlineData(9, Fractional.MaxRadix + 1)]
    public static void DigitArrayCtor_BadArgs_Throws(int count, int radix)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fractional.DigitArray(count, radix));
    }

    [Fact]
    public static void DigitArray_GetCountAndRadix_Match()
    {
        var digits = new Fractional.DigitArray(3, 10);

        Assert.Equal(3, digits.Count);
        Assert.Equal(10, digits.Radix);
    }

    public static IEnumerable<object[]> FillTestData =>
    [
        [3, 5],
        [3, 5, 4],
        [3, 7, 6, 4],
        [3, 8, 6, 4, 7],
        [3, 10, 6, 4, 9, 7],
        [30, Fractional.MaxRadix, 6, 4, 9, 7, 6, 4, 9, 7, 6, 4, 9, 7],
        [0, 2, 6, 4, 9, 7],
        [1, 2]
    ];

    [Theory]
    [MemberData(nameof(FillTestData))]
    public static void DigitArray_Fill_FillCountMatches(int count, int radix, params int[] digits)
    {
        var digitsArr = new Fractional.DigitArray(count, radix);
        var filled = digitsArr.Fill(digits.Select(Convert.ToByte));

        var expectedCount = Math.Min(digits.Length, count);
        Assert.Equal(expectedCount, filled);
    }

    [Theory]
    [MemberData(nameof(FillTestData))]
    public static async Task DigitArray_FillAsync_FillCountMatches(int count, int radix, params int[] digits)
    {
        var digitArr = new Fractional.DigitArray(count, radix);
        var filled = await digitArr.FillAsync(MakeAsyncEnum(digits));

        var expectedCount = Math.Min(digits.Length, count);
        Assert.Equal(expectedCount, filled);
    }

    [Fact]
    public static void DigitArray_Fill_BecomesReadOnly()
    {
        var digits = new Fractional.DigitArray(1, 12);

        Assert.False(digits.ReadOnly);

        digits.Fill([11]);

        Assert.True(digits.ReadOnly);
        Assert.Throws<InvalidOperationException>(() => digits.Fill([]));
    }

    [Fact]
    public static async Task DigitArray_FillAysnc_BecomesReadOnly()
    {
        var digits = new Fractional.DigitArray(1, 12);

        Assert.False(digits.ReadOnly);

        await digits.FillAsync(MakeAsyncEnum([11]));

        Assert.True(digits.ReadOnly);
        await Assert.ThrowsAsync<InvalidOperationException>(() => digits.FillAsync(AsyncEnumerable.Empty<byte>()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public static void DigitArray_GetByInvalidIndex_Throws(int index)
    {
        var digits = new Fractional.DigitArray(4, 2);

        Assert.Throws<IndexOutOfRangeException>(() => digits[index]);
    }

    static IReadOnlyList<byte> s_expectedDigits =>
    [
        3, 26, 7, 0, 79,
        56, 38, 1, 15, 32,
        37, 40, 45, 6, 72,
        4, 9, 32, 27, 50
    ];

    [Fact]
    public static void DigitArray_GetByValidIndex_MatchesExpected()
    {
        var digits = new Fractional.DigitArray(s_expectedDigits.Count, s_expectedDigits.Max() + 1);

        digits.Fill(s_expectedDigits);

        for (var i = 0; i < s_expectedDigits.Count; i++)
        {
            Assert.Equal(s_expectedDigits[i], digits[i]);
        }
    }

    [Fact]
    public static void DigitArray_Enumerate_WhatGoesInMustComeOut()
    {
        var digits = new Fractional.DigitArray(s_expectedDigits.Count, s_expectedDigits.Max() + 1);

        digits.Fill(s_expectedDigits);

        Assert.Equal(s_expectedDigits, digits);
    }

    static IAsyncEnumerable<byte> MakeAsyncEnum(IEnumerable<int> enumerable) =>
        enumerable.Select(Convert.ToByte).ToAsyncEnumerable();
}
