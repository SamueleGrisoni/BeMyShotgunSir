using System.Collections.Generic;
using BeMyShotgunSir.Player;
using UnityEngine;

namespace BeMyShotgunSir.LevelGenerator
{
    public class RoadManager : MonoBehaviour
    {
        [SerializeField] private TrackSeed _trackSeed;
        [SerializeField] private SOTrack _trackData;
        [SerializeField] private Transform _startingPoint;
        [SerializeField] private Driver _driver;
        [SerializeField] private TrackPooler _trackPooler;

        [SerializeField] private float _checkDistanceAhead = 50f;
        [SerializeField] private float _checkDistanceBehind = 30f;
        [SerializeField] private float _checkInterval;
        private float _timer;
        [SerializeField] private LinkedList<PooledRoadChunk> _activeRoadChunks;
        [SerializeField] private LinkedList<PooledEnvChunk[]> _activeEnvChunks;

        private float _distanceToLastChunkEnd;
        private float _distanceToFirstChunkStart;

        public void SetDriver(Driver driver) => _driver = driver;
        public void ClearRoadManager()
        {
            _trackPooler.ClearTrackData();
            _activeRoadChunks = null;
            _activeEnvChunks = null;
        }

        private void Start()
        {
            _trackPooler.SetTrackData(_trackData);
            _activeRoadChunks = new LinkedList<PooledRoadChunk>();
            _activeEnvChunks = new LinkedList<PooledEnvChunk[]>();

            if (_driver != null)
                _driver.transform.position = _startingPoint.position;

            PooledRoadChunk firstRoadChunk = _trackPooler.GetPooledRoadChunk(_trackSeed.RNG.Next(0, _trackData.RoadChunks.Length));
            PlaceRoadChunk(firstRoadChunk);
            _activeRoadChunks.AddLast(firstRoadChunk);
            float d = Vector3.Distance(_driver.transform.position, firstRoadChunk.Component.SpawnAnchor.position);
            _distanceToFirstChunkStart = d;
            _distanceToLastChunkEnd = d;

            _timer = _checkInterval; //start the timer
        }

        private void Update()
        {
            if (_driver == null || _activeRoadChunks.Count == 0)
                return;

            _timer -= Time.deltaTime;
            if (_timer > 0f)
                return;

            _timer = _checkInterval; //reset timer

            _distanceToLastChunkEnd = Vector3.Distance(_driver.transform.position, _activeRoadChunks.Last.Value.Component.SpawnAnchor.position);
            _distanceToFirstChunkStart = Vector3.Distance(_driver.transform.position, _activeRoadChunks.First.Value.transform.position);

            while (_distanceToLastChunkEnd < _checkDistanceAhead)
            {
                PooledRoadChunk newRoadChunk = _trackPooler.GetPooledRoadChunk(_trackSeed.RNG.Next(0, _trackData.RoadChunks.Length));
                PlaceRoadChunk(newRoadChunk);
                _activeRoadChunks.AddLast(newRoadChunk);

                _distanceToLastChunkEnd = Vector3.Distance(_driver.transform.position, newRoadChunk.Component.SpawnAnchor.position);
            }

            while (_activeRoadChunks.Count > 1 && _distanceToFirstChunkStart > _checkDistanceBehind)
            {
                PooledRoadChunk oldRoadChunk = _activeRoadChunks.First.Value;
                _activeRoadChunks.RemoveFirst();
                CleanRoadChunk();
                oldRoadChunk.ReturnToPool();

                _distanceToFirstChunkStart = Vector3.Distance(_driver.transform.position, _activeRoadChunks.First.Value.transform.position);
            }
        }

        private void PlaceRoadChunk(PooledRoadChunk roadChunk)
        {
            if (_activeRoadChunks.Count == 0)
                roadChunk.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            else
            {
                // 1. Ancora di uscita del pezzo precedente
                Transform targetAnchor = _activeRoadChunks.Last.Value.Component.NextRoadAnchors[0];

                // 2. Ancora di INGRESSO del pezzo nuovo (quella che deve combaciare con targetAnchor)
                // Nota: Assicurati che nel componente RoadChunk tu abbia un riferimento all'ingresso,
                // non usare SpawnAnchor (che di solito è l'uscita) per entrambi.
                Transform entrance = roadChunk.Component.SpawnAnchor;

                // 3. Allinea la rotazione del nuovo pezzo a quella del target
                roadChunk.transform.rotation = targetAnchor.rotation;

                // 4. Calcola l'offset locale: quanto dista l'ingresso dal pivot del padre?
                // Usiamo transform.InverseTransformPoint per ottenere la posizione RELATIVA dell'ingresso
                Vector3 localOffset = roadChunk.transform.InverseTransformPoint(entrance.position);

                // 5. Posiziona il padre in modo che l'ingresso finisca esattamente sul target
                // Sottraiamo l'offset ruotato dalla posizione del target
                roadChunk.transform.position = targetAnchor.position - (roadChunk.transform.rotation * localOffset);
            }
            roadChunk.gameObject.SetActive(true);
            PopulateRoadChunk(roadChunk);
        }

        private void PopulateRoadChunk(PooledRoadChunk roadChunk)
        {
            if (_trackData.EnvChunks.Length == 0)
                return;

            RoadChunk component = roadChunk.Component;
            int innerCount = component.InnerEnvChunkAreas.Length;
            int outerCount = component.EnvChunkAreas.Length;
            int totalEnv = innerCount + outerCount;

            // Prepariamo l'array nella LinkedList
            var spawnedInThisChunk = new PooledEnvChunk[totalEnv];
            _activeEnvChunks.AddLast(spawnedInThisChunk);

            // Uniamo le aree in un unico ciclo per pulizia (facoltativo, ma consigliato)
            for (int i = 0; i < totalEnv; i++)
            {
                // Identifichiamo il punto sulla strada (Target)
                SpawnArea currentArea = (i < innerCount)
                    ? component.InnerEnvChunkAreas[i]
                    : component.EnvChunkAreas[i - innerCount];
                EnvChunkSize targetSize = currentArea.Size;
                Transform targetPoint = currentArea.SpawnAnchor;

                // 1. Prendiamo l'oggetto dal pool
                int start = _trackData.GetEnvSizePoolStartIndex(targetSize);
                int end = _trackData.GetEnvSizePoolEndIndex(targetSize);
                if (start == -1)
                {
                    Debug.LogWarning($"No EnvChunks of size {targetSize} available in track data.");
                    continue;
                }
                PooledEnvChunk envChunk = _trackPooler.GetPooledEnvChunk(_trackSeed.RNG.Next(start, end));

                // Recuperiamo il punto di ancoraggio dell'oggetto ambientale (quello che deve toccare la strada)
                // Assicurati che PooledEnvChunk abbia un riferimento a questo punto (es. EntranceAnchor)
                Transform envEntrance = envChunk.Component.SpawnAnchor;

                // 2. Allineiamo la rotazione: l'EnvChunk deve guardare dove guarda il punto sulla strada
                // Usiamo la rotazione globale del punto sulla strada
                envChunk.transform.rotation = targetPoint.rotation;

                // 3. Calcoliamo l'OFFSET interno dell'EnvChunk
                // Quanto dista l'ancora dell'erba/albero dal pivot del suo padre?
                // Usiamo InverseTransformPoint per calcolare questa distanza nello spazio locale del prefab
                Vector3 localOffset = envChunk.transform.InverseTransformPoint(envEntrance.position);

                // 4. Calcoliamo la posizione finale (World)
                // Destinazione = Punto sulla strada - Offset ruotato
                // (Sottraiamo l'offset perché vogliamo spostare il padre "all'indietro" rispetto all'ancora)
                Vector3 worldPos = targetPoint.position - (envChunk.transform.rotation * localOffset);

                // 5. Applichiamo la trasformazione
                envChunk.transform.position = worldPos;
                envChunk.gameObject.SetActive(true);

                spawnedInThisChunk[i] = envChunk;
            }
        }

        private void CleanRoadChunk(int isLast = 0)
        {
            if (isLast == 0)
            {
                foreach (PooledEnvChunk envChunk in _activeEnvChunks.First.Value)
                    envChunk.ReturnToPool();
                _activeEnvChunks.RemoveFirst();
            }
            else
            {
                foreach (PooledEnvChunk envChunk in _activeEnvChunks.Last.Value)
                    envChunk.ReturnToPool();
                _activeEnvChunks.RemoveLast();
            }
        }
    }
}
