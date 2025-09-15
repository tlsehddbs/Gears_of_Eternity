using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // TODO: 네이밍 수정할 것
    public bool isDrawingCards;
    public bool isDraggingCard;
    
    public bool isPointerEventEnabled = true;
    

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
