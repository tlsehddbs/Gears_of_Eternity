using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class ShopGoldTextViewer : MonoBehaviour
{
    [SerializeField] private PlayerState playerState;

    private void Awake()
    {
        playerState = PlayerState.Instance;
        
        GetComponent<TMP_Text>().text = playerState.Gold.ToString();
    }

    private void OnEnable()
    {
        playerState.OnGoldChanged += HandleGoldText;
    }
    
    private void OnDisable()
    {
        playerState.OnGoldChanged -= HandleGoldText;
    }


    private void HandleGoldText(int gold)
    {
        GetComponent<TMP_Text>().text = gold.ToString();
    }
}
