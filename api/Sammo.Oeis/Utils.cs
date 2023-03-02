// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Numerics;
using System.Diagnostics;

namespace Sammo.Oeis;

struct RentedArray<T> : IDisposable where T : unmanaged
{
    readonly static ArrayPool<T> s_pool = ArrayPool<T>.Shared;

    readonly T[] _array;

    bool _disposed = false;

    readonly public T[] Array
    {
        get
        {
            CheckDisposed();

            return _array;
        }
    }

    public RentedArray(int size)
    {
        _array = s_pool.Rent(size);
    }

    [DebuggerStepThrough]
    readonly void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(null, "Array has already been returned to the pool!");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            s_pool.Return(_array ?? System.Array.Empty<T>());
        }

        _disposed = true;
    }
}

/// <summary>
/// Provides helper method for working with buffers whose size is unknown until runtime
/// </summary>
static class BufferUtils
{
    /// <summary>
    /// The maximum number of bytes that methods which use buffers should attempt to allocate on the stack
    /// </summary>
    public const int MaxStackAllocBytes = 360;

    /// <summary>
    /// The maximum number of chars that methods which use buffers should attempt to allocate on the stack
    /// </summary>
    public const int MaxStackAllocChars = MaxStackAllocBytes / sizeof(char);

    /// <summary>
    /// Represents a method that uses a provided buffer to perform work
    /// </summary>
    /// <typeparam name="TItem">The type of items in the provided buffer</typeparam>
    /// <param name="buffer">The buffer provided to the delegate</param>
    public delegate void BufferAction<TItem>(Span<TItem> buffer) where TItem : unmanaged;

    /// <summary>
    /// Represent a method that uses a provided buffer to perform work and returns a result
    /// </summary>
    /// <typeparam name="TItem">The type of items in the provided buffer</typeparam>
    /// <typeparam name="TResult">The type of object returned by the delegate</typeparam>
    /// <param name="buffer">The buffer provided to the delegate</param>
    public delegate TResult BufferFunc<TItem, TResult>(Span<TItem> buffer) where TItem : unmanaged;

    public static int MaxStackAlloc<T>() where T : unmanaged =>
        MaxStackAllocBytes / Unsafe.SizeOf<T>();

    // <summary>
    /// Calls the given <see cref="BufferAction{TItem}" /> delegate with a provided buffer.
    /// The buffer is allocated on the stack if the necessary size of the buffer is
    /// less than or equal to <see cref="MaxStackAllocBytes" />. Otherwise, the buffer is rented on the heap
    /// and returned when this method returns.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the provided buffer</typeparam>
    /// <param name="bufferLength">The number of items in the provided buffer</param>
    /// <param name="bufferAction">The delegate that performs work using the provided buffer/param>
    public static void CallWithBuffer<TItem>(int bufferLength, BufferAction<TItem> bufferAction)
        where TItem : unmanaged
    {
        if (bufferLength <= MaxStackAlloc<TItem>())
        {
            Span<TItem> buffer = stackalloc TItem[bufferLength];

            bufferAction(buffer);
        }
        else
        {
            using var rented = new RentedArray<TItem>(bufferLength);

            bufferAction(rented.Array.AsSpan(0, bufferLength));
        }
    }

    // <summary>
    /// Calls the given <see cref="BufferFunc{TItem, TResult}" /> delegate with a provided buffer.
    /// The buffer is allocated on the stack if the necessary size of the buffer is
    /// less than or equal to <see cref="MaxStackAllocBytes" />. Otherwise, the buffer is rented on the heap
    /// and returned when this method returns.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the provided buffer</typeparam>
    /// <typeparam name="TResult">The type of object returned by the delegate</typeparam>
    /// <param name="bufferLength">The number of items in the provided buffer</param>
    /// <param name="bufferFunc">The delegate that performs work using the provided buffer and returns a result</param>
    /// <returns>The result of the <see cref="BufferFunc{TItem, TResult}" /> delegate</returns>
    public static TResult CallWithBuffer<TItem, TResult>(int bufferLength, BufferFunc<TItem, TResult> bufferFunc)
        where TItem : unmanaged
    {
        if (bufferLength <= MaxStackAlloc<TItem>())
        {
            Span<TItem> buffer = stackalloc TItem[bufferLength];

            return bufferFunc(buffer);
        }
        else
        {
            using var rented = new RentedArray<TItem>(bufferLength);

            return bufferFunc(rented.Array.AsSpan(0, bufferLength));
        }
    }
}

unsafe ref struct StackStringBuilder
{
    fixed char _array[BufferUtils.MaxStackAllocChars];

    public int Position { get; private set; }

    public int Capacity =>
        BufferUtils.MaxStackAllocChars;

    public int RemainingCapacity =>
        Capacity - Position;

    public void Append(ReadOnlySpan<char> value)
    {
        if (RemainingCapacity < value.Length)
        {
            throw BufferExhausted();
        }

        fixed(char* ptr = &_array[Position])
        {
            value.CopyTo(new Span<char>(ptr, RemainingCapacity));
        }

        Position += value.Length;
    }

    public void Append(char value)
    {
        if (RemainingCapacity == 0)
        {
            throw BufferExhausted();
        }

        _array[Position] = value;

        Position++;
    }

    /// <remarks>
    /// This is defined as a generic method to avoid boxing.
    /// </remarks>
    public void Append<T>(T value, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : ISpanFormattable
    {
        fixed (char* ptr = &_array[Position])
        {
            if (!value.TryFormat(new Span<char>(ptr, RemainingCapacity), out var charsWritten, format, provider))
            {
                throw BufferExhausted();
            }

            Position += charsWritten;
        }
    }

    public override string ToString()
    {
        fixed (char* ptr = _array)
        {
            return new ReadOnlySpan<char>(ptr, Position).ToString();
        }
    }

    static InvalidOperationException BufferExhausted() =>
        new InvalidOperationException("Buffer is exhausted!");
}

static class ExceptionExtensions
{
    public static bool IsSystemTextJsonException(this Exception ex) =>
        ex is JsonException
            || (ex.Source is not null && ex.Source.StartsWith("System.Text.Json"));
}

[DebuggerStepThrough]
public static class TextReaderExtensions
{
    public static IEnumerable<string> EnumerateLines(this TextReader reader)
    {
        while(reader.ReadLine() is var line && line is not null)
        {
           yield return line;
        }
    }

    public static async IAsyncEnumerable<string> EnumerateLinesAsync(this TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while(await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            yield return line;

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}

static class NumberUtil
{
    public static int GetShortestBitLength(uint value) =>
        ((IBinaryInteger<uint>) value).GetShortestBitLength();

    public static uint Ceiling(uint dividend, uint divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }

        return dividend == 0 ? 0 : (dividend - 1) / divisor + 1;
    }
}
