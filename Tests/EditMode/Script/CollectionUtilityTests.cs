#nullable enable

using System.Collections.Generic;
using NUnit.Framework;

namespace Ayla.Tests
{
    public sealed class CollectionUtilityTests
    {
        [Test]
        public void AddRange_AppendsItemsInEnumerationOrder()
        {
            var list = new List<int> { 1 };

            list.AddRange(new[] { 2, 3 });

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list);
        }

        [Test]
        public void IsValidIndex_SupportsForwardAndFromEndIndices()
        {
            IReadOnlyList<string> list = new[] { "first", "second", "third" };

            Assert.That(list.IsValidIndex(0), Is.True);
            Assert.That(list.IsValidIndex(2), Is.True);
            Assert.That(list.IsValidIndex(3), Is.False);
            Assert.That(list.IsValidIndex(^1), Is.True);
            Assert.That(list.IsValidIndex(^3), Is.True);
            Assert.That(list.IsValidIndex(^0), Is.False);
        }

        [Test]
        public void GetOrDefaultAt_ReturnsFallbackForOutOfRangeIndices()
        {
            IReadOnlyList<string> list = new[] { "first", "second", "third" };

            Assert.That(list.GetOrDefaultAt(1, "fallback"), Is.EqualTo("second"));
            Assert.That(list.GetOrDefaultAt(^1, "fallback"), Is.EqualTo("third"));
            Assert.That(list.GetOrDefaultAt(-1, "fallback"), Is.EqualTo("fallback"));
            Assert.That(list.GetOrDefaultAt(^0, "fallback"), Is.EqualTo("fallback"));
        }
    }
}
