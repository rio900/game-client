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

    public void SetId(int id)
    {
        _asteroidObjects.ForEach(p => p.SetActive(false));

        // NFT
        if (id % 20 == 0)
        {
            _typeId = 3;
            _asteroidObjects[3].SetActive(true);
            Debug.Log($"NFT {_typeId} position {transform.position}");
        }
        else if (id % 14 == 0)
        {
            _typeId = 4;
            _asteroidObjects[4].SetActive(true);
            Debug.Log($"NFT {_typeId} position {transform.position}");

        }
        else if (id % 8 == 0)
        {
            _typeId = 5;
            _asteroidObjects[5].SetActive(true);
            Debug.Log($"NFT {_typeId} position {transform.position}");

        }
        else if (id % 2 == 0)
        {
            _typeId = 0;
            _asteroidObjects[0].SetActive(true);
        }
        else if (id % 3 == 0)
        {
            _typeId = 2;
            _asteroidObjects[2].SetActive(true);
        }

        //--------
        else
        {
            _typeId = 1;
            _asteroidObjects[1].SetActive(true);
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
