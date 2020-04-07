﻿using Xunit;
using static Funk.Prelude;

namespace Funk.Tests
{
    public class ObjectTests : Test
    {
        [Fact]
        public void SafeCast_False()
        {
            UnitTest(
                _ => ("Harun", 24),
                r => r.SafeCast<Record<string, int>>(),
                m => Assert.True(m.IsEmpty)
            );
        }

        [Fact]
        public void SafeCast_True()
        {
            UnitTest(
                _ => (object)rec("Harun", 24),
                r => r.SafeCast<Record<string, int>>(),
                m => Assert.True(m.NotEmpty)
            );
        }

        [Fact]
        public void Match_On_Int_For_Result()
        {
            UnitTest(
                _ => 2,
                i => i.Match(
                    2, _ => "Funk",
                    3, _ => "Harun"
                ),
                s => Assert.Equal("Funk", s)
            );
        }

        [Fact]
        public void Match_On_String_For_Result()
        {
            UnitTest(
                _ => "Bosnia",
                i => i.Match(
                    "Harun", _ => "Funk",
                    "Funk", _ => "Harun",
                    _ => "Hello Funk"
                ),
                s => Assert.Equal("Hello Funk", s)
            );
        }

        [Fact]
        public void Match_On_String_For_Result_Throws()
        {
            UnitTest(
                _ => "Bosnia",
                i => act(() => i.Match(
                    "Harun", _ => "Funk",
                    "Funk", _ => "Harun"
                )),
                a => Assert.Throws<UnhandledValueException>(a)
            );
        }
    }
}
