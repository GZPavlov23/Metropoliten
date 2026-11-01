using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A setting with a list of answers, stepped through with the left and right keys, Enter,
// or a click on either side of the answer.
public class MenuChoice : Selectable, IPointerClickHandler, ISubmitHandler
{
    public MenuGlow glow;

    [Tooltip("The part of the line that shows the answer.")]
    public RectTransform answerArea;

    public TMPro.TMP_Text answerText;

    public string[] answers;
    public Action<int> onChanged;

    public int Index { get; private set; }

    public void Show(int index)
    {
        Index = index;
        answerText.text = answers[Index];
    }

    public void Step(int direction)
    {
        Show((Index + direction + answers.Length) % answers.Length);
        onChanged?.Invoke(Index);
        GameMenu.Tick();
    }

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

    public override void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Left) Step(-1);
        else if (eventData.moveDir == MoveDirection.Right) Step(1);
        else base.OnMove(eventData);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Step(1);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(answerArea, eventData.position, eventData.pressEventCamera, out Vector2 point)) return;
        if (!answerArea.rect.Contains(point)) return;
        Step(point.x < answerArea.rect.center.x ? -1 : 1);
    }
}
