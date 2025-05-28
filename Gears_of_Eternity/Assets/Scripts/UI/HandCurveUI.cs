using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HandCurveUI : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform handPanel;
    public float radius = 300f;
    public float angleRange = 20f; // 전체 호 각도
    public float cardSpacingAngle = 10f;

    private List<GameObject> cardInstances = new();

    public void RefreshHandUI(List<RuntimeUnitCard> hand)
    {
        foreach (var card in cardInstances)
            Destroy(card);
        cardInstances.Clear();

        int count = hand.Count;

        // 카드 개수에 따라 펼침 각도 조정
        float spacingAngle = 15f; // 카드 사이 각도 간격
        float angleRange = spacingAngle * (count - 1); // 전체 부채꼴 각도
        float startAngle = -angleRange / 2f;
        float radius = Mathf.Lerp(300f, 500f, Mathf.InverseLerp(1f, 10f, count)); // 카드 수에 비례한 반지름

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + spacingAngle * i;
            float rad = Mathf.Deg2Rad * angle;

            Vector2 pos = new Vector2(
                Mathf.Sin(rad) * radius,
                Mathf.Cos(rad) * radius * 0.3f
            );

            var card = Instantiate(cardPrefab, handPanel);
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;

            card.GetComponent<CardSlotUI>().Initialize(hand[i]);

            card.transform.DOLocalMove(pos, 0.5f).SetEase(Ease.OutExpo);
            
            float angleScale = 0.4f; // 회전 강도 계수 (0.0 ~ 1.0)
            float limitedAngle = -angle * angleScale;

            card.transform.DOLocalRotate(Vector3.forward * limitedAngle, 0.5f).SetEase(Ease.OutQuad);

            cardInstances.Add(card);
        }
    }
}
