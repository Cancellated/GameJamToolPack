using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using MyGame.UI.Settings.Model;

namespace MyGame.UI.Settings.View
{
    /// <summary>
    /// 新设置面板运行时布局：纯代码构建整个设置内容区。
    /// 只依赖 Catalog 数据 + ISettingsValueProvider，不依赖行预制体或旧设置组件。
    /// </summary>
    public class SettingsRuntimeLayout : MonoBehaviour
    {
        private const string CATALOG_ADDRESS = "Config/SettingsCatalog";

        [Tooltip("Catalog 直接引用；为空时从 Addressables 加载")]
        [SerializeField] private SettingsCatalog m_catalog;

        private readonly List<SettingsOptionRowView> m_rows = new();
        private ISettingsValueProvider m_provider;
        private AsyncOperationHandle<SettingsCatalog> m_catalogHandle;
        private bool m_catalogLoadedFromAddressables;
        private SettingsCatalog m_fallbackCatalog;
        private RectTransform m_layoutRoot;
        private RectTransform m_viewport;
        private RectTransform m_content;

        public bool IsBuilt { get; private set; }

        public bool Build(ISettingsValueProvider provider)
        {
            Clear();

            m_provider = provider;
            if (m_provider == null)
            {
                Debug.LogWarning("[Settings] SettingsRuntimeLayout.Build: provider is null");
                return false;
            }

            SettingsCatalog catalog = ResolveCatalog();
            if (catalog == null || catalog.Entries == null || catalog.Entries.Count == 0)
            {
                Debug.LogWarning("[Settings] SettingsRuntimeLayout.Build: catalog unavailable");
                return false;
            }

            EnsureLayoutRoot();
            Transform content = m_content;
            if (content == null)
            {
                return false;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform child = content.GetChild(i);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            foreach (SettingsOptionDefinition definition in catalog.Entries)
            {
                if (definition == null)
                {
                    continue;
                }

                GameObject rowGo = new(definition.type == SettingsOptionType.Header
                        ? $"Header_{definition.title}"
                        : $"Option_{definition.key}",
                    typeof(RectTransform), typeof(LayoutElement), typeof(SettingsOptionRowView));
                rowGo.transform.SetParent(content, false);

                RectTransform rect = (RectTransform)rowGo.transform;
                float height = definition.type == SettingsOptionType.Header ? 60f : 64f;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, height);

                LayoutElement layout = rowGo.GetComponent<LayoutElement>();
                layout.preferredHeight = height;
                layout.flexibleWidth = 1f;

                SettingsOptionRowView row = rowGo.GetComponent<SettingsOptionRowView>();
                row.Initialize(definition, m_provider);
                m_rows.Add(row);
            }

            IsBuilt = true;
            return true;
        }

        public void RefreshAll()
        {
            foreach (SettingsOptionRowView row in m_rows)
            {
                if (row != null)
                {
                    row.Refresh();
                }
            }
        }

        public void Clear()
        {
            m_rows.Clear();
            m_provider = null;
            IsBuilt = false;

            if (m_catalogLoadedFromAddressables && m_catalogHandle.IsValid())
            {
                Addressables.Release(m_catalogHandle);
                m_catalogHandle = default;
                m_catalog = null;
                m_catalogLoadedFromAddressables = false;
            }

            if (m_fallbackCatalog != null)
            {
                Destroy(m_fallbackCatalog);
                m_fallbackCatalog = null;
            }
        }

        private SettingsCatalog ResolveCatalog()
        {
            if (m_catalog != null)
            {
                return m_catalog;
            }

            if (!m_catalogHandle.IsValid())
            {
                m_catalogHandle = Addressables.LoadAssetAsync<SettingsCatalog>(CATALOG_ADDRESS);
            }

            m_catalogHandle.WaitForCompletion();
            if (m_catalogHandle.Status == AsyncOperationStatus.Succeeded && m_catalogHandle.Result != null)
            {
                m_catalog = m_catalogHandle.Result;
                m_catalogLoadedFromAddressables = true;
                return m_catalog;
            }

            if (m_catalogHandle.IsValid())
            {
                Addressables.Release(m_catalogHandle);
                m_catalogHandle = default;
            }

            m_fallbackCatalog = CreateFallbackCatalog();
            return m_fallbackCatalog;
        }

        private static SettingsCatalog CreateFallbackCatalog()
        {
            SettingsCatalog fallback = ScriptableObject.CreateInstance<SettingsCatalog>();
            fallback.Entries.AddRange(new[]
            {
                new SettingsOptionDefinition { key = "header_audio", title = "音频", type = SettingsOptionType.Header },
                new SettingsOptionDefinition { key = "musicVolume", title = "音乐音量", type = SettingsOptionType.Slider, minValue = 0f, maxValue = 1f },
                new SettingsOptionDefinition { key = "sfxVolume", title = "音效音量", type = SettingsOptionType.Slider, minValue = 0f, maxValue = 1f },
                new SettingsOptionDefinition { key = "header_graphics", title = "画质", type = SettingsOptionType.Header },
                new SettingsOptionDefinition { key = "qualityLevel", title = "画质等级", type = SettingsOptionType.Dropdown, options = new List<string> { "低", "中", "高" } },
                new SettingsOptionDefinition { key = "fullscreen", title = "全屏", type = SettingsOptionType.Toggle },
                new SettingsOptionDefinition { key = "resolutionIndex", title = "分辨率", type = SettingsOptionType.Dropdown },
                new SettingsOptionDefinition { key = "header_controls", title = "操作", type = SettingsOptionType.Header },
                new SettingsOptionDefinition { key = "invertYAxis", title = "Y轴反转", type = SettingsOptionType.Toggle },
                new SettingsOptionDefinition { key = "header_developer", title = "开发者", type = SettingsOptionType.Header },
                new SettingsOptionDefinition { key = "developerMode", title = "开发者模式（保存后生效）", type = SettingsOptionType.Toggle },
            });
            return fallback;
        }

        private void EnsureLayoutRoot()
        {
            // 每次 Build 都重建运行时内容区，避免旧行残留
            Transform existing = transform.Find("RuntimeContent");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                Destroy(existing.gameObject);
            }

            GameObject rootGo = new("RuntimeContent",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            rootGo.transform.SetParent(transform, false);
            m_layoutRoot = (RectTransform)rootGo.transform;

            m_layoutRoot.anchorMin = new Vector2(0.08f, 0f);
            m_layoutRoot.anchorMax = new Vector2(0.92f, 1f);
            m_layoutRoot.pivot = new Vector2(0.5f, 0.5f);
            m_layoutRoot.anchoredPosition = Vector2.zero;
            m_layoutRoot.offsetMin = new Vector2(0f, 110f);
            m_layoutRoot.offsetMax = new Vector2(0f, -120f);

            Image rootImage = rootGo.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.02f);
            rootImage.raycastTarget = true;

            ScrollRect scroll = rootGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;

            GameObject viewportGo = new("Viewport",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(m_layoutRoot, false);
            m_viewport = (RectTransform)viewportGo.transform;
            m_viewport.anchorMin = Vector2.zero;
            m_viewport.anchorMax = Vector2.one;
            m_viewport.offsetMin = Vector2.zero;
            m_viewport.offsetMax = Vector2.zero;
            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewportImage.raycastTarget = true;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(m_viewport, false);
            m_content = (RectTransform)contentGo.transform;
            m_content.anchorMin = new Vector2(0f, 1f);
            m_content.anchorMax = new Vector2(1f, 1f);
            m_content.pivot = new Vector2(0.5f, 1f);
            m_content.anchoredPosition = Vector2.zero;
            m_content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup group = contentGo.GetComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(20, 20, 12, 12);
            group.spacing = 10f;
            group.childAlignment = TextAnchor.UpperCenter;
            group.childControlWidth = true;
            group.childControlHeight = false;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = m_viewport;
            scroll.content = m_content;

            // 隐藏旧布局容器，避免和旧行/占位内容重叠
            Transform layoutLeft = transform.Find("LayoutLeft");
            if (layoutLeft != null) layoutLeft.gameObject.SetActive(false);

            Transform layoutRight = transform.Find("LayoutRight");
            if (layoutRight != null) layoutRight.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
