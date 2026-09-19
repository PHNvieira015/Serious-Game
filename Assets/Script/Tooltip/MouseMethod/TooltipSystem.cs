using UnityEngine;

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem instance;

    public Tooltip tooltip;

    private void Awake()
    {
        instance = this;

        if (tooltip != null)
        {
            tooltip.gameObject.SetActive(false);
        }
    }

    public static void Show(string content, string header = "")
    {
        if (instance == null)
        {
            Debug.LogWarning("TooltipSystem instance not found!");
            return;
        }

        instance.tooltip.SetText(content, header);
        instance.tooltip.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        if (instance == null)
        {
            return;
        }

        instance.tooltip.gameObject.SetActive(false);
    }
}
