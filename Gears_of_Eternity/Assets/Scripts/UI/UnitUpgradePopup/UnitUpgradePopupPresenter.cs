using System;
using Unity.VisualScripting;
using UnityEngine;

public class UnitUpgradePopupPresenter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UIPopupAnimator popupAnimator;
    [SerializeField] private UnitUpgradeSceneController upgradeController;

    private bool _closing;
    private bool _shownOnce;

    private void Awake()
    {
        if (popupAnimator == null)
        {
            popupAnimator = GetComponentInChildren<UIPopupAnimator>(true);
        }

        if (upgradeController == null)
        {
            upgradeController = GetComponentInChildren<UnitUpgradeSceneController>(true);
        }
    }

    private void OnEnable()
    {
        if (upgradeController != null)
        {
            upgradeController.CloseRequested += HandleCloseRequested;
            upgradeController.UpgradeApplied += HandleUpgradeApplied;
        }
    }

    private void OnDisable()
    {
        if (upgradeController != null)
        {
            upgradeController.CloseRequested -= HandleCloseRequested;
            upgradeController.UpgradeApplied -= HandleUpgradeApplied;
        }
    }

    public void ShowWithRandomPickedCard()
    {
        if (upgradeController == null)
        {
            Debug.LogWarning("[UnitUpgradeSceneController] Missing upgradeController.");
            return;
        }
        
        upgradeController.OpenRandom();
        ShowIfNeeded();
    }

    public void ShowWithTargetCard(RuntimeUnitCard target)
    {
        if (upgradeController == null)
        {
            Debug.LogWarning("[UnitUpgradeSceneController] Missing upgradeController.");
            return;
        }
        
        upgradeController.OpenForTarget(target);
        ShowIfNeeded();
    }

    private void ShowIfNeeded()
    {
        if (_shownOnce)
        {
            return;
        }
        
        _shownOnce = true;
        popupAnimator?.Show();
    }

    private void HandleUpgradeApplied()
    {
        if (StageFlow.Instance.CurrentStageDef.type == StageTypes.StageNodeTypes.Shop)
        {
            ClosePopup();
        }
        else
        {
            ClosePopupAndEndStage();
        }
    }

    private void HandleCloseRequested()
    {
        if (StageFlow.Instance.CurrentStageDef.type == StageTypes.StageNodeTypes.Shop)
        {
            ClosePopup();
        }
        else
        {
            ClosePopupAndEndStage();
        }
    }
    
    private void ClosePopup()
    {
        if (_closing)
        {
            return;
        }
        _closing = true;

        if (popupAnimator == null)
        {
            return;
        }
        
        popupAnimator.Hide(() => Destroy(gameObject));
    }

    private void ClosePopupAndEndStage()
    {
        if (_closing)
        {
            return;
        }
        _closing = true;

        if (popupAnimator == null)
        {
            EndStage();
            return;
        }
        
        PlayerState.Instance.AddLife();     // Rest Scene에서는 기본적으로 체력이 1씩 회복되도록 설정
        PlayerState.Instance.ResetUpgradeCount();
        
        popupAnimator.Hide(EndStage);
    }

    private async void EndStage()
    {
        try
        {
            await StageFlow.Instance.OnStageEnd();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        // finally
        // {
        //     Destroy(gameObject);
        // }
    }
}
