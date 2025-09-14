using UnityEngine;

public class DeckTestUI : MonoBehaviour
{
    [SerializeField] [Min(1)]
    private int testDrawCount;
    
    [SerializeField]
    public CardUIHandler cardUIHandler;

    // TODO: 추후 test 파일을 정리하면서 여기(decktest)에 있는 로직을 다른 파일로 병합할 예정 -> deckManager 또는 별도 신규 파일 등
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space) && !GameManager.Instance.isDraggingCard)
        {
            OnDrawClick();
        }
#endif
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void OnDrawClick()
    {
        DeckManager.Instance.DrawCards(testDrawCount);
    }
}