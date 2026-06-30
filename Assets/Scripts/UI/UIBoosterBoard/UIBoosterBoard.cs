using UnityEngine;

public class UIBoosterBoard : MonoBehaviour
{
    public float GetHeightInWorldUnits()
    {
        Vector3[] corners = new Vector3[4];
        GetComponent<RectTransform>().GetWorldCorners(corners);
        return corners[1].y - corners[0].y;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
