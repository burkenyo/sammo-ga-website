// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Sammo.Oeis;

/// <summary>
/// Represents a number in any radix between 2 and <see cref="Byte.MaxValue" /> + 1
/// with an integer part and and a fractional part of any precision.
/// </summary>
public partial class Fractional : IEquatable<Fractional>
{
    /// <summary>
    /// A collection that efficiently stores the digits of a fractional number.
    /// </summary>
    internal class DigitArray : IReadOnlyList<byte>, IEquatable<DigitArray>
    {
        readonly ulong[] _blocks;
        readonly int _bitsPerDigit;
        readonly int _digitsPerBlock;
        readonly ulong _mask;

        public int Count { get; }
        public int Radix { get; }

        public bool ReadOnly { get; private set; }

        public static DigitArray Zero(int radix) =>
            new DigitArray(0, radix);

        public static DigitArray One(int radix)
        {
            var digitArray = new DigitArray(1, radix);
            digitArray._blocks[0] = 1;
            return digitArray;
        }

        public DigitArray(int count, int radix)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (radix < MinRadix || radix > MaxRadix)
            {
                throw new ArgumentOutOfRangeException(nameof(radix));
            }

            // Must set radix at least before bailing
            Radix = radix;

            if (count == 0)
            {
                _blocks = Array.Empty<ulong>();

                return;
            }

            _bitsPerDigit = NumberUtil.GetShortestBitLength((uint) radix - 1);
            _digitsPerBlock = 64 / _bitsPerDigit;
            Count = count;

            // Shift happens AFTER subtraction, so these parens are mandatory
            _mask = (1ul << _bitsPerDigit) - 1;

            var blockCount = NumberUtil.Ceiling((uint) count, (uint) _digitsPerBlock);
            _blocks = new ulong[blockCount];
        }

        public byte this[int index]
        {
            get
            {
                CheckIndex(index);

                var block_num = Math.DivRem(index, _digitsPerBlock, out var digit_num);
                return (byte)(_blocks[block_num] >> (digit_num * _bitsPerDigit) & _mask);
            }
        }

        public IEnumerator<byte> GetEnumerator()
        {
            switch (Count)
            {
                case 0:
                    yield break;
                case 1:
                    yield return (byte) _blocks[0];
                    yield break;
                default:
                    var i = 0;
                    for (var j = 0; j < _blocks.Length; j++)
                    {
                        var block = _blocks[j];
                        for (var k = 0; k < _digitsPerBlock; k++)
                        {
                            yield return (byte)(block & _mask);

                            if (++i == Count)
                            {
                                yield break;
                            }

                            block >>= _bitsPerDigit;
                        }
                    }
                    // never hit but required by compiler
                    break;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public int Fill(IEnumerable<byte> filler)
        {
            CheckReadOnly();

            switch (Count)
            {
                case 0:
                    return FillNone();
                case 1:
                    // dummy foreach to grab the first (if any) element
                    foreach (var digit in filler)
                    {
                        return FillSingle(digit);
                    }

                    return FillNone();
                default:
                    var i = 0; // digits in block
                    var j = 0; // block number
                    var k = 0; // total number
                    var block = 0ul;

                    foreach (var digit in filler)
                    {
                        CheckValue(digit);

                        block += (ulong)digit << (i * _bitsPerDigit);

                        if (++i == _digitsPerBlock)
                        {
                            _blocks[j++] = block;
                            i = 0;
                            block = 0;
                        }

                        if (++k == Count)
                        {
                            break;
                        }
                    }

                    if (i > 0)
                    {
                        // we are at a partial block
                        _blocks[j] = block;
                    }

                    ReadOnly = true;

                    return k;
            }
        }

        public async Task<int> FillAsync(IAsyncEnumerable<byte> filler)
        {
            CheckReadOnly();

            switch (Count)
            {
                case 0:
                    return FillNone();
                case 1:
                    // dummy foreach to grab the first (if any) element
                    await foreach (var digit in filler)
                    {
                        return FillSingle(digit);
                    }

                    return FillNone();
                default:
                    var i = 0; // digits in block
                    var j = 0; // block number
                    var k = 0; // total number
                    var block = 0ul;

                    await foreach (var digit in filler)
                    {
                        CheckValue(digit);

                        block += (ulong) digit << (i * _bitsPerDigit);

                        if (++i == _digitsPerBlock)
                        {
                            _blocks[j++] = block;
                            i = 0;
                            block = 0;
                        }

                        if (++k == Count)
                        {
                            break;
                        }
                    }

                    if (i > 0)
                    {
                        // we are at a partial block
                        _blocks[j] = block;
                    }

                    ReadOnly = true;

                    return k;
            }
        }

        int FillNone()
        {
            ReadOnly = true;

            return 0;
        }

        int FillSingle(byte digit)
        {
            CheckValue(digit);

            _blocks[0] = digit;
            ReadOnly = true;

            return 1;
        }

        [DebuggerStepThrough]
        void CheckReadOnly()
        {
            if (ReadOnly)
            {
                throw new InvalidOperationException("DigitArray is read-only and cannot be modified!");
            }
        }

        [DebuggerStepThrough]
        void CheckValue(byte value)
        {
            // bytes are unsigned and therefore no need to check for less than 0
            if (value >= Radix)
            {
                throw new InvalidOperationException($"Digit value exceeds the maximum for radix {Radix}!");
            }
        }

        [DebuggerStepThrough]
        void CheckIndex(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException();
            }
        }

        public bool Equals([NotNullWhen(true)] DigitArray? other) =>
            Count == other?.Count && Radix == other.Radix
                && _blocks.AsSpan().SequenceEqual(other._blocks);

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is DigitArray other && Equals(other);

        public static bool operator ==(DigitArray? left, DigitArray? right) =>
            left is null && right is null || left?.Equals(right) is true;

        public static bool operator !=(DigitArray? left, DigitArray? right) =>
            !(left == right);

        public override int GetHashCode() =>
            HashCode.Combine(Radix, Count, _blocks.Length == 0 ? 0 : _blocks[0]);
    }

    private protected const char s_defaultFractionalSeparator = ';';

    public const int DefaultMaxDigits = 120;
    public const int MinRadix = 2;
    public const int MaxRadix = Byte.MaxValue + 1;

    readonly DigitArray _digits;

    // pass-through that returns the correct (public) type
    public IReadOnlyList<byte> Digits =>
        _digits;

    public int Offset { get; }

    public int Radix =>
        _digits.Radix;

    /// <summary>
    /// The expectation is that this constructor will only ever be called via <see cref="CreateInternal" />
    /// </summary>
    private protected Fractional(DigitArray digits, int offset)
    {
#if DEBUG
        var stack = new StackTrace();
        var found = Enumerable.Range(0, stack.FrameCount)
            .Select(i => stack.GetFrame(i)!.GetMethod()!)
            .Any(m => m.DeclaringType == typeof(Fractional) && m.Name == nameof(CreateInternal));

        Debug.Assert(found, $"Constructor must be called via {nameof(CreateInternal)}!");
#endif

        _digits = digits;
        Offset = offset;
    }

    static Fractional CreateInternal(DigitArray digits, int offset) =>
        digits.Radix switch
        {
            BigDecimal.Radix =>
                new BigDecimal(digits, offset),
            Dozenal.Radix =>
                new Dozenal(digits, offset),
            _ =>
                new Fractional(digits, offset)
        };

    static Fractional Zero(int radix) =>
        CreateInternal(DigitArray.Zero(radix), 0);

    static Fractional One(int radix) =>
        CreateInternal(DigitArray.One(radix), 1);

    public static Fractional Create(IReadOnlyList<byte> digits, int offset, int radix)
    {
        switch (digits.Count, offset)
        {
            case (0, 0):
                return Zero(radix);
            case (1, 1):
                switch (digits[0])
                {
                    case 0:
                        return Zero(radix);
                    case 1:
                        return One(radix);
                }

                break;

        }

        switch (offset)
        {
            case < 0:
                var leadingZeroCountToPad = -offset;

                var copyDigitArray1 = new DigitArray(digits.Count + leadingZeroCountToPad, radix);
                copyDigitArray1.Fill(Enumerable.Repeat((byte) 0, leadingZeroCountToPad).Concat(digits));

                return CreateInternal(copyDigitArray1, 0);
            case 0:
                // this case is also entered sometimes from case > 0, so always use offset in CreateInternal calls
                if (digits is DigitArray digitArray && digitArray.Radix == radix)
                {
                    return CreateInternal(digitArray, offset);
                }

                var copyDigitArray2 = new DigitArray(digits.Count, radix);
                copyDigitArray2.Fill(digits);

                return CreateInternal(copyDigitArray2, offset);
            case > 0:
                var realCount = digits.Count;
                var leadingZeroCountToTrim = digits.Take(offset).TakeWhile(d => d == 0).Count();
                offset -= leadingZeroCountToTrim;

                if (offset > realCount)
                {
                    realCount = offset;
                }

                if (leadingZeroCountToTrim == 0 && realCount == digits.Count)
                {
                    goto case 0;
                }

                var copyDigitArray3 = new DigitArray(realCount, radix);
                copyDigitArray3.Fill(digits.Skip(leadingZeroCountToTrim));

                return CreateInternal(copyDigitArray3, offset);
        }
    }

    static void Reduce(ref BigInteger num, ref BigInteger den)
    {
        var div = BigInteger.GreatestCommonDivisor(num, den);

        num /= div;
        den /= div;
    }

    static IEnumerable<byte> GetDigitsInRadix(BigInteger intPart, BigInteger fracPart, BigInteger den,
        int radix, int numIntDigits, int numFracDigits)
    {
        var radixAsBigInt = (BigInteger) radix;

        if (intPart > 0)
        {
            if (intPart < radixAsBigInt)
            {
                yield return (byte) intPart;
            }
            else
            {
                var num = intPart;
                using var rented = new RentedArray<byte>(numIntDigits);
                var intDigits = new ArraySegment<byte>(rented.Array, 0, numIntDigits);

                // Integer-part digits get filled in reverse.
                for (var i = intDigits.Count - 1; i >= 0; i--)
                {
                    num = BigInteger.DivRem(num, radixAsBigInt, out var rem);
                    intDigits[i] = (byte)rem;
                }

                foreach(var digit in intDigits)
                {
                    yield return digit;
                }
            }
        }

        if (numFracDigits > 0 & fracPart > 0)
        {
            var num = fracPart * radixAsBigInt;

            Reduce(ref num, ref den);

            for (var i = numIntDigits; i < numIntDigits + numFracDigits; i++)
            {
                yield return (byte)BigInteger.DivRem(num, den, out num);

                if(num == BigInteger.Zero)
                {
                    // there is nothing left to divide out, i.e. this is a terminating fractional
                    yield break;
                }

                num *= radixAsBigInt;
            }
        }
    }

    static Fractional FromRatioInternal(BigInteger num, BigInteger den, int radix, int numFracDigits)
    {
        var intPart = BigInteger.DivRem(num, den, out var fracPart);

        var numIntDigits = intPart == 0
            ? 0
            : (int)(BigInteger.Log(intPart) / Math.Log(radix)) + 1;

        var digits = new DigitArray(numIntDigits + numFracDigits, radix);

        digits.Fill(GetDigitsInRadix(intPart, fracPart, den, radix, numIntDigits, numFracDigits));

        return CreateInternal(digits, digits.Count - numFracDigits);
    }

    public static Fractional FromRatio(long num, long den, int radix, int numFracDigits)
    {
        if (den == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(den));
        }
        // reject opposite signs, allowing for num = 0
        if (Math.Sign(num) + Math.Sign(den) == 0)
        {
            throw new ArgumentException($"{nameof(num)} and {nameof(den)} must have the same sign!)");
        }

        switch (numFracDigits)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(numFracDigits));
            case 0:
                if (num == 0)
                {
                    return Zero(radix);
                }
                if (num == den)
                {
                    return One(radix);
                }

                break;
        }

        if (den < 0)
        {
            num = -num;
            den = -den;
        }

        return FromRatioInternal(num, den, radix, numFracDigits);
    }

    public static Fractional FromRatio(ulong num, ulong den, int radix, int numFracDigits)
    {
        if (den == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(den));
        }

        switch (numFracDigits)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(numFracDigits));
            case 0:
                if (num == 0)
                {
                    return Zero(radix);
                }
                if (num == den)
                {
                    return One(radix);
                }

                break;
        }

        return FromRatioInternal(num, den, radix, numFracDigits);
    }

    public static Fractional FromRatio(BigInteger num, BigInteger den, int radix, int numFracDigits)
    {
        // BigIntegers can’t be compile-time constants, so no gains from pattern matching
        if (den == BigInteger.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(den));
        }
        // reject opposite signs, allowing for num = 0
        if (num.Sign + den.Sign == 0)
        {
            throw new ArgumentException($"{nameof(num)} and {nameof(den)} must have the same sign!)");
        }

        switch (numFracDigits)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(numFracDigits));
            case 0:
                if (num == BigInteger.Zero)
                {
                    return Zero(radix);
                }
                if (num == den)
                {
                    return One(radix);
                }

                break;
        }

        if (den < BigInteger.Zero)
        {
            num = -num;
            den = -den;
        }


        Reduce(ref num, ref den);

        return FromRatioInternal(num, den, radix, numFracDigits);
    }

    public static Fractional FromInteger(long value, int radix) =>
        value switch
        {
            < 0 =>
                throw new ArgumentOutOfRangeException(nameof(value)),
            0 =>
                Zero(radix),
            1 =>
                One(radix),
            _ =>
                FromRatioInternal(value, BigInteger.One, radix, 0)
        };

    public static Fractional FromInteger(ulong value, int radix) =>
        value switch
        {
            0 =>
                Zero(radix),
            1 =>
                One(radix),
            _ =>
                FromRatioInternal(value, BigInteger.One, radix, 0)
        };

    public static Fractional FromInteger(BigInteger value, int radix)
    {
        if(value < BigInteger.One)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }


        if(value == BigInteger.Zero)
        {
            return Zero(radix);
        }
        if(value == BigInteger.One)
        {
            return One(radix);
        }

        return FromRatioInternal(value, BigInteger.One, radix, 0);
    }

    static Fractional FromScaled(BigInteger value, int fromRadix, int toRadix, int numFromRadixFracDigits)
    {
        BigInteger fracPart, intPart, den;

        if (numFromRadixFracDigits == 0)
        {
            den = BigInteger.Zero;
            intPart = value;
            fracPart = BigInteger.Zero;
        }
        else
        {
            den = BigInteger.Pow(fromRadix, numFromRadixFracDigits);
            intPart = BigInteger.DivRem(value, den, out fracPart);
        }

        var numToRadixIntDigits = intPart == 0
            ? 0
            : (int)(BigInteger.Log(intPart) / Math.Log(toRadix)) + 1;
        var numToRadixFracDigits = (int)(numFromRadixFracDigits * Math.Log(fromRadix) / Math.Log(toRadix));

        if (numToRadixIntDigits + numToRadixFracDigits == 0)
        {
            return Zero(toRadix);
        }

        var digits = new DigitArray(numToRadixIntDigits + numToRadixFracDigits, toRadix);

        digits.Fill(GetDigitsInRadix(intPart, fracPart, den, toRadix, numToRadixIntDigits, numToRadixFracDigits));

        return CreateInternal(digits, numToRadixIntDigits);
    }

    public static Fractional FromDouble(double value, int radix)
    {
        if (!Double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        switch (value)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(value));
            case 0:
                return Zero(radix);
            case 1:
                return One(radix);
        }

        var bits = Unsafe.As<double, ulong>(ref value);

        const int NUM_EXP_BITS = 11;
        const ulong EXP_BIT_MASK = (1ul << NUM_EXP_BITS) - 1; // 0x7FF
        const int EXP_BIAS = (1 << NUM_EXP_BITS - 1) - 1; // 1023

        const int NUM_SIG_BITS = 52;
        const ulong SIG_BIT_MASK = (1ul << NUM_SIG_BITS) - 1; // 0xF_FFFF_FFFF_FFFF
        const ulong IMPLICIT_SIG_BIT = 1ul << NUM_SIG_BITS; // Ox10_0000_0000_0000

        var exp = (int) ((bits >> NUM_SIG_BITS) & EXP_BIT_MASK);
        var sig = bits & SIG_BIT_MASK;
        if (exp == 0)
        {
            exp = 1;
        }
        else
        {
            sig |= IMPLICIT_SIG_BIT;
        }

        // un-bias the exponent (1023) and slap it down for the significand bits (52)
        exp -= EXP_BIAS + NUM_SIG_BITS;

        var normalizeBy = BitOperations.TrailingZeroCount(sig);
        sig >>= normalizeBy;
        exp += normalizeBy;

        BigInteger valueAsBigInt;
        int numFracDigits;
        if (exp < 0)
        {
            numFracDigits = -exp;
            valueAsBigInt = sig;
        }
        else
        {
            numFracDigits = 0;
            valueAsBigInt = (BigInteger.One << exp) * sig;
        }

        return FromScaled(valueAsBigInt, 2, radix, numFracDigits);
    }

    public static Fractional FromDecimal(decimal value, int radix)
    {
        switch (value)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(value));
            case 0:
                return Zero(radix);
            case 1:
                return One(radix);
        }

        // get a ref var of the decimal for raw manipulation.
        // This should be ok since it’s sitting on the stack.
        ref var flags = ref Unsafe.As<decimal, int>(ref value);
        // determine the number of fractional digits
        var scale = flags >> 16 & 0xFF;
        // blank out the scale but keep the sign (lowest bit)
        flags &= 0x1;

        return FromScaled(new BigInteger(value), 10, radix, scale);
    }

    public static Fractional FromFractional(Fractional value, int radix)
    {
        if (radix == value.Radix)
        {
            return value;
        }

        BigInteger valueAsBigInt;
        var valueDigitCount = value._digits.Count;

        if (value.Radix == 10)
        {
            // fast path for base-10 numbers using parsing
            // profiling showed this to be 15 - 100 X faster.
            valueAsBigInt = BufferUtils.CallWithBuffer<char, BigInteger>(valueDigitCount, buffer =>
            {
                var i = 0;

                foreach (var digit in value._digits)
                {
                    buffer[i++] = (char)('0' + digit);
                }

                return BigInteger.Parse(buffer);
            });
        }
        else
        {
            valueAsBigInt = BigInteger.Zero;
            var valueRadixAsBigInt = new BigInteger(value.Radix);

            foreach (var digit in value._digits)
            {
                valueAsBigInt = (valueAsBigInt * valueRadixAsBigInt) + digit;
            }
        }

        return FromScaled(valueAsBigInt, value.Radix, radix, valueDigitCount - value.Offset);
    }

    private protected static Fractional ParseInternal(string input, Regex digitMatcher, int radix, string? digitMap)
    {
        var parts = digitMatcher.Match(input);

        // parts.Length == 0 checks for both unsuccessful match (bad input) and empty input (which _does_ match)
        if(parts.Length == 0)
        {
            throw new FormatException("Input string was not in the correct format!");
        }

        var intPart = parts.Groups[1].Value;
        var fracPart = parts.Groups[2].Value;

        if (intPart == "" && fracPart == "")
        {
            return Zero(radix);
        }

        var offset = intPart.Length;

        var digits = new DigitArray(intPart.Length + fracPart.Length, radix);

        Func<char, byte> mapper = digitMap is null
            ? static c => (byte)(c <= '9' ? c - '0' : c - 'A' + 10)
            : c => (byte) digitMap.IndexOf(c);

        digits.Fill(intPart.Concat(fracPart).Select(mapper));

        return CreateInternal(digits, offset);
    }

    public override string ToString() =>
        ToStringInternal(null, s_defaultFractionalSeparator, DefaultMaxDigits);

    public virtual string ToString(int? maxDigits) =>
        ToStringInternal(null, s_defaultFractionalSeparator, maxDigits);

    public string ToString(
        string? digitMap, char fractionalSeparator = s_defaultFractionalSeparator, int? maxDigits = DefaultMaxDigits)
    {
        if (!char.IsPunctuation(fractionalSeparator))
        {
            throw new ArgumentException(
                "Fractional separator must be a punctuation symbol!", nameof(fractionalSeparator));
        }

        if (digitMap is not null)
        {
            if (digitMap.Length != Radix)
            {
                throw new ArgumentException("Length of digit map must match radix!", nameof(digitMap));
            }

            // profiling revealed creating a HashSet is much faster than searching with a regex;
            var set = new HashSet<char>(digitMap.Length);
            foreach (var c in digitMap)
            {
                if (!set.Add(c))
                {
                    throw new ArgumentException("All chars in digit map must be unique!", nameof(digitMap));
                }
            }
        }

        return ToStringInternal(digitMap, fractionalSeparator, maxDigits);
    }

    private protected string ToStringInternal(string? digitMap, char fractionalSeparator, int? maxDigits)
    {
        if (maxDigits < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDigits));
        }

        Func<byte, char> getDigit = digitMap is null
            ? static d => d switch
            {
                <= 9 =>
                    (char)('0' + d),
                <= 35 =>
                    (char)('A' - 10 + d),
                _ =>
                    '?'
            }
            : d => digitMap[d];

        const int INT_PART_ONLY = 0;
        const int FRAC_PART_ONLY = 1;
        const int INT_PART_AND_FRAC_PART = 2;

        switch(_digits.Count)
        {
            case 0:
                return getDigit(0).ToString();
            case 1:
                return getDigit(Digits[0]).ToString();
        }

        var count = maxDigits is null
            ? Digits.Count
            : Math.Min(Digits.Count, (int) maxDigits);

        var needsEllipsis = count < Digits.Count;

        int bufferSize;
        int style;
        if (Offset == 0)
        {
            bufferSize = count + 2;
            style = FRAC_PART_ONLY;
        }
        else if (Offset == _digits.Count || Offset >= count)
        {
            bufferSize = count;
            style = INT_PART_ONLY;
        }
        else
        {
            bufferSize = count + 1;
            style = INT_PART_AND_FRAC_PART;
        }

        if (needsEllipsis)
        {
            bufferSize++;
        }

        return BufferUtils.CallWithBuffer<char, string>(bufferSize, buffer =>
        {
            int i = 0;
            if (style == FRAC_PART_ONLY)
            {
                buffer[0] = getDigit(0);
                buffer[1] = fractionalSeparator;
                i = 2;
            }

            int j = 0;
            // enumerating through the digits is more efficient than random-access
            // when using DigitArray
            foreach (var digit in _digits)
            {
                // it doesn't appear we actually need to check for style == INT_PART_AND_FRAC_PART,
                // but leave it in so the intent is clear
                if (style == INT_PART_AND_FRAC_PART && i == Offset)
                {
                    buffer[i++] = fractionalSeparator;
                }
                buffer[i++] = getDigit(digit);

                if (++j == count)
                {
                    break;
                }
            }

            if (needsEllipsis)
            {
                buffer[i] = '…';
            }

            Debug.Assert(buffer[^1] != '\0', "Buffer was not filled!");

            return new String(buffer);
        });
    }

    public bool Equals([NotNullWhen(true)] Fractional? other) =>
        _digits == other?._digits && Offset == other.Offset;


    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is Fractional other && Equals(other);

    public static bool operator==(Fractional? left, Fractional? right) =>
        left is null && right is null || left?.Equals(right) is true;

    public static bool operator !=(Fractional? left, Fractional? right) =>
        !(left == right);

    public override int GetHashCode() =>
        HashCode.Combine(_digits, Offset);
}

public partial class BigDecimal : Fractional
{
    [GeneratedRegex(@"^0*([0-9]*)(?:[.,]([0-9]*))?$")]
    private static partial Regex GetDigitMatcherRegex();

    readonly static char s_decimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator[0];

    new public const int Radix = 10;

    internal BigDecimal(DigitArray digits, int offset) : base(digits, offset) { }

    public static BigDecimal Create(IReadOnlyList<byte> digits, int offset) =>
        (BigDecimal) Create(digits, offset, Radix);

    public static Fractional FromRatio(BigInteger num, BigInteger den, int numFracDigits) =>
        (BigDecimal) FromRatio(num, den, Radix, numFracDigits);

    public static BigDecimal FromInteger(long value) =>
        (BigDecimal) FromInteger(value, Radix);

    public static BigDecimal FromInteger(ulong value) =>
        (BigDecimal) FromInteger(value, Radix);

    public static BigDecimal FromInteger(BigInteger value) =>
        (BigDecimal) FromInteger(value, Radix);

    public static BigDecimal FromDouble(double value) =>
        (BigDecimal) FromDouble(value, Radix);

    public static BigDecimal FromDecimal(decimal value) =>
        (BigDecimal) FromDecimal(value, Radix);

    public static BigDecimal FromFractional(Fractional value) =>
        (BigDecimal) FromFractional(value, Radix);

    public static BigDecimal Parse(string input) =>
        (BigDecimal) ParseInternal(input, GetDigitMatcherRegex(), Radix, null);

    public override string ToString() =>
        ToStringInternal(null, s_decimalSeparator, DefaultMaxDigits);

    public override string ToString(int? maxDigits) =>
        ToStringInternal(null, s_decimalSeparator, maxDigits);
}

public partial class Dozenal : Fractional
{
    const string s_digitMap = "0123456789XE";

    // The semicolon in the regex string purposefully matches the default fractional separator
    // .NET const semantics prevent concatenating a single character into a built string
    [GeneratedRegex(@"^0*([" + s_digitMap + "]*)(?:;([" + s_digitMap + "]*))?$")]
    private static partial Regex GetDigitMatcherRegex();

    new public const int Radix = 12;

    internal Dozenal(DigitArray digits, int offset) : base(digits, offset) { }

    public static Dozenal Create(IReadOnlyList<byte> digits, int offset) =>
        (Dozenal) Create(digits, offset, Radix);

    public static Fractional FromRatio(BigInteger num, BigInteger den, int numFracDigits) =>
        (Dozenal) FromRatio(num, den, Radix, numFracDigits);

    public static Dozenal FromInteger(long value) =>
        (Dozenal) FromInteger(value, Radix);

    public static Dozenal FromInteger(ulong value) =>
        (Dozenal) FromInteger(value, Radix);

    public static Dozenal FromInteger(BigInteger value) =>
        (Dozenal) FromInteger(value, Radix);

    public static Dozenal FromDouble(double value) =>
        (Dozenal) FromDouble(value, Radix);

    public static Dozenal FromDecimal(decimal value) =>
        (Dozenal) FromDecimal(value, Radix);

    public static Dozenal FromFractional(Fractional value) =>
        (Dozenal) FromFractional(value, Radix);

    public static Dozenal Parse(string input) =>
        (Dozenal) ParseInternal(input, GetDigitMatcherRegex(), Radix, s_digitMap);

    public override string ToString()
        => ToStringInternal(s_digitMap, s_defaultFractionalSeparator, DefaultMaxDigits);

    public override string ToString(int? maxDigits)
        => ToStringInternal(s_digitMap, s_defaultFractionalSeparator, maxDigits);
}