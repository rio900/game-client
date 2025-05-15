using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipEntity : MonoBehaviour
{
    public string Id { get; set; }

    Vector3 _from;
    Vector3 _to;
    int _blockDifference;

    float _travelDuration;
    float _launchTime;

    public void Lounch(Vector3 from, Vector3 to, int currentBlock, int destinationBlock)
    {
        if (_to == to) return;

        if (_from == Vector3.zero)
        {
            transform.position = from;
        }

        _blockDifference = destinationBlock - currentBlock; // 2s block
        _from = from;
        _to = to;

        _travelDuration = Mathf.Abs(_blockDifference) * 2f;
        _launchTime = Time.time;

        Debug.Log($"[ShipEntity] Lounch ship from {from} to {to} with duration {_travelDuration} seconds {destinationBlock} - {currentBlock} = {_blockDifference}");
    }


    private void Update()
    {
        if (_to != Vector3.zero)
        {
            float elapsed = Time.time - _launchTime;

            if (elapsed >= _travelDuration)
            {
                transform.position = _to;

                // Reset
                _from = Vector3.zero;
                _to = Vector3.zero;
            }
            else
            {
                float t = elapsed / _travelDuration;
                transform.position = Vector3.Lerp(_from, _to, t);
            }
        }
    }
}
