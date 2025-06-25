using System.Collections;
using System.Collections.Generic;
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

    int _energy = 100;
    int _coin = 0;
    float _dots = 0;

    void Start()
    {
        _coin = 0; //Random.Range(0, 100);
        _energy = 100; //Random.Range(50, 100);
        _dots = 0; //0.01f;

        _coinText.text = $"{_coin}";
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
        _coin = 0;
        _dots = 0;

        _coinText.text = $"{_coin}";
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
        _energyText.text = $"{_energy}%";
    }

    public void UpdateCoinAmount(int coin)
    {
        _coin += coin;
        _coinText.text = $"{_coin}";
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
}
