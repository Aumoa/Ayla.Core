#nullable enable

namespace Ayla
{
    internal class SystemCategoryAttribute : CategoryAttribute
    {
        public SystemCategoryAttribute() : base("System")
        {
        }

        public override string Category => SystemToolsText.CategoryName;
    }
}
