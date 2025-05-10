#nullable enable

using UnityEngine;

namespace Ayla.Core
{
    public static class ColorUtility
    {
        public static Color AlphaBlend(this Color colorA, Color colorB)
        {
            float aA = 1.0f - colorB.a;
            float aB = colorB.a;
            return new Color(
                colorA.r * aA + colorB.r * aB,
                colorA.g * aA + colorB.g * aB,
                colorB.b * aA + colorB.b * aB,
                aB
            );
        }

        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
