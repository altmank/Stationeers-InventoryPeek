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

    private InventoryWindow _window;
    private CanvasGroup _group;
    private EyeButton _eye;
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
            _eye.Tagged = tagged;
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
}
