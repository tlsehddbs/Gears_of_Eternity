using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ShopQuit : MonoBehaviour
{
    private Button _button;
    
    private void Awake()
    {
        _button = GameObject.Find("QuitButton").GetComponent<Button>();
        
        _button.onClick.AddListener(RequestStageEnd);
    }
    
    private async void RequestStageEnd()
    {
        try
        {
            await StageFlow.Instance.OnStageEnd();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
