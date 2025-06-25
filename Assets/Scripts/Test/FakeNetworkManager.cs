using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class FakeNetworkManager : MonoBehaviour
{
    [SerializeField] TMP_Text _blockNumberText;
    [SerializeField] TMP_Text _updateNumberText;
    [SerializeField] GameObject _asteroidPrefab;
    [SerializeField] GameObject _starshipPrefab;
    [SerializeField] CameraController _cameraController;
    [SerializeField] EnergySlider _energySlider;

    [SerializeField] GameObject _callIndicatorPrefab;

    [SerializeField]
    UIManager _uiManager;

    int _blockNumber = 0;
    ulong _updateNumber = 0;

    // Координаты, где находятся астероиды и корабли
    Dictionary<long, AsteroidData> _asteroids = new();
    Dictionary<string, FlightData> _activeFlights = new();
    Dictionary<string, Vector2> _activeShips = new();
    Dictionary<string, ShipEntity> _shipsInSpace = new();

    class AsteroidData
    {
        public GameObject Instance;
        public Vector2 Position;
        public int TtlBlock;
    }

    class FlightData
    {
        public Vector2 From;
        public Vector2 To;
        public int EndBlock;
    }
    List<AsteroidView> _asteroidViews = new List<AsteroidView>();

    void Start()
    {
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 50; j++)
            {
                var callIndicator = Instantiate(_callIndicatorPrefab);
                callIndicator.transform.position = new Vector3(i, 0, j);
            }
        }

        InvokeRepeating(nameof(UpdateFakeWorld), 1f, 1f);
    }

    void UpdateFakeWorld()
    {
        _updateNumber++;
        _updateNumberText.text = $"Update number: {_updateNumber}";

        if (_updateNumber % 2 == 0)
            _blockNumber++;

        _blockNumberText.text = $"Block number: {_blockNumber}";

        UpdateAsteroids();
        CompleteFlights();
    }

    void UpdateAsteroids()
    {
        // Удаление астероидов, чей TTL истёк
        var toRemove = new List<long>();
        foreach (var kvp in _asteroids)
        {
            if (kvp.Value.TtlBlock < _blockNumber)
            {
                Destroy(kvp.Value.Instance);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var id in toRemove)
            _asteroids.Remove(id);

        // Добавление новых астероидов (до 10)
        int maxAsteroids = 80;
        int needed = maxAsteroids - _asteroids.Count;
        for (int i = 0; i < needed; i++)
        {
            long id = GenerateAsteroidId();
            Vector2 coord = new(Random.Range(0, 50), Random.Range(0, 50));
            int ttl = _blockNumber + 5 + i;

            var asteroidGO = Instantiate(_asteroidPrefab, new Vector3(coord.x, 0, coord.y), Quaternion.identity);
            asteroidGO.name = $"Asteroid_{id}";

            var asteroidView = asteroidGO.GetComponent<AsteroidView>();
            asteroidView.SetId((int)id);
            _asteroidViews.Add(asteroidView);

            _asteroids[id] = new AsteroidData
            {
                Instance = asteroidGO,
                Position = coord,
                TtlBlock = ttl
            };

            Debug.Log($"[FakeNetworkManager] Spawned asteroid {id} at {coord}");
        }
    }

    void CompleteFlights()
    {
        // Завершаем полеты, которые достигли целевого блока
        var completed = new List<string>();
        foreach (var kvp in _activeFlights)
        {
            if (kvp.Value.EndBlock <= _blockNumber)
            {
                _activeShips[kvp.Key] = kvp.Value.To;
                completed.Add(kvp.Key);

                Debug.Log($"[FakeNetworkManager] Flight completed for {kvp.Key}");
            }
        }

        foreach (var key in completed)
            _activeFlights.Remove(key);

    }

    public void LounchStarship(Vector2 targetCoord)
    {
        string playerId = "Player";
        if (_shipsInSpace.ContainsKey(playerId))
        {
            var ship = _shipsInSpace[playerId];

            var shipPos = ship.transform.position;

            foreach (var kvp in _asteroids)
            {
                if (kvp.Value.Position != targetCoord ||
                    Vector3.Distance(shipPos, targetCoord.ToV3()) > 1.8f) continue;

                TookAsteroid(targetCoord, kvp);
                return;
            }
        }

        LounchStarshipCoroutine(targetCoord);
    }

    bool _isLounch = false;
    private void LounchStarshipCoroutine(Vector2 targetCoord)
    {
        if (_isLounch) return;
        _isLounch = true;
        _uiManager.UpdateEnergy();

        string playerId = "Player";
        Vector2 from = _activeShips.ContainsKey(playerId) ? _activeShips[playerId] : Vector2.zero;
        int endBlock = _blockNumber + 2;

        _activeFlights[playerId] = new FlightData
        {
            From = from,
            To = targetCoord,
            EndBlock = endBlock
        };

        Debug.Log($"[FakeNetworkManager] Started flight for {playerId} from {from} to {targetCoord} (ends at block {endBlock})");

        StartCoroutine(LaunchOrMoveShip(playerId, from.ToV3(), targetCoord.ToV3(), _blockNumber, endBlock));
    }

    IEnumerator LaunchOrMoveShip(string playerId, Vector3 from, Vector3 to, int blockStart, int blockEnd)
    {

        _energySlider.FillOverTime(2f);
        if (_shipsInSpace.ContainsKey(playerId))
        {
            _shipsInSpace[playerId].ShowRoketFlame();
        }

        yield return new WaitForSeconds(Random.Range(1f, 2f));
        _energySlider.FillInstantly();

        if (_shipsInSpace.ContainsKey(playerId))
        {
            _shipsInSpace[playerId].Lounch(from, to, blockStart, blockEnd);
        }
        else
        {
            GameObject ship = Instantiate(_starshipPrefab, from, Quaternion.identity);
            ship.name = playerId;

            var entity = ship.GetComponent<ShipEntity>();
            entity.Lounch(from, to, blockStart, blockEnd);

            _shipsInSpace[playerId] = entity;
            _cameraController.SetTarget(ship.transform);
        }

    }

    public void CompleteFlight()
    {
        _isLounch = false;
    }

    long _asteroidIdCounter = 1;
    long GenerateAsteroidId() => _asteroidIdCounter++;

    internal void TookAsteroid(Vector3 ateroidPosition)
    {
        var v2Pos = new Vector2(ateroidPosition.x, ateroidPosition.z);

        foreach (var kvp in _asteroids)
        {
            if (kvp.Value.Position != v2Pos) continue;

            TookAsteroid(v2Pos, kvp);

            return;
        }

        Debug.Log($"[FakeNetworkManager] No asteroid found at {v2Pos}");
    }

    private void TookAsteroid(Vector2 v2Pos, KeyValuePair<long, AsteroidData> kvp)
    {
        var asteroidView = kvp.Value.Instance.GetComponent<AsteroidView>();
        asteroidView.Collect();
        _uiManager.CollectScreenEffect();

        if (asteroidView.TypeId == 0)
        {
            _uiManager.UpdateCoinAmount(10);
        }
        else if (asteroidView.TypeId == 1)
        {
            _uiManager.UpdateDots(0.1f);
        }
        else if (asteroidView.TypeId == 2)
        {
            _uiManager.AddEnergy(11);
        }
        else
        {
            string playerId = "Player";
            if (_shipsInSpace.ContainsKey(playerId))
            {
                var ship = _shipsInSpace[playerId];

                ship.GetComponent<ShipEntity>().SetSkin(asteroidView.TypeId - 2);
            }
        }


        Destroy(kvp.Value.Instance);
        _asteroids.Remove(kvp.Key);
        Debug.Log($"[FakeNetworkManager] Took asteroid {kvp.Key} at {v2Pos}");
    }
}


