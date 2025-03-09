// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

namespace Sammo.Oeis.Tests;

public static class UtilsTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public static void StackStringBuilder_Exhausted_AppendEmptyOk(string? emptyIshValue)
    {
        StackStringBuilder builder = default;

        // fill up the builder
        builder.Append(new String('\0', builder.RemainingCapacity));

        Assert.Equal(0, builder.RemainingCapacity);

        builder.Append(emptyIshValue);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData('p')]
    [InlineData(90)]
    public static void StackStringBuilder_Exhausted_AppendsThrow(object value)
    {
        StackStringBuilder builder = default;

        // fill up the builder
        builder.Append(new String('\0', builder.RemainingCapacity));

        Assert.Equal(0, builder.RemainingCapacity);

        // not using Assert.Throws because we have a ref struct that cannot be captured in the lambda it requires
        try
        {
            switch (value)
            {
                case string s:
                    builder.Append(s);
                    break;
                case char c:
                    builder.Append(c);
                    break;
                default:
                    builder.Append((ISpanFormattable) value);
                    break;
            }

            Assert.Fail("Exception not thrown as expected!");
        }
        catch (InvalidOperationException) { }
    }

    [Fact]
    public static void StackStringBuilder_Append_ValuesAppended()
    {
        StackStringBuilder builder = new();
        var remainingCapacity = builder.RemainingCapacity;

        Assert.True(remainingCapacity > 0);

        builder.Append(90);

        Assert.Equal(remainingCapacity -= 90.ToString().Length, builder.RemainingCapacity);

        builder.Append("foo");

        Assert.Equal(remainingCapacity -= "foo".Length, builder.RemainingCapacity);

        builder.Append('#');

        Assert.Equal(--remainingCapacity, builder.RemainingCapacity);

        builder.Append(new DateTime(1965, 8, 23), "yyyy-MM-dd");

        Assert.Equal(remainingCapacity -= "yyyy-MM-dd".Length, builder.RemainingCapacity);

        Assert.Equal("90foo#1965-08-23", builder.ToString());
    }
}
