using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool isDrawingCards;
    public bool isDraggingCard;
    
    public bool isInteractable = true;
    
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

    // private void Start()
    // {
    //     isInteractable = true;
    // }
}
