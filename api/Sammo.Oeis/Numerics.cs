using System.Collections;
using System.Diagnostics;
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
        private bool _readOnly;
        readonly ulong[] _blocks;
        readonly int _bitsPerDigit;
        readonly int _digitsPerBlock;
        readonly ulong _mask;

        public int Count { get; }
        public int Radix { get; }

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
                // In this case, don’t bother with initializing the blocks array since it will never be used.
                // However, all access to the array must be preceded by a check against count
                _blocks = null!;

                return;
            }

            _bitsPerDigit = NumberUtil.GetShortestBitLength((uint)radix - 1);
            _digitsPerBlock = 64 / _bitsPerDigit;
            Count = count;

            // Shift happens AFTER subtraction, so these parens are mandatory
            _mask = (1ul << _bitsPerDigit) - 1;

            var blockCount = NumberUtil.Ceiling((uint)count, (uint)_digitsPerBlock);
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
                    break;
                default:
                    var i = 0;
                    // when _count == 0, this loop will terminate immediately
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

                    _readOnly = true;

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

                    _readOnly = true;

                    return k;
            }
        }

        int FillNone()
        {
            _readOnly = true;

            return 0;
        }

        int FillSingle(byte digit)
        {
            CheckValue(digit);

            _blocks[0] = digit;
            _readOnly = true;

            return 1;
        }

        [DebuggerStepThrough]
        void CheckReadOnly()
        {
            if (_readOnly)
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

        public bool Equals(DigitArray? other) =>
            Count == other?.Count && Radix == other.Radix
                && _blocks.AsSpan().SequenceEqual(other._blocks);

        public override bool Equals(object? obj) =>
            obj is DigitArray other && Equals(other);

        public static bool operator ==(DigitArray? left, DigitArray? right) =>
            left is null && right is null || left?.Equals(right) is true;

        public static bool operator !=(DigitArray? left, DigitArray? right) =>
            !(left == right);

        public override int GetHashCode() =>
            HashCode.Combine(Radix, Count, _blocks.Length, _blocks[0]);
    }

    const char s_defaultFractionalSeparator = ';';

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
        var found = false;
        var stack = new StackTrace();
        for (var i = 0; i < stack.FrameCount; i ++)
        {
            var method = stack.GetFrame(i)!.GetMethod()!;

            if (method.DeclaringType == typeof(Fractional) && method.Name == nameof(CreateInternal))
            {
                found = true;
                break;
            }
        }

        Debug.Assert(found, $"Constructor must be called via {nameof(CreateInternal)}!");
#endif

        _digits = digits;
        Offset = offset;
    }

    static Fractional CreateInternal(DigitArray digits, int offset, int radix)
    {
        Debug.Assert(digits.Radix == radix, $"{nameof(DigitArray)} radix must match radix argument!");

        return radix switch
        {
            BigDecimal.Radix =>
                new BigDecimal(digits, offset),
            Dozenal.Radix =>
                new Dozenal(digits, offset),
            _ =>
                new BigDecimal(digits, offset)
        };
    }

    static Fractional Zero(int radix) =>
        CreateInternal(DigitArray.Zero(radix), 0, radix);

    static Fractional One(int radix) =>
        CreateInternal(DigitArray.One(radix), 1, radix);

    public static Fractional Create(IReadOnlyList<byte> digits, int offset, int radix)
    {
        if (offset < 0 || offset > digits.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (digits.Count == 0)
        {
            return Zero(radix);
        }
        if (digits.Count == 1 && offset == 1)
        {
            switch (digits[0])
            {
                case 0:
                    return Zero(radix);
                case 1:
                    return One(radix);
            }
        }

        var leadingZeroCount = digits.Take(offset).TakeWhile(d => d == 0).Count();

        if (leadingZeroCount == 0 && digits is DigitArray digitArray && digitArray.Radix == radix)
        {
            return CreateInternal(digitArray, offset, radix);
        }

        var copyDigitArray = new DigitArray(digits.Count - leadingZeroCount, radix);
        copyDigitArray.Fill(digits.Skip(leadingZeroCount));

        return CreateInternal(copyDigitArray, offset - leadingZeroCount, radix);
    }

    static Fractional FromParts(BigInteger value, int fromRadix, int toRadix, int numFromRadixFracDigits)
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

        var toRadixAsBigInt = (BigInteger)toRadix;

        IEnumerable<byte> getDigitsInToRadix()
        {
            if (intPart > 0)
            {
                if (intPart < toRadixAsBigInt)
                {
                    yield return (byte) intPart;
                }
                else
                {
                    var num = intPart;
                    using var rented = new RentedArray<byte>(numToRadixIntDigits);
                    var intDigits = rented.Array;

                    // Integer-part digits get filled in reverse.
                    for (var i = intDigits.Count - 1; i >= 0; i--)
                    {
                        num = BigInteger.DivRem(num, toRadixAsBigInt, out var rem);
                        intDigits[i] = (byte)rem;
                    }

                    foreach(var digit in intDigits)
                    {
                        yield return digit;
                    }
                }
            }

            if (numToRadixFracDigits > 0)
            {
                var num = fracPart * toRadixAsBigInt;

                var div = BigInteger.GreatestCommonDivisor(num, den);

                num /= div;
                den /= div;

                for (var i = numToRadixIntDigits; i < numToRadixIntDigits + numToRadixFracDigits; i++)
                {
                    yield return (byte)BigInteger.DivRem(num, den, out num);
                    num *= toRadixAsBigInt;
                }
            }
        }

        digits.Fill(getDigitsInToRadix());

        return CreateInternal(digits, numToRadixIntDigits, toRadix);
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
                FromParts(value, 0, radix, 0)
        };

    public static Fractional FromInteger(ulong value, int radix) =>
        value switch
        {
            0 =>
                Zero(radix),
            1 =>
                One(radix),
            _ =>
                FromParts(value, 0, radix, 0)
        };

    public static Fractional FromInteger(BigInteger value, int radix)
    {
        // BigIntegers can’t be compile-time constants, so no gains from pattern matching
        if (value < BigInteger.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (value == BigInteger.Zero)
        {
            return Zero(radix);
        }
        else if (value == BigInteger.One)
        {
            return One(radix);
        }

        // set fromRadix as zero in call. Since there are no fractional digits, it should go unused.
        return FromParts(value, 0, radix, 0);
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

        return FromParts(valueAsBigInt, 2, radix, numFracDigits);
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

        return FromParts(new BigInteger(value), 10, radix, scale);
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

        return FromParts(valueAsBigInt, value.Radix, radix, valueDigitCount - value.Offset);
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

        return CreateInternal(digits, offset, radix);
    }

    public override string ToString() =>
        ToString();

    public string ToString(string? digitMap, char fractionalSeparator = s_defaultFractionalSeparator)
    {
        const int INT_PART_ONLY = 0;
        const int FRAC_PART_ONLY = 1;
        const int INT_PART_AND_FRAC_PART = 2;

        if (digitMap is null)
        {
            if (Radix > 36)
            {
                throw new ArgumentNullException(nameof(digitMap));
            }
        }
        else if (digitMap.Length != Radix)
        {
            throw new ArgumentException("Length of digit map must match radix!", nameof(digitMap));
        }
        else
        {
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

        Func<byte, char> getDigit = digitMap is null
            ? static d => (char)(d <= 9 ? '0' + d : 'A' - 10 + d)
            : d => digitMap[d];

        switch(_digits.Count)
        {
            case 0:
                return getDigit(0).ToString();
            case 1:
                return getDigit(Digits[0]).ToString();
        }

        int bufferSize;
        int style;
        if (Offset == 0)
        {
            bufferSize = _digits.Count + 2;
            style = FRAC_PART_ONLY;
        }
        else if (Offset == _digits.Count)
        {
            bufferSize = _digits.Count;
            style = INT_PART_ONLY;
        }
        else
        {
            bufferSize = _digits.Count + 1;
            style = INT_PART_AND_FRAC_PART;
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

            // enumerating through the digits is more efficient than random-access
            // when using DigitArray
            foreach (var digit in _digits)
            {
                // it doesn't appear we actually need to check for style == INT_PART_AND_FRAC_PART,
                // but leave it in so the intent is clear
                if (i == Offset && style == INT_PART_AND_FRAC_PART)
                {
                    buffer[i++] = fractionalSeparator;
                }
                buffer[i++] = getDigit(digit);
            }

            return new String(buffer);
        });
    }

    public bool Equals(Fractional? other) =>
        _digits == other?._digits && Offset == other.Offset;


    public override bool Equals(object? obj) =>
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
    const string s_DigitMatcherRegexString = @"^0*([0-9]*)(?:[.,]([0-9]*))?$";

    #if NET7_0
        [GeneratedRegex(s_DigitMatcherRegexString)]
        private static partial Regex GetDigitMatcherRegex();
    #else
        static readonly Regex s_digitMatcherRegex = new(s_DigitMatcherRegexString);

        static Regex GetDigitMatcherRegex() =>
            s_digitMatcherRegex;
    #endif

    readonly static char s_decimalSeparator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator[0];

    new public const int Radix = 10;

    internal BigDecimal(DigitArray digits, int offset) : base(digits, offset) { }

    public static BigDecimal Create(IReadOnlyList<byte> digits, int offset) =>
        (BigDecimal) Create(digits, offset, Radix);

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
        ToString(null, fractionalSeparator: s_decimalSeparator);
}

public partial class Dozenal : Fractional
{
    const string s_digitMap = "0123456789XE";

    // The semicolon in the regex string purposefully matches the default fractional separator
    // .NET const semantics prevent concatenating a single character into a built string
    const string s_digitMatcherRegexString = @"^0*([" + s_digitMap + "]*)(?:;([" + s_digitMap + "]*))?$";
#if NET7_0
    [GeneratedRegex(s_digitMatcherRegexString)]
    private static partial Regex GetDigitMatcherRegex();
#else
    static readonly Regex s_digitMatcherRegex = new(s_digitMatcherRegexString);

    static Regex GetDigitMatcherRegex() =>
        s_digitMatcherRegex;
#endif

    new public const int Radix = 12;

    internal Dozenal(DigitArray digits, int offset) : base(digits, offset) { }

    public static Dozenal Create(IReadOnlyList<byte> digits, int offset) =>
        (Dozenal) Create(digits, offset, Radix);

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
        => ToString(s_digitMap);
}