using System.Collections.Generic;
using System.Linq;
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

    void Start()
    {
        if (drawButton != null) drawButton.onClick.AddListener(OnDrawClick);
        else Debug.LogError("drawButton is not assigned.");

        if (useButton != null) useButton.onClick.AddListener(OnUseClick);
        else Debug.LogError("useButton is not assigned.");

        // if (debugButton != null) debugButton.onClick.AddListener(OnDebugClick);
        // else Debug.LogError("debugButton is not assigned.");
        
        handPanelManager.RefreshHandUI(DeckManager.Instance.hand);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("🔸 Space 눌림 - 강제 드로우 실행");
            OnDrawClick();
        }
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    public void OnDrawClick()
    {
        DeckManager.Instance.DrawCards(1);
        handPanelManager.RefreshHandUI(DeckManager.Instance.hand);
    }

    public void OnUseClick()
    {
        if (DeckManager.Instance.hand.Count > 0)
        {
            int testRandHand = Random.Range(0, DeckManager.Instance.hand.Count);
            var card = DeckManager.Instance.hand[testRandHand];
            DeckManager.Instance.UseCard(card);
        }
        handPanelManager.RefreshHandUI(DeckManager.Instance.hand);
    }
}