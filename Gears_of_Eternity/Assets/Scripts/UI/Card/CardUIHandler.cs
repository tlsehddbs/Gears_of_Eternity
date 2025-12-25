using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class CardUIHandler : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform handPanel;

    public List<GameObject> cardInstances = new();
    
    
    // 제거된 카드 파악 및 제거
    public void RemoveCards(IReadOnlyList<RuntimeUnitCard> hand)
    {
        for (int i = cardInstances.Count - 1; i >= 0; i--)
        {
            var cardIndex = cardInstances[i];
            var cardData = cardIndex.GetComponent<CardSlotUI>().CardData;

            bool stillExists = hand.Any(c => c.uniqueId == cardData.uniqueId);

            if (!stillExists)
            {
                Destroy(cardIndex);
                cardInstances.RemoveAt(i);
            }
        }
        UpdateCardLayout();
    }

    // 새로 생긴 카드 파악 및 생성
    public void AddCards(IReadOnlyList<RuntimeUnitCard> hand)
    {
        foreach (var newCard in hand)
        {
            bool alreadyExists = cardInstances.Any(go => go.GetComponent<CardSlotUI>().CardData.uniqueId == newCard.uniqueId);

            if (!alreadyExists)
            {
                var go = Instantiate(cardPrefab, handPanel);
                go.GetComponent<RectTransform>().localPosition = new Vector3(0, -250f, 0);
                go.GetComponent<CardSlotUI>().Initialize(newCard);
                cardInstances.Add(go);
            }
        }
        UpdateCardLayout();
    }

    // TODO: 카드 레이아웃을 계산하는 부분과 UI에 실질 적용하는 두 개의 함수로 분리하여 적용할 것 (배치 실패 시 불필요한 계산을 줄이기 위함)
    public void UpdateCardLayout()
    {
        GameManager.Instance.isPointerEventEnabled = false;
        int count = cardInstances.Count;

        // 카드 개수에 따라 펼침 각도 조정
        float spacingAngle = 15f;                           // 카드 사이 각도 간격
        float angleRange = spacingAngle * (count - 1);      // 전체 부채꼴 각도
        float startAngle = -angleRange / 2f;
        float radius = Mathf.Lerp(300f, 500f, Mathf.InverseLerp(1f, 10f, count));   // 반지름이 카드 수에 비례하도록  
        
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + spacingAngle * i;
            float rad = Mathf.Deg2Rad * angle;

            Vector2 pos = new Vector2(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius * 0.3f);
            
            float delay = i * 0.02f;
            float angleScale = 0.4f; // 회전 강도 계수 (0.0 ~ 1.0)
            float limitedAngle = -angle * angleScale;
            
            var card = cardInstances[i];
            var cardHandler = card.GetComponent<CardUIManager>();
            
            cardHandler.UpdateOriginalTransform(pos);
            cardHandler.OnPointerExitEffect(true);
            
            Tween s = DOTween.Sequence()
                .AppendInterval(delay)
                .Join(card.transform.DOLocalMove(pos, 0.5f).SetEase(Ease.OutExpo))
                .Join(card.transform.DOLocalRotate(Vector3.forward * limitedAngle, 0.5f).SetEase(Ease.OutQuad));

            if (i == count - 1)
            {
                s.OnComplete(() => GameManager.Instance.isPointerEventEnabled = true);
            }
        }
    }
}
