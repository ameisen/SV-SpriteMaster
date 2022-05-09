using Microsoft.Toolkit.HighPerformance;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global

namespace SpriteMaster.Types.Spans;

[DebuggerTypeProxy(typeof(PinnedSpanDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
internal readonly ref struct ReadOnlyPinnedSpan<T> {

	internal static ReadOnlyPinnedSpan<T> Empty => new(null);

	internal readonly ReadOnlySpan<T> InnerSpan;

	/// <summary>
	/// Creates a new read-only span over the entirety of the target array.
	/// </summary>
	/// <param name="array">The target array.</param>
	/// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ReadOnlyPinnedSpan(T[]? array) {
		InnerSpan = new(array);
	}

	/// <summary>
	/// Creates a new read-only span over the portion of the target array beginning
	/// at 'start' index and ending at 'end' index (exclusive).
	/// </summary>
	/// <param name="array">The target array.</param>
	/// <param name="start">The index at which to begin the read-only span.</param>
	/// <param name="length">The number of items in the read-only span.</param>
	/// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
	/// <exception cref="System.ArgumentOutOfRangeException">
	/// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;Length).
	/// </exception>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ReadOnlyPinnedSpan(T[] array, int start, int length) {
		InnerSpan = new(array, start, length);
	}

	/// <summary>
	/// Creates a new read-only span over the target unmanaged buffer.  Clearly this
	/// is quite dangerous, because we are creating arbitrarily typed T's
	/// out of a void*-typed block of memory.  And the length is not checked.
	/// But if this creation is correct, then all subsequent uses are correct.
	/// </summary>
	/// <param name="pointer">An unmanaged pointer to memory.</param>
	/// <param name="length">The number of <typeparamref name="T"/> elements the memory contains.</param>
	/// <exception cref="System.ArgumentException">
	/// Thrown when <typeparamref name="T"/> is reference type or contains pointers and hence cannot be stored in unmanaged memory.
	/// </exception>
	/// <exception cref="System.ArgumentOutOfRangeException">
	/// Thrown when the specified <paramref name="length"/> is negative.
	/// </exception>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal unsafe ReadOnlyPinnedSpan(void* pointer, int length) {
		InnerSpan = new(pointer, length);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal unsafe ReadOnlyPinnedSpan(ref T pointer, int length) {
		InnerSpan = new(Unsafe.AsPointer(ref pointer), length);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private ReadOnlyPinnedSpan(ReadOnlySpan<T> span) {
		InnerSpan = span;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private ReadOnlyPinnedSpan(Span<T> span) {
		InnerSpan = span;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ReadOnlyPinnedSpan<T> FromInternal(Span<T> span) => new(span);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ReadOnlyPinnedSpan<T> FromInternal(ReadOnlySpan<T> span) => new(span);

	// Most ripped from Span.cs

	/// <summary>
	/// Returns a reference to specified element of the Span.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	/// <exception cref="System.IndexOutOfRangeException">
	/// Thrown when index less than 0 or index greater than or equal to Length
	/// </exception>
	internal ref readonly T this[int index] {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get => ref InnerSpan[index];
	}

	/// <summary>
	/// The number of items in the span.
	/// </summary>
	internal int Length {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get => InnerSpan.Length;
	}

	/// <summary>
	/// Returns true if Length is 0.
	/// </summary>
	internal bool IsEmpty {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get => InnerSpan.IsEmpty;
	}

	/// <summary>
	/// Returns true if left and right point at the same memory and have the same length.  Note that
	/// this does *not* check to see if the *contents* are equal.
	/// </summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(ReadOnlyPinnedSpan<T> left, ReadOnlySpan<T> right) => left.InnerSpan == right;

	/// <summary>
	/// Returns true if left and right point at the same memory and have the same length.  Note that
	/// this does *not* check to see if the *contents* are equal.
	/// </summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(ReadOnlySpan<T> left, ReadOnlyPinnedSpan<T> right) => left == right.InnerSpan;

	/// <summary>
	/// Returns false if left and right point at the same memory and have the same length.  Note that
	/// this does *not* check to see if the *contents* are equal.
	/// </summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(ReadOnlyPinnedSpan<T> left, ReadOnlySpan<T> right) => !(left == right);

	/// <summary>
	/// Returns false if left and right point at the same memory and have the same length.  Note that
	/// this does *not* check to see if the *contents* are equal.
	/// </summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(ReadOnlySpan<T> left, ReadOnlyPinnedSpan<T> right) => !(left == right);

	/// <summary>
	/// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
	/// <exception cref="System.NotSupportedException">
	/// Always thrown by this method.
	/// </exception>
	/// </summary>
	[Obsolete("Equals() on Span will always throw an exception. Use == instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
	public override bool Equals(object? obj) => InnerSpan.Equals(obj);
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

	/// <summary>
	/// This method is not supported as spans cannot be boxed.
	/// <exception cref="System.NotSupportedException">
	/// Always thrown by this method.
	/// </exception>
	/// </summary>
	[Obsolete("GetHashCode() on Span will always throw an exception.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
	public override int GetHashCode() => InnerSpan.GetHashCode();
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member

	/// <summary>
	/// Defines an implicit conversion of an array to a <see cref="ReadOnlyPinnedSpan{T}"/>
	/// </summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator ReadOnlyPinnedSpan<T>(T[]? array) => new(array);

	/// <summary>
	/// Defines an implicit conversion of a <see cref="ArraySegment{T}"/> to a <see cref="ReadOnlyPinnedSpan{T}"/>
	/// </summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator ReadOnlyPinnedSpan<T>(ArraySegment<T> segment) => new(segment);

	/// <summary>Gets an enumerator for this span.</summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public ReadOnlySpan<T>.Enumerator GetEnumerator() => InnerSpan.GetEnumerator();

	/// <summary>
	/// Returns a reference to the 0th element of the Span. If the Span is empty, returns null reference.
	/// It can be used for pinning and is required to support the use of span within a fixed statement.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ref readonly T GetPinnableReference() => ref InnerSpan.GetPinnableReference();

	[EditorBrowsable(EditorBrowsableState.Never)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ref T GetPinnableReferenceUnsafe() => ref MemoryMarshal.GetReference(InnerSpan);

	/// <summary>
	/// Copies the contents of this span into destination span. If the source
	/// and destinations overlap, this method behaves as if the original values in
	/// a temporary location before the destination is overwritten.
	/// </summary>
	/// <param name="destination">The span to copy items into.</param>
	/// <exception cref="System.ArgumentException">
	/// Thrown when the destination Span is shorter than the source Span.
	/// </exception>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void CopyTo(Span<T> destination) => InnerSpan.CopyTo(destination);

	/// <summary>
	/// Copies the contents of this span into destination span. If the source
	/// and destinations overlap, this method behaves as if the original values in
	/// a temporary location before the destination is overwritten.
	/// </summary>
	/// <param name="destination">The span to copy items into.</param>
	/// <returns>If the destination span is shorter than the source span, this method
	/// return false and no data is written to the destination.</returns>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal bool TryCopyTo(Span<T> destination) => InnerSpan.TryCopyTo(destination);

	/// <summary>
	/// Defines an implicit conversion of a <see cref="ReadOnlyPinnedSpan{T}"/> to a <see cref="ReadOnlySpan{T}"/>
	/// </summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator ReadOnlySpan<T>(ReadOnlyPinnedSpan<T> span) => span.InnerSpan;

	/// <summary>
	/// For <see cref="Span{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
	/// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
	/// </summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public override string ToString() => InnerSpan.ToString();

	/// <summary>
	/// Forms a slice out of the given span, beginning at 'start'.
	/// </summary>
	/// <param name="start">The index at which to begin this slice.</param>
	/// <exception cref="System.ArgumentOutOfRangeException">
	/// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
	/// </exception>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ReadOnlyPinnedSpan<T> Slice(int start) => new(InnerSpan.Slice(start));

	/// <summary>
	/// Forms a slice out of the given span, beginning at 'start', of given length
	/// </summary>
	/// <param name="start">The index at which to begin this slice.</param>
	/// <param name="length">The desired length for the slice (exclusive).</param>
	/// <exception cref="System.ArgumentOutOfRangeException">
	/// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
	/// </exception>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ReadOnlyPinnedSpan<T> Slice(int start, int length) => new(InnerSpan.Slice(start, length));

	/// <summary>
	/// Copies the contents of this span into a new array.  This heap
	/// allocates, so should generally be avoided, however it is sometimes
	/// necessary to bridge the gap with APIs written in terms of arrays.
	/// </summary>
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal T[] ToArray() => InnerSpan.ToArray();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal PinnedSpan<T> ToSpanUnsafe() =>
		PinnedSpan<T>.FromInternal(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(InnerSpan), InnerSpan.Length));
}
