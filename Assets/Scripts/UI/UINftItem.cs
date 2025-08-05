using System.Collections;
using System.Collections.Generic;
using DotStriker.NetApiExt.Generated.Model.pallet_dot_striker;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UINftItem : MonoBehaviour
{
    [SerializeField]
    AsteroidKind _asteroidKind;

    [SerializeField]
    bool _isDefault;

    [SerializeField]
    TMP_Text _countText;

    [SerializeField]
    Toggle _toggle;

    int _count;
    public int Count => _count;

    public bool IsDefault => _isDefault;
    public bool Selected
    {
        get => _toggle.isOn;
        set => _toggle.isOn = value;
    }
    public AsteroidKind NftType => _asteroidKind;

    void Start()
    {

    }

    public void SetCount(int count)
    {
        _count = count;
        _countText.text = count.ToString();
        if (IsDefault) return;

        if (_count <= 0)
        {
            _toggle.interactable = false;
            _countText.text = "0";
        }
        else
        {
            _toggle.interactable = true;
        }
    }
}
