using TMPro;
using UnityEngine;

public class GoldText : MonoBehaviour
{
    [SerializeField] private PlayerState playerState;

    private void Start()
    {
        playerState = PlayerState.Instance;
        
        GetComponent<TMP_Text>().text = playerState.Gold.ToString();
        
        playerState.OnGoldChanged += HandleGoldText;
    }

    // private void OnEnable()
    // {
    //     playerState.OnGoldChanged += HandleGoldText;
    // }
    
    private void OnDisable()
    {
        playerState.OnGoldChanged -= HandleGoldText;
    }


    private void HandleGoldText(int gold)
    {
        GetComponent<TMP_Text>().text = gold.ToString();
    }
}
