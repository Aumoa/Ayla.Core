#nullable enable

using System;

namespace Ayla
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string category)
        {
            Category = category;
        }

        public virtual string Category { get; }
    }
}