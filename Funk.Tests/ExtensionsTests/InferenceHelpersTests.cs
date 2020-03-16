﻿using System;
using Xunit;
using static Funk.Operators;

namespace Funk.Tests
{
    public partial class OperatorsTests : Test
    {
        [Fact]
        public void Check_Type_Of_Inferred_Func_With_Arity_Of_3()
        {
            UnitTest(
                () => func((int x, int y) => ""),
                f => f.GetType() == typeof(Func<int, int, string>),
                Assert.True
            );
        }

        [Fact]
        public void Check_Type_Of_Inferred_Func_With_Arity_Of_4()
        {
            UnitTest(
                () => func((int x, int y, int z) => ""),
                f => f.GetType() == typeof(Func<int, int, string>),
                Assert.False
            );
        }

        [Fact]
        public void Check_Result_Of_Inferred_Func_With_Arity_Of_2()
        {
            UnitTest(
                () => func((int x) => $"Funk {x}"),
                f => f(2),
                r => Assert.Equal("Funk 2", r)
            );
        }

        [Fact]
        public void Check_Type_Of_Inferred_Action_With_Arity_Of_2()
        {
            UnitTest(
                () => act((int x, int y) => { }),
                a => a.GetType() == typeof(Action<int, int>),
                Assert.True
            );
        }

        [Fact]
        public void Check_Type_Of_Inferred_Action_With_Arity_Of_5()
        {
            UnitTest(
                () => act((int x, int y, int z, string s, string g) => { }),
                a => a.GetType() == typeof(Action<int, int>),
                Assert.False
            );
        }

        [Fact]
        public void Check_Result_Of_Inferred_Action_With_Arity_Of_0()
        {
            UnitTest(
                () => new Exception("Funk exception."),
                e => act(() => throw e),
                a =>
                {
                    var e = Assert.Throws<Exception>(a);
                    Assert.Equal("Funk exception.", e.Message);
                }
            );
        }
    }
}
