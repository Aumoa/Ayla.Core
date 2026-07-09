#nullable enable

using NUnit.Framework;
using UnityEngine;

namespace Ayla.Tests
{
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
