using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A setting with a range. The whole line highlights, but only the bar itself can be grabbed,
// so that clicking on the name of a setting doesn't change it.
public class MenuSlider : Slider
{
    public MenuGlow glow;

    [Tooltip("The part of the line the mouse can grab.")]
    public RectTransform grabArea;

    bool grabbed;

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        if (glow != null) glow.SetSelected(state != SelectionState.Normal && state != SelectionState.Disabled);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        Select();
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        GameMenu.Tick();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        grabbed = RectTransformUtility.RectangleContainsScreenPoint(grabArea, eventData.position, eventData.pressEventCamera);
        if (grabbed) base.OnPointerDown(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (grabbed) base.OnDrag(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        grabbed = false;
        base.OnPointerUp(eventData);
    }
}
