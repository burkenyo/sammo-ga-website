// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Unicode;

namespace Sammo.Oeis;

[DebuggerStepThrough]
public readonly record struct OeisId : IComparable<OeisId>, IEqualityOperators<OeisId, OeisId, bool>,
    ISpanParsable<OeisId>, ISpanFormattable, IUtf8SpanParsable<OeisId>, IUtf8SpanFormattable
{
    public enum ParseOption
    {
        /// <summary>
        /// String must match canonical form of ‘A’ + a positive integer.
        /// </summary>
        Strict,

        /// <summary>
        /// Prefix of ‘A’ is optional.
        /// </summary>
        Lax
    }

    public const int MinValue = 1;

    public const int MaxValue = 999_999_999;

    public OeisId(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, MinValue);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxValue);

        Value = value;
    }

    // the maximum possible length is 10 chars:
    //   • the ‘A’ prefix
    //   • 9 chars for the MaxValue
    public const int MaxStringLength = 10;

    public int Value { get; }

    public override string ToString()
    {

        Span<char> buffer = stackalloc char[MaxStringLength];

        TryFormat(buffer, out var charsWritten);

        return new String(buffer[..charsWritten]);
    }

    public bool TryFormat(Span<char> destination, out int charsWritten) =>
        destination.TryWrite($"A{Value:D6}", out charsWritten);

    public bool TryFormat(Span<byte> destination, out int bytesWritten) =>
        Utf8.TryWrite(destination, $"A{Value:D6}", out bytesWritten);

    internal string GetPaddedValue() =>
        Value.ToString("D6");

    internal bool TryGetPaddedValue(Span<char> destination, out int bytesWritten) =>
        Value.TryFormat(destination, out bytesWritten, "D6");

    internal bool TryGetPaddedValue(Span<byte> destination, out int bytesWritten) =>
        Value.TryFormat(destination, out bytesWritten, "D6");

    public static OeisId Parse(ReadOnlySpan<char> value, ParseOption option = ParseOption.Strict) =>
        TryParse(value, out var oeisId, option)
            ? oeisId
            : throw new FormatException("Input string was not in the correct format!");

    public static bool TryParse(ReadOnlySpan<char> value, out OeisId id, ParseOption option = ParseOption.Strict)
    {
        if (value.Length >= 2 && (value[0] == 'A' || (option == ParseOption.Lax && value[0] == 'a')))
        {
            value = value[1..];
        }
        else if (value.Length == 0 || option == ParseOption.Strict)
        {
            id = default;
            return false;
        }

        if (Int32.TryParse(value, out var intVal) && intVal is >= MinValue and <= MaxValue)
        {
            id = new OeisId(intVal);
            return true;
        }

        id = default;
        return false;
    }

    public static OeisId Parse(ReadOnlySpan<byte> value, ParseOption option = ParseOption.Strict) =>
        TryParse(value, out var oeisId, option)
            ? oeisId
            : throw new FormatException("Input string was not in the correct format!");

    public static bool TryParse(ReadOnlySpan<byte> value, out OeisId id, ParseOption option = ParseOption.Strict)
    {
        if (value.Length >= 2 && (value[0] == 'A' || (option == ParseOption.Lax && value[0] == 'a')))
        {
            value = value[1..];
        }
        else if (value.Length == 0 || option == ParseOption.Strict)
        {
            id = default;
            return false;
        }

        if (Int32.TryParse(value, out var intVal) && intVal is >= MinValue and <= MaxValue)
        {
            id = new OeisId(intVal);
            return true;
        }

        id = default;
        return false;
    }

    public int CompareTo(OeisId other) =>
        Value.CompareTo(other.Value);

    static OeisId IParsable<OeisId>.Parse(string value, IFormatProvider? provider) =>
        Parse(value);

    static bool IParsable<OeisId>.TryParse(
        [NotNullWhen(true)] string? value, IFormatProvider? provider, out OeisId result) =>
        TryParse(value, out result);

    static OeisId ISpanParsable<OeisId>.Parse(ReadOnlySpan<char> value, IFormatProvider? provider) =>
        Parse(value);

    static bool ISpanParsable<OeisId>.TryParse(ReadOnlySpan<char> value, IFormatProvider? provider, out OeisId result) =>
        TryParse(value, out result);

    static OeisId IUtf8SpanParsable<OeisId>.Parse(ReadOnlySpan<byte> value, IFormatProvider? provider) =>
        Parse(value);

    static bool IUtf8SpanParsable<OeisId>.TryParse(ReadOnlySpan<byte> value, IFormatProvider? provider, out OeisId result) =>
        TryParse(value, out result);

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) =>
        ToString();

    bool ISpanFormattable.TryFormat(
        Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        TryFormat(destination, out charsWritten);

    bool IUtf8SpanFormattable.TryFormat(
        Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        TryFormat(destination, out bytesWritten);

    public static explicit operator int(OeisId id) =>
        id.Value;

    public static explicit operator OeisId(int value) =>
        new OeisId(value);
}

public interface IOeisSequence
{
    OeisId Id { get; }

    string Name { get; }
}

public class OeisSequence : IOeisSequence
{
    public OeisId Id { get; }

    public string Name { get; }

    internal int Offset { get; }

    internal OeisSequence(OeisId id, string name, int offset)
    {
        Id = id;
        Name = name;
        Offset = offset;
    }
}

public class StoredOeisExpansionInfo : IOeisSequence
{
    public OeisId Id { get; }

    public string Name { get; }

    public int Radix { get; }

    public string Preview { get; }

    public Uri Uri { get; }

    public StoredOeisExpansionInfo(OeisId id, string name, int radix, string preview, Uri uri)
    {
        Id = id;
        Name = name;
        Radix = radix;
        Preview = preview;
        Uri = uri;
    }
}

public interface IOeisFractionalExpansion : IOeisSequence
{
    Fractional Expansion { get; }
}

public interface IOeisFractionalExpansion<T> : IOeisFractionalExpansion where T : Fractional
{
    new T Expansion { get; }
}

public class OeisDecimalExpansion : IOeisFractionalExpansion<BigDecimal>
{
    public OeisId Id { get; }

    public string Name { get; }

    public BigDecimal Expansion { get; }

    Fractional IOeisFractionalExpansion.Expansion =>
        Expansion;

    internal OeisDecimalExpansion(OeisId id, string name, BigDecimal expansion)
    {
        Id = id;
        Name = name;
        Expansion = expansion;
    }

    internal OeisDozenalExpansion ConvertToDozenal() =>
        new OeisDozenalExpansion(Id, Name, Dozenal.FromFractional(Expansion));
}

public class OeisDozenalExpansion : IOeisFractionalExpansion<Dozenal>
{
    public OeisId Id { get; }

    public string Name { get; }

    public Dozenal Expansion { get; }

    Fractional IOeisFractionalExpansion.Expansion =>
        Expansion;

    internal OeisDozenalExpansion(OeisId id, string name, Dozenal expansion)
    {
        Id = id;
        Name = name;
        Expansion = expansion;
    }
}

class OeisSearchResult
{
    public static readonly OeisSearchResult Empty = new(-1, 0, null);

    public int Index { get; }

    public int TotalCount { get; }

    public OeisSequence? Sequence { get; }

    public OeisSearchResult(int index, int totalCount, OeisSequence? sequence)
    {
        Debug.Assert(totalCount > 0 ^ sequence is null,
            $"{nameof(totalCount)} must be 0 and {nameof(sequence)} must be null "
            + $"or {nameof(totalCount)} must be > 0 and {nameof(sequence)} must be not null!");

        Debug.Assert(index >= 0 && index < totalCount || index == -1 && totalCount == 0,
            $"Invalid {nameof(index)} and {nameof(TotalCount)}!");

        Index = index;
        TotalCount = totalCount;
        Sequence = sequence;
    }
}

public enum ClientExceptionCause
{
    /// <summary>
    /// The sequence was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The sequence cannot be interpreted as a decimal expansion.
    /// </summary>
    InvalidSequence,

    /// <summary>
    /// An error occurred downloading the sequence data or retrieving it from storage.
    /// </summary>
    IOError,

    /// <summary>
    /// The sequence data could not be interpreted in the expected format.
    /// </summary>
    ParseError

}

/// <summary>
/// Indicates a problem occurred while retrieving sequence data from OEIS or storage.
/// </summary>
public class OeisClientException : Exception
{
    public ClientExceptionCause Cause { get; }

    public OeisId? Id { get; }

    OeisClientException(ClientExceptionCause cause, string message, OeisId? id,
        Exception? innerException) : base(message, innerException)
    {
#if DEBUG
        var mustIncludeId = cause is ClientExceptionCause.NotFound or ClientExceptionCause.InvalidSequence;

        Debug.Assert(!mustIncludeId || id?.Value != 0, $"Id must be specified for cause {cause}!");
#endif

        Cause = cause;
        Id = id;
    }

    public static OeisClientException NotFound(string message, OeisId id, Exception? innerException = null) =>
        new OeisClientException(ClientExceptionCause.NotFound, message, id, innerException);

    public static OeisClientException InvalidSequence(string message, OeisId id, Exception? innerException = null) =>
        new OeisClientException(ClientExceptionCause.InvalidSequence, message, id, innerException);

    public static OeisClientException IOError(string message, OeisId id, Exception? innerException = null) =>
        new OeisClientException(ClientExceptionCause.IOError, message, id, innerException);

    public static OeisClientException IOError(string message, Exception? innerException = null) =>
        new OeisClientException(ClientExceptionCause.IOError, message, null, innerException);

    public static OeisClientException ParseError(string message, OeisId id, Exception? innerException = null) =>
        new OeisClientException(ClientExceptionCause.ParseError, message, id, innerException);

    public static OeisClientException ParseError(string message, Exception? innerException = null) =>
        new OeisClientException(ClientExceptionCause.ParseError, message, null, innerException);
}
