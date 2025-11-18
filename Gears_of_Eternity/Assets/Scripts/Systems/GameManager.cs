using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Data")] public IGetPlayerProgress PlayerProgress;

    
    //public bool isDrawingCards;
    [HideInInspector]
    public bool isDraggingCard;
    [HideInInspector]
    public bool isPointerEventEnabled = true;
    

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if(PlayerProgress == null)
                PlayerProgress = GetComponent<IGetPlayerProgress>();

            var flow = FindAnyObjectByType<StageFlow>();
            if (flow != null)
            {
                flow.playerProgress = PlayerProgress;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
