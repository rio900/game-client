using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidView : MonoBehaviour
{
    [SerializeField]
    GameObject _collectEffectPrefab;

    [SerializeField]
    GameObject _nftEffectPrefab;


    [SerializeField]
    List<GameObject> _asteroidObjects;

    int _typeId;
    public int TypeId => _typeId;


    void Start()
    {
    }

    public void SetId(int kind)
    {
        _asteroidObjects.ForEach(p => p.SetActive(false));

        _typeId = kind;

        if (_typeId >= 0 && _typeId < _asteroidObjects.Count)
        {
            _asteroidObjects[_typeId].SetActive(true);
            Debug.Log($"[AsteroidView] Set type: {_typeId} ({kind}) at position {transform.position}");
        }
        else
        {
            Debug.LogWarning($"[AsteroidView] Unknown asteroid kind: {_typeId} ({kind})");
        }
    }

    internal void Collect()
    {
        var collectEffect = Instantiate(_collectEffectPrefab);
        collectEffect.transform.position = transform.position;
        Destroy(collectEffect, 1f);

        if (TypeId > 2)
        {
            var nftEffect = Instantiate(_nftEffectPrefab);
            nftEffect.transform.position = transform.position;
            Destroy(nftEffect, 4f);
        }
    }


}
