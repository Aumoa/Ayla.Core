#nullable enable

namespace Ayla
{
    internal interface IUnityIconExplorer
    {
        IconSize IconSize { get; set; }
        string SearchSymbol { get; set; }
        float VerticalScroll { get; set; }
        long Selected { get; set; }
    }
}