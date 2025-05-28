using UnityEngine;

public class HandPanelManager : MonoBehaviour
{
    public Transform handPanel;
    public GameObject cardPrefab;

    public void RefreshHandUI()
    {
        if (handPanel.childCount > 0)
        {
            foreach (Transform child in handPanel)
                Destroy(child.gameObject);
        }

        foreach (var card in DeckManager.Instance.hand)
        {
            var go = Instantiate(cardPrefab, handPanel);
            go.GetComponent<CardSlotUI>().Initialize(card);
        }
    }
}
