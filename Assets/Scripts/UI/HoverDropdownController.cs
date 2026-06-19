using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HoverDropdownController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private GameObject dropdown;
    [SerializeField] private bool hideOnStart = true;

    private void Awake()
    {
        if (hideOnStart)
            Hide();
    }

    private void OnEnable()
    {
        if (hideOnStart)
            Hide();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hide();
    }

    public void OnSelect(BaseEventData eventData)
    {
        Show();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Hide();
    }

    private void Show()
    {
        if (dropdown != null)
            dropdown.SetActive(true);
    }

    private void Hide()
    {
        if (dropdown != null)
            dropdown.SetActive(false);
    }
}
