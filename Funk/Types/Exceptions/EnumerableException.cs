﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;

namespace Funk.Exceptions
{
    public static class EnumerableException
    {
        [Pure]
        public static EnumerableException<E> Create<E>(string message) where E : Exception => new EnumerableException<E>(message);

        [Pure]
        public static EnumerableException<E> Create<E>(E exception) where E : Exception => new EnumerableException<E>(exception);

        [Pure]
        public static EnumerableException<E> Create<E>(string message, IEnumerable<E> exceptions) where E : Exception => new EnumerableException<E>(message, exceptions);
    }

    public sealed class EnumerableException<E> : FunkException, IEnumerable<E> where E : Exception
    {
        /// <summary>
        /// Structure-preserving map.
        /// Maps EnumerableException to the new one with aggregated nested exceptions with new exception.
        /// </summary>
        public EnumerableException<E> MapWith(Func<Unit, E> selector)
        {
            return EnumerableException.Create(Message, selector(Unit.Value).MergeRange(Nested));
        }

        /// <summary>
        /// Structure-preserving map.
        /// Maps EnumerableException to the new one with aggregated nested exceptions with new exceptions.
        /// </summary>
        public EnumerableException<E> MapWith(Func<Unit, IEnumerable<E>> selector)
        {
            var list = new List<E>(Nested);
            list.AddRange(selector(Unit.Value).Map());
            return EnumerableException.Create(Message, list.ExceptNulls());
        }

        public EnumerableException(string message)
            : base(FunkExceptionType.Enumerable, message)
        {
            Nested = ImmutableList<E>.Empty;
        }

        public EnumerableException(E exception)
            : base(FunkExceptionType.Enumerable, exception?.Message)
        {
            Nested = ImmutableList<E>.Empty;
        }

        public EnumerableException(string message, IEnumerable<E> nested)
            : base(FunkExceptionType.Enumerable, message)
        {
            Nested = nested.ExceptNulls();
        }

        public IReadOnlyCollection<E> Nested { get; }

        /// <summary>
        /// Returns an immutable dictionary of key as a discriminator and collection of corresponding exceptions.
        /// </summary>
        [Pure]
        public IReadOnlyDictionary<TKey, IReadOnlyCollection<Exception>> ToDictionary<TKey>(Func<Exception, TKey> keySelector) => Nested.ToDictionary(keySelector);

        public IEnumerator<E> GetEnumerator()
        {
            return Nested.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
