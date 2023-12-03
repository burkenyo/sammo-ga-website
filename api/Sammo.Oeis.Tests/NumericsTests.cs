// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Numerics;
namespace Sammo.Oeis.Tests;

public static class NumericsTests
{
    static void AssertFractional<T>(Fractional value, int radix, string digits) where T : Fractional
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

    [Fact]
    public static void BigDecimalFactories_ValidInput_Works()
    {
        AssertFractional<BigDecimal>(BigDecimal.Create([3, 7, 8], 2),
            radix: 10, digits: "37:8");
        AssertFractional<BigDecimal>(BigDecimal.FromDecimal(89.6000m),
            radix: 10, digits: "89:600_0");
        AssertFractional<BigDecimal>(BigDecimal.FromDouble(23.3125),
            radix: 10, digits: "23:3");
        AssertFractional<BigDecimal>(BigDecimal.FromFractional(BigDecimal.FromDouble(23.3125)),
            radix: 10, digits: "23:3");
        AssertFractional<BigDecimal>(BigDecimal.FromInteger(16),
            radix: 10, digits: "16:");
        AssertFractional<BigDecimal>(BigDecimal.FromInteger(16_700_000_000_000_000_000UL),
            radix: 10, digits: "16_700_000_000_000_000_000:");
        AssertFractional<BigDecimal>(BigDecimal.FromInteger(BigInteger.One),
            radix: 10, digits: "1:");
        AssertFractional<BigDecimal>(BigDecimal.FromRatio(400, 8_361_293, 20),
            radix: 10, digits: ":000_047_839_490_853_866_74");
    }

    [Fact]
    public static void DozenalFactories_ValidInput_Works()
    {
        AssertFractional<Dozenal>(Dozenal.Create([3, 7, 8], 2),
            radix: 12, digits: "37:8");
        AssertFractional<Dozenal>(Dozenal.FromDecimal(89.6000m),
            radix: 12, digits: "75:724");
        AssertFractional<Dozenal>(Dozenal.FromDouble(23.3125),
            radix: 12, digits: "1B:3");
        AssertFractional<Dozenal>(Dozenal.FromFractional(Dozenal.FromDouble(23.3125)),
            radix: 12, digits: "1B:3");
        AssertFractional<Dozenal>(Dozenal.FromInteger(16L),
            radix: 12, "14:");
        AssertFractional<Dozenal>(Dozenal.FromInteger(16_700_000_000_000_000_000UL),
            radix: 12, digits: "763_B08_175_142_4B7_A28:");
        AssertFractional<Dozenal>(Dozenal.FromInteger(BigInteger.One),
            radix: 12, "1:");
        AssertFractional<Dozenal>(Dozenal.FromRatio(400, 8_361_293, 20),
            radix: 12, digits: ":000_0BA_A21_321_A1A_903_76");
    }

    [Fact]
    public static void BigDecimalFactories_Garbage_Throws()
    {
        // digit out-of-range
        Assert.Throws<InvalidOperationException>(() => BigDecimal.Create([3, 7, 18], 2));

        //negative numbers disallowed
        Assert.Throws<ArgumentOutOfRangeException>(() => BigDecimal.FromDecimal(-89.6m));
        Assert.Throws<ArgumentOutOfRangeException>(() => BigDecimal.FromDouble(-23.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => BigDecimal.FromInteger(-6_700_000_000_000_000_000));
        Assert.Throws<ArgumentOutOfRangeException>(() => BigDecimal.FromInteger(BigInteger.MinusOne));

        // mismatched sign
        Assert.Throws<ArgumentOutOfRangeException>(() => BigDecimal.FromRatio(-400, 8_361_293, 20));

        // 0 denominator
        Assert.Throws<ArgumentOutOfRangeException>(() => BigDecimal.FromRatio(718, 0, 20));
    }

    [Fact]
    public static void DozenalFactories_Garbage_Throws()
    {
        // digit out-of-range
        Assert.Throws<InvalidOperationException>(() => Dozenal.Create([3, 7, 18], 2));

        //negative numbers disallowed
        Assert.Throws<ArgumentOutOfRangeException>(() => Dozenal.FromDecimal(-89.6m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Dozenal.FromDouble(-23.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => Dozenal.FromInteger(-6_700_000_000_000_000_000));
        Assert.Throws<ArgumentOutOfRangeException>(() => Dozenal.FromInteger(BigInteger.MinusOne));

        // mismatched sign
        Assert.Throws<ArgumentOutOfRangeException>(() => Dozenal.FromRatio(-400, 8_361_293, 20));

        // 0 denominator
        Assert.Throws<ArgumentOutOfRangeException>(() => Dozenal.FromRatio(718, 0, 20));
    }

    [Fact]
    public static void BigDecimalParse_ValidString_MatchesExpected()
    {
        AssertFractional<BigDecimal>(BigDecimal.Parse("00000.0123"),
            radix: 10, digits: ":0123");
        AssertFractional<BigDecimal>(BigDecimal.Parse(".0123"),
            radix: 10, digits: ":0123");
        AssertFractional<BigDecimal>(BigDecimal.Parse("4.50"),
            radix: 10, digits: "4:50");
        AssertFractional<BigDecimal>(BigDecimal.Parse("06.7"),
            radix: 10, digits: "6:7");
        AssertFractional<BigDecimal>(BigDecimal.Parse("89"),
            radix: 10, digits: "89:");
        AssertFractional<BigDecimal>(BigDecimal.Parse("0089."),
            radix: 10, digits: "89:");
    }

    [Fact]
    public static void DozenalParse_ValidString_MatchesExpected()
    {
        AssertFractional<Dozenal>(Dozenal.Parse("00000;0123"),
            radix: 12, digits: ":0123");
        AssertFractional<Dozenal>(Dozenal.Parse(";045"),
            radix: 12, digits: ":045");
        AssertFractional<Dozenal>(Dozenal.Parse("6;70"),
            radix: 12, digits: "6:70");
        AssertFractional<Dozenal>(Dozenal.Parse("08;9"),
            radix: 12, digits: "8:9");
        AssertFractional<Dozenal>(Dozenal.Parse("XE"),
            radix: 12, digits: "AB:");
        AssertFractional<Dozenal>(Dozenal.Parse("00XE;"),
            radix: 12, digits: "AB:");
    }

    [Fact]
    public static void DecimalToString_Default_MatchesExpected()
    {
        Assert.Equal("80.2378", BigDecimal.Parse("80.2378").ToString());
        Assert.Equal("0.2378", BigDecimal.Parse("0.2378").ToString());
        Assert.Equal("802358", BigDecimal.Parse("802358").ToString());
    }

    [Fact]
    public static void DozenalToString_Default_MatchesExpected()
    {
        Assert.Equal("80;2378", Dozenal.Parse("80;2378").ToString());
        Assert.Equal("0;2378", Dozenal.Parse("0;2378").ToString());
        Assert.Equal("8023X8", Dozenal.Parse("8023X8").ToString());
    }

    [Fact]
    public static void DigitArrayCtor_BadArgs_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fractional.DigitArray(-1, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fractional.DigitArray(9, Fractional.MinRadix - 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fractional.DigitArray(9, Fractional.MaxRadix + 1));
    }

    [Fact]
    public static void DigitArray_GetCountAndRadix_Match()
    {
        var digits = new Fractional.DigitArray(3, 10);

        Assert.Equal(3, digits.Count);
        Assert.Equal(10, digits.Radix);
    }

    [Fact]
    public static void DigitArray_Fill_FillCountMatches()
    {
        var digits0 = new Fractional.DigitArray(3, 5);
        var filled0 = digits0.Fill([]);

        Assert.Equal(0, filled0);


        var digits1 = new Fractional.DigitArray(3, 5);
        var filled1 = digits1.Fill([4]);

        Assert.Equal(1, filled1);

        var digits2 = new Fractional.DigitArray(3, 7);
        var filled2 = digits2.Fill([6, 4]);

        Assert.Equal(2, filled2);

        var digits3 = new Fractional.DigitArray(3, 8);
        var filled3 = digits3.Fill([6, 4, 7]);

        Assert.Equal(3, filled3);

        var digits4 = new Fractional.DigitArray(3, 10);
        var filled4 = digits4.Fill([6, 4, 9, 7]);

        Assert.Equal(3, filled4);

        var digits5 = new Fractional.DigitArray(30, Fractional.MaxRadix);
        var filled5 = digits5.Fill([6, 4, 9, 7, 6, 4, 9, 7, 6, 4, 9, 7]);

        Assert.Equal(12, filled5);

        var digits6 = new Fractional.DigitArray(0, 2);
        var filled6 = digits6.Fill([6, 4, 9, 7]);

        Assert.Equal(0, filled6);

        var digits7 = new Fractional.DigitArray(1, 2);
        var filled7 = digits7.Fill([]);

        Assert.Equal(0, filled7);
    }

    [Fact]
    public static async Task DigitArray_FillAsync_FillCountMatches()
    {
        var digits0 = new Fractional.DigitArray(3, 5);
        var filled0 = await digits0.FillAsync(AsyncEnumerable.Empty<byte>());

        Assert.Equal(0, filled0);

        var digits1 = new Fractional.DigitArray(3, 5);
        var filled1 = await digits1.FillAsync(MakeAsyncEnum([4]));

        Assert.Equal(1, filled1);

        var digits2 = new Fractional.DigitArray(3, 7);
        var filled2 = await digits2.FillAsync(MakeAsyncEnum([6, 4]));

        Assert.Equal(2, filled2);

        var digits3 = new Fractional.DigitArray(3, 8);
        var filled3 = await digits3.FillAsync(MakeAsyncEnum([6, 4, 7]));

        Assert.Equal(3, filled3);

        var digits4 = new Fractional.DigitArray(3, 10);
        var filled4 = await digits4.FillAsync(MakeAsyncEnum([6, 4, 9, 7]));

        Assert.Equal(3, filled4);

        var digits5 = new Fractional.DigitArray(30, Fractional.MaxRadix);
        var filled5 = await digits5.FillAsync(MakeAsyncEnum([6, 4, 9, 7, 6, 4, 9, 7, 6, 4, 9, 7]));

        Assert.Equal(12, filled5);

        var digits6 = new Fractional.DigitArray(0, 2);
        var filled6 = await digits6.FillAsync(MakeAsyncEnum([6, 4, 9, 7]));

        Assert.Equal(0, filled6);

        var digits7 = new Fractional.DigitArray(1, 2);
        var filled7 = await digits7.FillAsync(AsyncEnumerable.Empty<byte>());

        Assert.Equal(0, filled7);
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

    [Fact]
    public static void DigitArray_GetByInvalidIndex_Throws()
    {
        var digits = new Fractional.DigitArray(4, 2);

        Assert.Throws<IndexOutOfRangeException>(() => digits[-1]);

        Assert.Throws<IndexOutOfRangeException>(() => digits[4]);
    }

    [Fact]
    public static void DigitArray_GetByValidIndex_MatchesExpected()
    {
        var digits = new Fractional.DigitArray(20, 80);

        byte[] expectedDigits =
        [
            3, 26, 7, 0, 79,
            56, 38, 1, 15, 32,
            37, 40, 45, 6, 72,
            4, 9, 32, 27, 50
        ];

        digits.Fill(expectedDigits);

        for (var i = 0; i < expectedDigits.Length; i++)
        {
            Assert.Equal(expectedDigits[i], digits[i]);
        }
    }

    [Fact]
    public static void DigitArray_Enumerate_WhatGoesInMustComeOut()
    {
        var digits = new Fractional.DigitArray(20, 80);

        byte[] expectedDigits =
        [
            3, 26, 7, 0, 79,
            56, 38, 1, 15, 32,
            37, 40, 45, 6, 72,
            4, 9, 32, 27, 50
        ];

        digits.Fill(expectedDigits);

        Assert.Equal(expectedDigits, digits);
    }

    // helper to support type inference with collection initializer
    static IAsyncEnumerable<byte> MakeAsyncEnum(IEnumerable<byte> enumerable) =>
        enumerable.ToAsyncEnumerable();
}
