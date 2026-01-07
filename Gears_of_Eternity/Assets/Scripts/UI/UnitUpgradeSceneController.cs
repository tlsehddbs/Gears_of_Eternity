using UnityEngine;

public class UnitUpgradeSceneController : MonoBehaviour
{
    [Header("PlayerState")] 
    [SerializeField] private PlayerState playerState;

    [Header("UI Anchors")] 
    [SerializeField] private RectTransform currentSlot;     // 현재 카드
    [SerializeField] private RectTransform optionRoot;      // 업그레이드 옵션 카드 배치 기준점(루트)
    [SerializeField] private TMPro.TMP_Text guideText;      // 안내용 문구
    [SerializeField] private GameObject noUpgradePanel;     // 업그레이드 불가 시 표시

    [Header("Layout")] 
    [SerializeField] private float columnSpacing = 40f;     // 카드 간 간격
    [SerializeField] private int maxPreRow = 4;
    [SerializeField] private float rowSpacing = 28f;
    

}
