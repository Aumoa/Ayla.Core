#nullable enable

using System;

namespace Ayla
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DefaultOrderAttribute : Attribute
    {
        public readonly int Order;

        public DefaultOrderAttribute(int order)
        {
            Order = order;
        }
    }
}