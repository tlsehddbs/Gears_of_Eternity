using System;
using RarityTypes.Enums;
using UnityEngine;
using UnityEngine.UI;

public class CardFrameUI : MonoBehaviour
{
    [SerializeField] private Image _image;
    
    [Header("Frame Sprites")] 
    [SerializeField] private Sprite brassFrame;
    [SerializeField] private Sprite nickelFrame;
    [SerializeField] private Sprite goldFrame;
    

    public void Apply(Rarity rarity)
    {
        if (_image == null)
        {
            return;
        }

        _image.sprite = rarity switch
        {
            Rarity.Common => brassFrame,
            Rarity.Rare => nickelFrame,
            Rarity.Legendary => goldFrame,
            _ => brassFrame
        };
    }
}
