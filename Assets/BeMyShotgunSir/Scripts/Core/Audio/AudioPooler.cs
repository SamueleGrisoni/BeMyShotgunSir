using UnityEngine;
using UnityEngine.Pool;
using BeMyShotgunSir.Scripts.Utils;
using static BeMyShotgunSir.Scripts.Utils.AudioUtils;

namespace BeMyShotgunSir.Scripts.Core.Audio
{
    public class PooledAudioSource : PooledObject<PooledAudioSource, AudioSource>
    {
        private Coroutine _monitorCoroutine;

        public void StartMonitoring()
        {
            StopMonitoring();
            _monitorCoroutine = StartCoroutine(MonitorAudioRoutine());
        }

        public void StopMonitoring()
        {
            if (_monitorCoroutine != null)
            {
                StopCoroutine(_monitorCoroutine);
                _monitorCoroutine = null;
            }
        }

        private System.Collections.IEnumerator MonitorAudioRoutine()
        {
            yield return null;
            while (Component.isPlaying)
                yield return new WaitForSeconds(0.1f); //every 0.1 seconds check if the clip is still playing, when it stops return to pool
            ReturnToPool();
        }
    }
    public class AudioPooler : MonoBehaviour
    {

        [SerializeField] private GameObject _inactiveObjsParent;
        [SerializeField] private GameObject _activeObjsParent;
        [SerializeField] private int _initCapacity = 5;
        [SerializeField] private int _maxCapacity = 15;

        private static IObjectPool<PooledAudioSource> _audioPool;
        private static GameObject _inactiveObjsParentStatic;
        private static GameObject _activeObjsParentStatic;

        private void Awake()
        {
            _inactiveObjsParentStatic = _inactiveObjsParent;
            _activeObjsParentStatic = _activeObjsParent;
            _audioPool = new ObjectPool<PooledAudioSource>(
                createFunc: CreateAudioObject,
                actionOnGet: OnGetAudioObject,
                actionOnRelease: OnReleaseAudioObject,
                actionOnDestroy: OnDestroyAudioObject,
                collectionCheck: false,
                defaultCapacity: _initCapacity,
                maxSize: _maxCapacity
            );
        }

        private PooledAudioSource CreateAudioObject()
        {
            var audioObj = new GameObject("PooledAudioSource");
            audioObj.transform.SetParent(_inactiveObjsParentStatic.transform);

            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = GetMixerGroup(MixerGroupEnum.Master);
            audioSource.spatialBlend = 1.0f; // Default to 3D sound

            PooledAudioSource pooledAudioSource = audioObj.AddComponent<PooledAudioSource>();
            pooledAudioSource.SetPool(_audioPool);
            audioObj.SetActive(false);
            return pooledAudioSource;
        }

        private void OnGetAudioObject(PooledAudioSource audioObj)
        {
            if (audioObj == null || audioObj.Component == null) return;
            audioObj.transform.SetParent(_activeObjsParentStatic.transform);
            //only universal resets here
            AudioSource aSource = audioObj.Component;
            aSource.volume = 1f;
            aSource.pitch = 1f;
            aSource.spatialBlend = 1f;
            aSource.loop = false;
        }

        private void OnReleaseAudioObject(PooledAudioSource audioObj)
        {
            audioObj.StopMonitoring(); audioObj.Component.Stop();
            audioObj.Component.clip = null;

            audioObj.transform.SetParent(_inactiveObjsParentStatic.transform);
            audioObj.gameObject.SetActive(false);
        }

        private void OnDestroyAudioObject(PooledAudioSource audioObj) =>
            Destroy(audioObj.gameObject);

        public AudioSource GetAudioSource() =>
             _audioPool.Get().Component;

    }
}
