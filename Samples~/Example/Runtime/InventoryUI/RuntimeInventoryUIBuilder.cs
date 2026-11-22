using LOGIYGames;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeInventoryUIBuilder
{
    private static Sprite _backgroundSprite;

    private static Sprite BackgroundSprite
    {
        get
        {
            if (_backgroundSprite == null)
                _backgroundSprite = Resources.Load<Sprite>("UI/Skin/Background.psd");
            return _backgroundSprite;
        }
    }

    // ---------- Drag ghost ----------

    public static Image CreateDragGhost(Transform parent, out TMP_Text amountText)
    {
        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(64f, 64f);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        img.enabled = false;

        var amountGo = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        amountGo.transform.SetParent(go.transform, false);
        var amountRt = amountGo.GetComponent<RectTransform>();
        amountRt.anchorMin = Vector2.zero;
        amountRt.anchorMax = Vector2.one;
        amountRt.offsetMin = new Vector2(0f, -4f);
        amountRt.offsetMax = new Vector2(-4f, 0f);

        var tmp = amountGo.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        tmp.color = Color.white;

        amountText = tmp;
        return img;
    }

    // ---------- Context menu ----------

    public static ItemContextMenu CreateContextMenu(Transform parent)
    {
        var root = new GameObject("ItemContextMenu", typeof(RectTransform), typeof(ItemContextMenu));
        root.transform.SetParent(parent, false);

        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // Backdrop (прозрачный, ловит клики вне меню)
        var backdrop = CreateFullscreenImage("Backdrop", root.transform, new Color(0f, 0f, 0f, 0.001f));
        var backdropBtn = backdrop.gameObject.AddComponent<Button>();
        backdropBtn.transition = Selectable.Transition.None;

        // Panel
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(root.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.sizeDelta = new Vector2(220f, 0f);

        var panelImg = panel.GetComponent<Image>();
        panelImg.sprite = BackgroundSprite;
        panelImg.type = Image.Type.Sliced;
        panelImg.color = new Color(0.13f, 0.13f, 0.17f, 0.97f);

        var panelVlg = panel.GetComponent<VerticalLayoutGroup>();
        panelVlg.padding = new RectOffset(6, 6, 6, 6);
        panelVlg.spacing = 4;
        panelVlg.childControlWidth = true;
        panelVlg.childControlHeight = true;
        panelVlg.childForceExpandWidth = true;
        panelVlg.childForceExpandHeight = false;

        var panelCsf = panel.GetComponent<ContentSizeFitter>();
        panelCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ButtonsRoot — контейнер для кнопок (клоны шаблона)
        var buttonsRoot = new GameObject("ButtonsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
        buttonsRoot.transform.SetParent(panel.transform, false);

        var brVlg = buttonsRoot.GetComponent<VerticalLayoutGroup>();
        brVlg.spacing = 4;
        brVlg.childControlWidth = true;
        brVlg.childControlHeight = true;
        brVlg.childForceExpandWidth = true;
        brVlg.childForceExpandHeight = false;

        var brLe = buttonsRoot.AddComponent<LayoutElement>();
        brLe.flexibleHeight = 0;

        // Button template (inactive) — клонируется фабрикой
        var buttonTemplate = CreateButtonTemplate(root.transform);

        var comp = root.GetComponent<ItemContextMenu>();
        comp.Setup(panelRt, buttonsRoot.transform as RectTransform, buttonTemplate, backdropBtn);

        return comp;
    }

    // ---------- helpers ----------

    private static Image CreateFullscreenImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.sprite = BackgroundSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = true;
        return img;
    }

    private static Button CreateButtonTemplate(Transform parent)
    {
        var go = new GameObject("ButtonTemplate", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.SetActive(false);   // шаблон невидим; фабрика активирует клоны

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 32f);

        var img = go.GetComponent<Image>();
        img.sprite = BackgroundSprite;
        img.type = Image.Type.Sliced;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.22f, 0.22f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.42f, 1f);
        colors.pressedColor = new Color(0.18f, 0.18f, 0.25f, 1f);
        colors.disabledColor = new Color(0.15f, 0.15f, 0.18f, 0.5f);
        btn.colors = colors;

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 32f;
        le.preferredHeight = 32f;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(10f, 2f);
        labelRt.offsetMax = new Vector2(-10f, -2f);

        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.fontSize = 18;
        label.raycastTarget = false;
        label.color = Color.white;

        return btn;
    }
}