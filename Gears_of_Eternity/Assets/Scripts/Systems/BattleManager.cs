using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    
    public bool isBattleRunning = false;

    private void Start()
    {
        Invoke("StartBattle", 1f);
    }

    public void StartBattle()
    {
        isBattleRunning = true;
        StartCoroutine(CardDrawCoolDown());
    }

    public void EndBattle()
    {
        isBattleRunning = false;
        StopCoroutine(CardDrawCoolDown());
    }

    // ReSharper disable Unity.PerformanceAnalysis
    IEnumerator CardDrawCoolDown()
    {
        while (isBattleRunning)
        {
            DeckManager.Instance.DrawCards(4);
            yield return new WaitForSeconds(10f);
        }
    }
}
