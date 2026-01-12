using System;
using UnityEngine;
using UnityEngine.UI;

public class CardSlotUI : MonoBehaviour
{
    public Text cardNameText;
    public Text cardDescriptionText;

    private RuntimeUnitCardRef _ref;

    private void Awake()
    {
        _ref = GetComponent<RuntimeUnitCardRef>();
        if (_ref != null)
        {
            _ref.OnCardChanged += Apply;
            if (_ref.Card != null)
            {
                Apply(_ref.Card);
            }
        }
    }

    private void OnDestroy()
    {
        if (_ref != null)
        {
            _ref.OnCardChanged -= Apply;
        }
    }

    public void Apply(RuntimeUnitCard data)
    {
        if (cardNameText != null)
        {
            cardNameText.text = data.unitName;
        }

        if (cardDescriptionText != null)
        {
            cardDescriptionText.text = data.unitDescription;
        }
    }
}