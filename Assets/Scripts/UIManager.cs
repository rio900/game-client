using System;
using System.Collections;
using System.Collections.Generic;
using DotStriker.NetApiExt.Generated.Model.pallet_template;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    TMP_Text _energyText;

    [SerializeField]
    TMP_Text _coinText;

    [SerializeField]
    TMP_Text _dotText;

    [SerializeField]
    GameObject _collectScreenEffect;

    [SerializeField]
    GameObject _restartPanel;

    [SerializeField]
    List<UINftItem> _nftItems;

    [SerializeField]
    GameObject _startGamePanel;

    [SerializeField]
    TMP_Text _playerCountText;

    int _energy = 100;
    int _coins = 0;
    float _dots = 0;

    void Start()
    {
        _coinText.text = $"{_coins}";
        _energyText.text = $"{_energy}%";
        _dotText.text = $"{_dots}";
    }

    public void UpdateEnergy()
    {
        _energy -= 4;
        if (_energy < 0)
        {
            _restartPanel.SetActive(true);
            Time.timeScale = 0;
        }
        _energyText.text = $"{_energy}%";
    }

    public void Reset()
    {
        _energy = 100;
        _coins = 0;
        _dots = 0;

        _coinText.text = $"{_coins}";
        _energyText.text = $"{_energy}%";
        _dotText.text = $"{_dots}";

        Time.timeScale = 1;
        _restartPanel.SetActive(false);
    }

    public void AddEnergy(int energy)
    {
        _energy += energy;
        if (_energy > 100)
        {
            _energy = 100;
        }
        SetEnergy(energy);
    }

    public void SetEnergy(int energy)
    {
        _energy = energy;
        if (_energy > 100)
        {
            _energy = 100;
        }
        _energyText.text = $"{_energy}%";
    }

    public void SetCoins(int coins)
    {
        _coins = coins;
        _coinText.text = $"{_coins}";
    }

    public void SetDots(int dots)
    {
        _dots = dots;
        _dotText.text = $"{_dots}";
    }

    public void SetPlayerCount(int count)
    {
        _playerCountText.text = $"Player count: {count}";
    }

    public void UpdateCoinAmount(int coin)
    {
        _coins += coin;
        SetCoins(_coins);
    }

    public void SetNftCount(AsteroidKind item, int count)
    {
        foreach (var nftItem in _nftItems)
        {
            Debug.Log($"Checking NFT item: {nftItem.NftType} against {item}");
            if (nftItem.NftType == item)
            {
                nftItem.SetCount(count);
                return;
            }
        }
        Debug.LogWarning($"Nft item of type {item} not found.");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CollectScreenEffect()
    {
        _collectScreenEffect.SetActive(true);
        StartCoroutine(DisableCollectScreenEffect());
    }

    private IEnumerator DisableCollectScreenEffect()
    {
        yield return new WaitForSeconds(1f);
        _collectScreenEffect.SetActive(false);
    }

    internal void UpdateDots(float val)
    {
        _dots += val;

        _dotText.text = _dots.ToString("0.#");
    }

    internal void SetResource(AsteroidKind item, int v)
    {
        switch (item)
        {
            case AsteroidKind.Gold:
                SetCoins(v);
                break;
            case AsteroidKind.Dot0:
                SetDots(v);
                break;
        }
    }

    internal void ShowStartGamePanel()
    {
        _startGamePanel.SetActive(true);
    }

    public AsteroidKind? GetSelectedNftItems()
    {
        foreach (var nftItem in _nftItems)
        {
            if (nftItem.Selected && nftItem.Count > 0)
                return nftItem.NftType;
        }

        return null;
    }
}
