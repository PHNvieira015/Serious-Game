using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string header;
    public string content;

    private float delay = 0.5f;
    private float timer;
    private bool isHovering;
    private bool tooltipShown;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        tooltipShown = false;
        timer = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        timer = 0f;
        tooltipShown = false;

        TooltipSystem.Hide();
    }

    private void Update()
    {
        if (!isHovering || tooltipShown)
            return;

        timer += Time.deltaTime;

        if (timer >= delay)
        {
            TooltipSystem.Show(content, header);
            tooltipShown = true;
        }
    }
}
