using UnityEngine;
using UnityEngine.UI;

public class DeckTestUI : MonoBehaviour
{
    public Text logText;
    
    public Button drawButton;
    public Button useButton;
    public Button debugButton;
    
    [SerializeField]
    public HandCurveUI handPanelManager;

    //
    //
    //
    //
    // TODO: 추후 test 파일을 정리하면서 여기(decktest)에 있는 로직을 다른 파일로 병합할 예정 -> deckManager 또는 별도 신규 파일 등
    //
    //
    //
    
    void Start()
    {
        if (drawButton != null) drawButton.onClick.AddListener(OnDrawClick);
        else Debug.LogError("drawButton is not assigned.");

        // if (useButton != null) useButton.onClick.AddListener(OnUseClick);
        // else Debug.LogError("useButton is not assigned.");

        // if (debugButton != null) debugButton.onClick.AddListener(OnDebugClick);
        // else Debug.LogError("debugButton is not assigned.");
        
        handPanelManager.RefreshHandUI(DeckManager.Instance.hand);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !GameManager.Instance.isDraggingCard)
        {
            Debug.Log("🔸 Space 눌림 - 강제 Draw 실행");
            OnDrawClick();
        }
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void OnDrawClick()
    {
        DeckManager.Instance.DrawCards(1);
        handPanelManager.RefreshHandUI(DeckManager.Instance.hand);
    }

    // private void OnUseClick()
    // {
    //     if (DeckManager.Instance.hand.Count > 0)
    //     {
    //         int testRandHand = Random.Range(0, DeckManager.Instance.hand.Count);
    //         var card = DeckManager.Instance.hand[testRandHand];
    //         DeckManager.Instance.UseCard(card);
    //     }
    //     handPanelManager.RefreshHandUI(DeckManager.Instance.hand);
    // }
}