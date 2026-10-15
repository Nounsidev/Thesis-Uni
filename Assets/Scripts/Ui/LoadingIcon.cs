using UnityEngine;

public class LoadingIcon : MonoBehaviour
{
    public static LoadingIcon Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
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
