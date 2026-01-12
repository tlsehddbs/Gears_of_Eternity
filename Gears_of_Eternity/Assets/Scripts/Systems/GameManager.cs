using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    //public bool isDrawingCards;
    [HideInInspector]
    public bool isDraggingCard;
    [HideInInspector]
    public bool isPointerEventEnabled = true;

    public float combatTime = 360f;
    public int maxCost = 10;
    

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

    private void Start()
    {
        if (PlayerState.Instance.DeckCards.Count == 0)
        {
            PlayerState.Instance.GenerateStarterDeck(CardCollection.Instance);
        }
    }
}
