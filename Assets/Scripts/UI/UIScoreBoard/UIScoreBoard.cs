using UnityEngine;

public class UIScoreBoard : MonoBehaviour
{
    public float GetHeight()
    {
        //TODO: Replace with actual logic later
        return GetComponent<RectTransform>().rect.height;
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
