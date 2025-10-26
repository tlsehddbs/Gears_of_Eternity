using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardCollection : MonoBehaviour
{
    
    public static CardCollection Instance;
    
    public List<UnitCardData> allAvailableCards;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        LoadCardsFromResources();
    }

    // 유닛 업그레이드 기능을 어떻게 구현할지, 모든 유닛을 가져올 필요가 있는지, 그렇지 않다면 런타임 유닛의 정보를 어떻게 수정하여 리소스를 줄일 수 있을지 고민해봐야 할 듯 
    void LoadCardsFromResources()
    {
        allAvailableCards = Resources.LoadAll<UnitCardData>("UnitCardAssets").ToList();
    }
}
