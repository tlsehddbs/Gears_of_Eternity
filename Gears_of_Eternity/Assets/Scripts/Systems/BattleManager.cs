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
    }

    public void StartBattle()
    {
    }

    public void EndBattle()
    {
    }
}
