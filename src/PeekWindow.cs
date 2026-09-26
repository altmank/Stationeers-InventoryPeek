using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryPeek;

/// <summary>
/// Lives on one inventory window. Hides it through its own CanvasGroup (alpha and raycasts only), so the game's
/// canvas, layout and open/closed state stay exactly as the game left them, adds the eye button that tags the
/// window hidable, and makes its background solid when OpaqueWindows is on.
/// </summary>
internal sealed class PeekWindow : MonoBehaviour
{
    private const float ShownAlpha = 1f;
    private const float ButtonGap = 2f;
    private static readonly Color TaggedColour = new Color(1f, 1f, 1f, 1f);
    private static readonly Color UntaggedColour = new Color(1f, 1f, 1f, 0.35f);

    private InventoryWindow _window;
    private CanvasGroup _group;
    private Image _eye;
    private OpaqueBackground _background;
    private bool _hidden;
    private bool _dragging;

    private void Awake()
    {
        _window = GetComponent<InventoryWindow>();
        _group = GetComponent<CanvasGroup>();
        if (_group == null)
        {
            _group = gameObject.AddComponent<CanvasGroup>();
        }

        _eye = EyeButton.Create(_window, OnEyeClicked);
        _background = new OpaqueBackground(GetComponent<Image>());
    }

    private void OnDestroy()
    {
        Show();
        _background?.Restore();
    }

    private void OnEyeClicked()
    {
        InventoryPeekPlugin.Instance?.Toggle(PeekState.KeyOf(_window));
    }

    private void LateUpdate()
    {
        InventoryPeekPlugin plugin = InventoryPeekPlugin.Instance;
        if (plugin == null || _window == null)
        {
            return;
        }

        FollowOpaqueSetting(plugin.Enabled && plugin.OpaqueWindows);

        bool tagged = plugin.IsHidable(PeekState.KeyOf(_window));
        if (_eye != null)
        {
            _eye.color = tagged ? TaggedColour : UntaggedColour;
        }

        bool peek = PeekState.PeekKeyHeld || plugin.Latched;
        // A drag that started while peeking keeps the window up until the button is let go.
        _dragging = !_hidden && Input.GetMouseButton(0) && (peek || _dragging);
        bool hide = plugin.Enabled && tagged && !peek && !_dragging;
        if (hide)
        {
            Hide(plugin.HiddenOpacity);
        }
        else if (_hidden)
        {
            Show();
        }
    }

    // Applied or restored only when the setting changes, never re-applied while it holds.
    private void FollowOpaqueSetting(bool opaque)
    {
        if (opaque == _background.Applied)
        {
            return;
        }

        if (opaque)
        {
            _background.Apply();
        }
        else
        {
            _background.Restore();
        }
    }

    private void Hide(float opacity)
    {
        _hidden = true;
        _group.alpha = opacity;
        _group.blocksRaycasts = false;
        _group.interactable = false;
    }

    private void Show()
    {
        _hidden = false;
        if (_group == null)
        {
            return;
        }

        _group.alpha = ShownAlpha;
        _group.blocksRaycasts = true;
        _group.interactable = true;
    }

    /// <summary>The eye button on the window's title bar, placed beside the game's sort and dock buttons.</summary>
    private static class EyeButton
    {
        private const int Width = 32;
        private const int Height = 20;
        private static Sprite _sprite;

        internal static Image Create(InventoryWindow window, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform sort = window.ButtonSort != null ? window.ButtonSort.transform as RectTransform : null;
            if (sort == null || sort.parent == null)
            {
                return null;
            }

            GameObject button = new GameObject("InventoryPeekEye", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)button.transform;
            rect.SetParent(sort.parent, false);
            rect.anchorMin = sort.anchorMin;
            rect.anchorMax = sort.anchorMax;
            rect.pivot = sort.pivot;
            Vector2 size = sort.rect.size;
            rect.sizeDelta = size;

            if (sort.parent.GetComponent<LayoutGroup>() != null)
            {
                // A laid-out title bar places us itself; take the slot just before the sort button.
                rect.SetSiblingIndex(sort.GetSiblingIndex());
                LayoutElement layout = button.AddComponent<LayoutElement>();
                layout.preferredWidth = size.x;
                layout.preferredHeight = size.y;
            }
            else
            {
                float left = sort.anchoredPosition.x;
                RectTransform dock = window.ButtonDock != null ? window.ButtonDock.transform as RectTransform : null;
                if (dock != null && dock.parent == sort.parent)
                {
                    left = Mathf.Min(left, dock.anchoredPosition.x);
                }

                rect.anchoredPosition = new Vector2(left - size.x - ButtonGap, sort.anchoredPosition.y);
            }

            Image image = button.GetComponent<Image>();
            image.sprite = EyeSprite();
            image.preserveAspect = true;
            image.color = UntaggedColour;
            button.GetComponent<Button>().onClick.AddListener(onClick);
            return image;
        }

        /// <summary>An eye drawn once in code: an almond outline with a round pupil, white on clear.</summary>
        private static Sprite EyeSprite()
        {
            if (_sprite != null)
            {
                return _sprite;
            }

            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            Color clear = new Color(1f, 1f, 1f, 0f);
            float cx = (Width - 1) / 2f;
            float cy = (Height - 1) / 2f;
            float halfWidth = Width / 2f - 1f;
            float halfHeight = Height / 2f - 1.5f;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float u = (x - cx) / halfWidth;
                    float lid = halfHeight * (1f - u * u);
                    float dy = Mathf.Abs(y - cy);
                    bool outline = Mathf.Abs(u) <= 1f && Mathf.Abs(dy - lid) < 1.2f;
                    bool pupil = (x - cx) * (x - cx) + (y - cy) * (y - cy) <= 16f;
                    texture.SetPixel(x, y, outline || pupil ? Color.white : clear);
                }
            }

            texture.Apply();
            _sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f));
            return _sprite;
        }
    }
}
