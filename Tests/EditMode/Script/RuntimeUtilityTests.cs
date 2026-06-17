#nullable enable

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

    public sealed class RectGeometryUtilityTests
    {
        [Test]
        public void Margin_ClampsWhenOffsetsPassOppositeEdges()
        {
            var source = new Rect(10f, 20f, 30f, 40f);

            var result = source.Margin(50f, 5f, 1f, 50f);

            AssertRectEqual(new Rect(40f, 25f, 0f, 0f), result);
        }

        [Test]
        public void FillRightBottom_UsesRequestedSizeAnchoredToLowerRight()
        {
            var source = new Rect(10f, 20f, 30f, 40f);

            var result = source.FillRightBottom(7f, 9f);

            AssertRectEqual(new Rect(33f, 51f, 7f, 9f), result);
        }

        [Test]
        public void MakeChild_WithOversizedSize_ClipsToParentMaximum()
        {
            var source = new Rect(10f, 20f, 30f, 40f);

            var result = source.MakeChild(new Vector2(25f, 50f), new Vector2(40f, 40f));

            AssertRectEqual(new Rect(25f, 50f, 15f, 10f), result);
        }

        private static void AssertRectEqual(Rect expected, Rect actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f), nameof(Rect.x));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f), nameof(Rect.y));
            Assert.That(actual.width, Is.EqualTo(expected.width).Within(0.0001f), nameof(Rect.width));
            Assert.That(actual.height, Is.EqualTo(expected.height).Within(0.0001f), nameof(Rect.height));
        }
    }
}
