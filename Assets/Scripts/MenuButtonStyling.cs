using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonStyling : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponentInChildren<TextMeshProUGUI>().color = new Color(210 / 256f, 210 / 256f, 210 / 256f); ;
    }
}
