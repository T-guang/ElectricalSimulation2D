using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ElectricalSim.UI
{
    public sealed class LocalProfilePageController : MonoBehaviour
    {
        private const string VersionText = "V1.0 本地版";
        private RectTransform contentRoot;
        private Text userInfoText;
        private Text drawingInfoText;
        private Text dataInfoText;

        private string SavedBlueprintDirectory => Path.Combine(Application.persistentDataPath, "SavedBlueprints");

        private void Awake()
        {
            BuildLayout();
            RefreshInfo();
        }

        private void OnEnable()
        {
            RefreshInfo();
        }

        private void BuildLayout()
        {
            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            var bg = gameObject.GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
            }

            bg.color = new Color(0.96f, 0.98f, 1f, 1f);

            var title = CreateText("ProfileTitle", root, "系统信息", 32, FontStyle.Bold, new Color(0.05f, 0.08f, 0.14f), TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(44f, -34f), new Vector2(-88f, 52f));

            var subtitle = CreateText("ProfileSubtitle", root, "当前为单机本地模式，数据保存在本机。", 18, FontStyle.Normal, new Color(0.35f, 0.42f, 0.52f), TextAnchor.MiddleLeft);
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(44f, -82f), new Vector2(-88f, 36f));

            var scrollGo = new GameObject("ProfileScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(root, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            SetRect(scrollRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -84f), new Vector2(-80f, -150f));
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            SetRect(viewportRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            contentRoot = content.GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.offsetMin = new Vector2(0f, 0f);
            contentRoot.offsetMax = new Vector2(0f, 0f);

            var layout = content.GetComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(44, 44, 24, 24);
            layout.spacing = new Vector2(24f, 24f);
            layout.cellSize = new Vector2(460f, 240f);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.Flexible;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRoot;
            scroll.horizontal = false;
            scroll.vertical = true;

            AddCard("软件信息", "软件名称：电工数字学生仿真系统\n软件版本：" + VersionText + "\n运行模式：PC 单机版\n网络状态：无需联网");
            drawingInfoText = AddCard("本地图纸", string.Empty, CreateDrawingButtons);
            dataInfoText = AddCard("数据管理", string.Empty, CreateDataButtons);
            AddCard("项目说明", "本系统用于电工电气教学仿真，支持本地模板、自由接线、运行仿真、检查助手、元器件百科和常用工具。当前版本不需要联网，主要功能均可在本机离线使用。");
        }

        private Text AddCard(string title, string body, Action<RectTransform> extraBuilder = null)
        {
            var card = new GameObject(title + "Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(contentRoot, false);
            var image = card.GetComponent<Image>();
            image.color = Color.white;

            var layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var titleText = CreateText("Title", card.transform, title, 18, FontStyle.Bold, new Color(0.06f, 0.14f, 0.26f), TextAnchor.MiddleLeft);
            titleText.rectTransform.sizeDelta = new Vector2(0f, 28f);

            var bodyText = CreateText("Body", card.transform, body, 14, FontStyle.Normal, new Color(0.35f, 0.42f, 0.52f), TextAnchor.UpperLeft);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(card.transform, false);
            spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;

            extraBuilder?.Invoke(card.GetComponent<RectTransform>());
            return bodyText;
        }

        private void CreateDrawingButtons(RectTransform parent)
        {
            var row = CreateButtonRow(parent);
            CreateButton(row, "打开图纸文件夹", OpenSavedBlueprintFolder, true);
            CreateButton(row, "刷新信息", RefreshInfo, false);
        }

        private void CreateDataButtons(RectTransform parent)
        {
            var row = CreateButtonRow(parent);
            CreateButton(row, "打开数据目录", OpenPersistentDataFolder, true);
            CreateButton(row, "清理本地缓存", () => { }, false);
        }

        private RectTransform CreateButtonRow(RectTransform parent)
        {
            var row = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var element = row.GetComponent<LayoutElement>();
            element.minHeight = 36f;
            return row.GetComponent<RectTransform>();
        }

        private void CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action, bool primary)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = primary ? new Color(0.14f, 0.38f, 0.92f, 1f) : new Color(0.92f, 0.95f, 0.99f, 1f);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(action);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = 140f;
            element.preferredHeight = 36f;
            var textColor = primary ? Color.white : new Color(0.33f, 0.41f, 0.55f);
            CreateText("Text", go.transform, label, 14, FontStyle.Normal, textColor, TextAnchor.MiddleCenter);
        }

        private void RefreshInfo()
        {
            if (drawingInfoText != null)
            {
                var count = CountSavedBlueprints();
                if (count == 0)
                {
                    drawingInfoText.text = "当前暂无本地图纸。\n你可以在模拟电路页面点击“保存图纸”创建本地图纸。";
                }
                else
                {
                    drawingInfoText.text = "本地图纸数量：" + count + "\n图纸保存位置：SavedBlueprints";
                }
            }

            if (dataInfoText != null)
            {
                dataInfoText.text = "本地数据目录：ElectricalSimulation2D\n说明：图纸、配置和本地缓存均保存在本机。";
            }
        }

        private int CountSavedBlueprints()
        {
            try
            {
                if (!Directory.Exists(SavedBlueprintDirectory))
                {
                    return 0;
                }

                return Directory.GetFiles(SavedBlueprintDirectory, "*.json").Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private void OpenSavedBlueprintFolder()
        {
            EnsureDirectory(SavedBlueprintDirectory);
            OpenFolder(SavedBlueprintDirectory);
        }

        private void OpenPersistentDataFolder()
        {
            EnsureDirectory(Application.persistentDataPath);
            OpenFolder(Application.persistentDataPath);
        }



        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void OpenFolder(string path)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start("explorer.exe", path);
#else
            Application.OpenURL("file:///" + path.Replace("\\", "/"));
#endif
        }

        private static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
