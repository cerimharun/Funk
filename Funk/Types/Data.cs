﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Funk.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static Funk.Prelude;

namespace Funk
{
    /// <summary>
    /// Provides fluent way of creating new immutable objects with updated fields/properties using builders.
    /// </summary>
    public abstract class Data<T> where T : Data<T>
    {
        protected void Include(Expression<Func<T, object>> expression) => expression.ToImmutableList().Include();

        protected void Include(params Expression<Func<T, object>>[] expressions) => expressions.Map().Include();
        
        protected void Exclude(Expression<Func<T, object>> expression) => expression.ToImmutableList().Exclude();

        protected void Exclude(params Expression<Func<T, object>>[] expressions) => expressions.Map().Exclude();
    }

    public static class Data
    {
        /// <summary>
        /// Creates a Builder object for the specified Data type.
        /// </summary>
        public static Builder<T> With<T, TKey>(this Data<T> item, Expression<Func<T, object>> expression, TKey value) where T : Data<T> =>
            new Builder<T>(
                item,
                new List<(Expression<Func<T, object>> expression, object value)>
                {
                    (expression, value)
                }
            );
        
        /// <summary>
        /// Creates a Builder object for the specified Data type.
        /// </summary>
        public static Builder<T> With<T>(this Data<T> item, params (Expression<Func<T, object>> expression, object value)[] expressions) where T : Data<T> =>
            new Builder<T>(
                item,
                expressions
            );
        
        /// <summary>
        /// Creates a new object with the modified field/property specified in the expression.
        /// </summary>
        public static T WithBuild<T, TKey>(this Data<T> item, Expression<Func<T, object>> expression, TKey value) where T : Data<T> =>
            item.With(expression, value).Build();
        
        /// <summary>
        /// Creates a Builder object for the specified Data type.
        /// </summary>
        public static T WithBuild<T>(this Data<T> item, params (Expression<Func<T, object>> expression, object value)[] expressions) where T : Data<T> =>
            item.With(expressions).Build();

        /// <summary>
        /// Creates a Builder object from the provided one for the specified Data type.
        /// </summary>
        public static Builder<T> With<T, TKey>(this Builder<T> builder, Expression<Func<T, object>> expression, TKey value) where T : Data<T> =>
            builder.With(expression, value);
        
        /// <summary>
        /// Creates a Builder object from the provided one for the specified Data type.
        /// </summary>
        public static Builder<T> With<T>(this Builder<T> builder, params (Expression<Func<T, object>> expression, object value)[] expressions) where T : Data<T> =>
            builder.With(expressions);
        
        /// <summary>
        /// Creates a new object with the modified field/property specified in the expression.
        /// </summary>
        public static T WithBuild<T, TKey>(this Builder<T> builder, Expression<Func<T, object>> expression, TKey value) where T : Data<T> =>
            builder.With(expression, value).Build();
        
        /// <summary>
        /// Creates a Builder object from the provided one for the specified Data type.
        /// </summary>
        public static T WithBuild<T>(this Builder<T> builder, params (Expression<Func<T, object>> expression, object value)[] expressions) where T : Data<T> =>
            builder.With(expressions).Build();

        /// <summary>
        /// Creates a new Data object from the specified Builder.
        /// </summary>
        public static T Build<T>(this Builder<T> builder) where T : Data<T>
        {
            var aggregate = builder.Expressions.Aggregate(builder.Item.Copy(),
                (item, expressions) => item.Map(expressions.expression, expressions.value));
            return aggregate.Copy();
        }

        internal static void Include<T>(this IEnumerable<Expression<Func<T, object>>> expressions) where T : Data<T>
        {
            foreach (var e in expressions)
            {
                var member = e.GetMember();
                Inclusions.TryAdd($"{member.type.FullName}.{member.member}", member);
            }
        }
        
        internal static void Exclude<T>(this IEnumerable<Expression<Func<T, object>>> expressions) where T : Data<T>
        {
            foreach (var e in expressions)
            {
                var member = e.GetMember();
                Exclusions.TryAdd($"{member.type.FullName}.{member.member}", member);
            }
        }

        internal static readonly ConcurrentDictionary<string, (Type type, string member)> Inclusions =
            new ConcurrentDictionary<string, (Type type, string member)>();
        
        internal static readonly ConcurrentDictionary<string, (Type type, string member)> Exclusions =
            new ConcurrentDictionary<string, (Type type, string member)>();
        
        private static T Copy<T>(this Data<T> data) where T : Data<T> =>
            Exc.Create(
                _ => JsonConvert.DeserializeObject<T>
                (
                    JsonConvert.SerializeObject
                    (
                        data,
                        new JsonSerializerSettings 
                        {
                            ContractResolver = new NonPublic()
                        }
                    ),
                    new JsonSerializerSettings
                    {
                        ContractResolver = new Writable()
                    }
                )
            ).Match(
                v => v,
                e => throw new SerializationException(e.Root.Map(r => r.Message).GetOr(_ => "Item cannot be serialized."))
            );
    }

    public sealed class Builder<T> where T : Data<T>
    {
        internal Builder(Data<T> item, IEnumerable<(Expression<Func<T, object>> expression, object value)> expressions)
        {
            Item = item;
            Expressions.AddRange(expressions.Map());
        }

        internal Builder<T> With(Expression<Func<T, object>> expression, object value) =>
            new Builder<T>(Item, Expressions.Concat(list((expression, value))));
        
        internal Builder<T> With(IEnumerable<(Expression<Func<T, object>> expression, object value)> expressions) =>
            new Builder<T>(Item, Expressions.Concat(expressions));
        
        internal Data<T> Item { get; }

        internal readonly List<(Expression<Func<T, object>> expression, object value)> Expressions =
            new List<(Expression<Func<T, object>>, object)>();

        public static implicit operator Builder<T>(Data<T> data) =>
            new Builder<T>(data, list<(Expression<Func<T, object>>, object)>());
    }
    
    internal class Writable : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            return new TypePattern<JsonProperty>
            {
                (PropertyInfo p) => p.GetSetMethod(true).AsMaybe().Map(_ =>
                {
                    property.Readable = true;
                    property.Writable = true;
                    return property;
                }).GetOr(_ => property),
                (FieldInfo f) =>
                {
                    property.Readable = true;
                    property.Writable = true;
                    return property;
                }
            }.Match(member).UnsafeGet(_ => new InvalidOperationException("Type member must be either a property or a field."));
        }
        
        protected override List<MemberInfo> GetSerializableMembers(Type type)
        {
            var result = base.GetSerializableMembers(type);
            var inclusions = Data.Inclusions.ToList()
                .Where(i => i.Value.type.SafeEquals(type)).Select(i => i.Value.member)
                .Select(i => type.GetMember(i, BindingFlags.NonPublic | BindingFlags.Instance).Single()).ToList();
            var exclusions = result.Where(m => Data.Exclusions.ToList()
                .Where(i => i.Value.type.SafeEquals(type)).Select(i => i.Value.member).Contains(m.Name)).ToList();
            result.AddRange(inclusions);
            return result.Except(exclusions).ToList();
        }
        
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            var included = properties.Where(p => Data.Inclusions.ToList()
                .Where(i => i.Value.type.SafeEquals(type)).Select(i => i.Value.member).Contains(p.PropertyName)).ToList();
            var excluded = properties.Where(p => Data.Exclusions.ToList()
                .Where(i => i.Value.type.SafeEquals(type)).Select(i => i.Value.member).Contains(p.PropertyName)).ToList();
            foreach (var p in included)
            {
                p.Readable = true;
                p.Writable = true;
            }
            foreach (var p in excluded)
            {
                p.Readable = false;
                p.Writable = false;
            }
            return properties;
        }
    }

    internal class NonPublic : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type type)
        {
            var result = base.GetSerializableMembers(type);
            var inclusions = Data.Inclusions.ToList()
                .Where(i => i.Value.type.SafeEquals(type)).Select(i => i.Value.member)
                .Select(i => type.GetMember(i, BindingFlags.NonPublic | BindingFlags.Instance).Single()).ToList();
            var exclusions = result.Where(m => Data.Exclusions.ToList()
                .Where(i => i.Value.type.SafeEquals(type)).Select(i => i.Value.member).Contains(m.Name)).ToList();
            result.AddRange(inclusions);
            return result.Except(exclusions).ToList();
        }
        
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            var included = properties.Where(p => Data.Inclusions.ToList()
                .Where(i => i.Value.type.SafeEquals(type)).Select(i => i.Value.member).Contains(p.PropertyName)).ToList();
            var excluded = properties.Where(p => Data.Exclusions.ToList()
                .Where(i => i.Value.type.SafeEquals(type)).Select(i => i.Value.member).Contains(p.PropertyName)).ToList();
            foreach (var p in included)
            {
                p.Readable = true;
                p.Writable = true;
            }
            foreach (var p in excluded)
            {
                p.Readable = false;
                p.Writable = false;
            }
            return properties;
        }
    }
}