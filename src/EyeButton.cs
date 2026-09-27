using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventoryPeek;

/// <summary>
/// The eye button on a window's title bar, made from a copy of the window's own sort button so it gets the same
/// layout cell, image and raycast setup. The copy's animator and game scripts are removed (the animator would put
/// the sort icon back) and the states the game's button animator plays are reproduced here: under the pointer the
/// highlighted tile at 1.1 scale, pressed the highlighted tile at half alpha. Also the eye's tooltip.
/// </summary>
internal sealed class EyeButton : ToolTipBase, IPointerDownHandler, IPointerUpHandler
{
    private const float ButtonGap = 3f;
    private const float HoverScale = 1.1f;
    private const float PressedAlpha = 0.5f;
    private const string TaggedTooltip =
        "Inventory Peek: this window hides unless Mouse Control (Alt) is held. Click to always show it.";
    private const string UntaggedTooltip =
        "Inventory Peek: this window is always shown. Click to hide it unless Mouse Control (Alt) is held.";

    private Image _image;
    private EyeSprites _sprites;
    private bool _tagged;
    private bool _inside;
    private bool _down;

    internal bool Tagged
    {
        set
        {
            if (_tagged != value)
            {
                _tagged = value;
                Refresh();
            }
        }
    }

    internal static EyeButton Create(InventoryWindow window, UnityAction onClick)
    {
        GameObject source = window.ButtonSort != null ? window.ButtonSort.gameObject : null;
        RectTransform sort = source != null ? source.transform as RectTransform : null;
        Image sourceImage = source != null ? source.GetComponent<Image>() : null;
        if (sort == null || sort.parent == null || sourceImage == null || sourceImage.sprite == null)
        {
            return null;
        }

        GameObject copy = Instantiate(source, sort.parent, false);
        copy.name = "InventoryPeekEye";
        foreach (UserInterfaceBase gameScript in copy.GetComponents<UserInterfaceBase>())
        {
            DestroyImmediate(gameScript);
        }

        Animator animator = copy.GetComponent<Animator>();
        if (animator != null)
        {
            DestroyImmediate(animator);
        }

        Button button = copy.GetComponent<Button>();
        if (button == null)
        {
            button = copy.AddComponent<Button>();
        }

        button.transition = Selectable.Transition.None;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);

        Place((RectTransform)copy.transform, sort, window);

        EyeButton eye = copy.AddComponent<EyeButton>();
        eye._image = copy.GetComponent<Image>();
        eye._image.preserveAspect = false;
        eye._sprites = EyeSprites.Of(sourceImage.sprite);
        eye.Refresh();
        copy.SetActive(true);
        return eye;
    }

    // A laid-out title bar (the game's is a grid of 30 px cells) places the copy itself, in the slot before the sort
    // button; otherwise it goes left of the sort and dock buttons.
    private static void Place(RectTransform rect, RectTransform sort, InventoryWindow window)
    {
        rect.localScale = Vector3.one;
        rect.SetSiblingIndex(sort.GetSiblingIndex());
        Vector2 size = sort.rect.size;
        if (sort.parent.GetComponent<LayoutGroup>() != null)
        {
            LayoutElement layout = rect.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = rect.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            return;
        }

        float left = sort.anchoredPosition.x;
        RectTransform dock = window.ButtonDock != null ? window.ButtonDock.transform as RectTransform : null;
        if (dock != null && dock.parent == sort.parent)
        {
            left = Mathf.Min(left, dock.anchoredPosition.x);
        }

        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(left - size.x - ButtonGap, sort.anchoredPosition.y);
    }

    public override string GetString() => _tagged ? TaggedTooltip : UntaggedTooltip;

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        _inside = true;
        Refresh();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        _inside = false;
        Refresh();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _down = true;
            Refresh();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _down = false;
            Refresh();
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        _inside = false;
        _down = false;
        Refresh();
    }

    private void Refresh()
    {
        if (_image == null || _sprites == null)
        {
            return;
        }

        bool pressed = _inside && _down;
        _image.sprite = _sprites.For(_tagged, _inside);
        _image.color = new Color(1f, 1f, 1f, pressed ? PressedAlpha : 1f);
        transform.localScale = _inside && !pressed ? Vector3.one * HoverScale : Vector3.one;
    }
}
