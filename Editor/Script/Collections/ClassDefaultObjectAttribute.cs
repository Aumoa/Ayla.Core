#nullable enable

using System;
using System.Reflection.Emit;

namespace Ayla
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ClassDefaultObjectAttribute : Attribute
    {
        public static readonly CustomAttributeBuilder Builder = new(
            typeof(ClassDefaultObjectAttribute).GetConstructor(Array.Empty<Type>()),
            Array.Empty<object>()
            );
    }
}
