using UnityEngine.EventSystems;
using UnityEngine.UI;

// A line of a menu that does something when pressed. The mouse and the keyboard move the same highlight.
public class MenuButton : Button
{
    public MenuGlow glow;

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
}
