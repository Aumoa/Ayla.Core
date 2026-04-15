#nullable enable

using System;
using System.Reflection;
using UnityEditor;

namespace Ayla
{
    public abstract class DevelopmentTools
    {
        internal static class InternalConstructorArgs
        {
            public readonly struct Disposable : IDisposable
            {
                public void Dispose()
                {
                    s_Allowed = false;
                    s_Owner = null;
                }
            }

            public static bool s_Allowed;
            public static DevelopmentWindow? s_Owner;
            public static Type? s_SourceType;

            public static Disposable Ready(DevelopmentWindow owner, Type sourceType)
            {
                s_Allowed = true;
                s_Owner = owner;
                s_SourceType = sourceType;
                return new Disposable();
            }

            internal static void OnAfterDeserialize()
            {
            }
        }

        internal readonly struct SuppressCallDelayUpdateDisposable : IDisposable
        {
            private readonly DevelopmentTools m_Owner;

            public SuppressCallDelayUpdateDisposable(DevelopmentTools owner)
            {
                m_Owner = owner;
            }

            public void Dispose()
            {
                m_Owner.m_SuppressCallDelayUpdate = false;
            }
        }

        internal readonly DevelopmentWindow m_Owner;
        internal readonly Type m_SourceType;
        private readonly string m_Title;
        internal bool m_SuppressCallDelayUpdate;

        internal float CachedHeight
        {
            get => EditorPrefs.GetFloat(GetPrefsKey(false, "m_CachedHeight"));
            set => EditorPrefs.SetFloat(GetPrefsKey(false, "m_CachedHeight"), value);
        }

        internal float ViewHeight
        {
            get => IsExpanded ? CachedHeight : 0;
        }

        public virtual string Title => m_Title;

        public virtual bool IsFavorite
        {
            get => EditorPrefs.GetBool(GetPrefsKey(false, "m_IsFavorite"));
            set
            {
                EditorPrefs.SetBool(GetPrefsKey(false, "m_IsFavorite"), value);
                if (m_SuppressCallDelayUpdate == false)
                {
                    EditorApplication.delayCall += m_Owner.ReorderAndPopulateFavorite;
                }
            }
        }

        public virtual int Order
        {
            get => EditorPrefs.GetInt(GetPrefsKey(true, "m_Order"), m_SourceType.GetCustomAttribute<DefaultOrderAttribute>()?.Order ?? 0);
            set
            {
                EditorPrefs.SetInt(GetPrefsKey(true, "m_Order"), value);
                if (m_SuppressCallDelayUpdate == false)
                {
                    EditorApplication.delayCall += m_Owner.ReorderAndPopulateFavorite;
                }
            }
        }

        public virtual bool IsExpanded
        {
            get => EditorPrefs.GetBool(GetPrefsKey(true, "m_IsExpanded"), true);
            set
            {
                EditorPrefs.SetBool(GetPrefsKey(true, "m_IsExpanded"), value);
                if (m_SuppressCallDelayUpdate == false)
                {
                    EditorApplication.delayCall += m_Owner.Repaint;
                }
            }
        }

        protected DevelopmentTools()
        {
            if (InternalConstructorArgs.s_Allowed == false)
            {
                throw new InvalidOperationException("Instantiate DevelopmentTools class is not allowed.");
            }

            m_Owner = InternalConstructorArgs.s_Owner!;
            m_SourceType = InternalConstructorArgs.s_SourceType!;
            m_Title = m_SourceType.GetCustomAttribute<NameAttribute>()?.Name ?? ObjectNames.NicifyVariableName(GetType().Name);
        }

        internal SuppressCallDelayUpdateDisposable SuppressCallDelayUpdate()
        {
            m_SuppressCallDelayUpdate = true;
            return new SuppressCallDelayUpdateDisposable(this);
        }

        internal void DoOnGUI(in DrawingArgs drawingArgs)
        {
            OnGUI(in drawingArgs);
        }

        protected abstract void OnGUI(in DrawingArgs drawingArgs);

        protected internal virtual string OnSerialize()
        {
            return EditorJsonUtility.ToJson(this);
        }

        protected internal virtual void OnDeserialize(string value)
        {
            EditorJsonUtility.FromJsonOverwrite(value, this);
        }

        public string GetPrefsKey(string memberName) => GetPrefsKey(false, memberName);

        protected virtual string GetPrefsKey(bool useSuffix, string memberName)
        {
            return $"Ayla.Inspector:{m_SourceType.FullName}.{memberName}";
        }
    }
}