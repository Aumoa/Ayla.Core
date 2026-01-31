#nullable enable

namespace Ayla
{
    internal class FavoriteDevTool : DevelopmentTools
    {
        private readonly DevelopmentTools m_Source;

        public FavoriteDevTool(DevelopmentTools source)
        {
            m_Source = source;
        }

        public override string Title => m_Source.Title;

        protected internal override void OnGUI(DrawingArgs drawingArgs)
        {
            m_Source.OnGUI(drawingArgs);
        }

        protected internal override string OnSerialize()
        {
            return m_Source.OnSerialize();
        }

        protected internal override void OnDeserialize(string value)
        {
            m_Source.OnDeserialize(value);
        }

        protected override string GetPrefsKey(bool useSuffix, string memberName)
        {
            if (useSuffix)
            {
                return $"Ayla.Inspector:{m_SourceType.FullName}@Favorite.{memberName}";
            }
            else
            {
                return $"Ayla.Inspector:{m_SourceType.FullName}.{memberName}";
            }
        }
    }
}