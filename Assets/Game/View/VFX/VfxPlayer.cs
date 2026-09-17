using System.Collections.Generic;
using Coffee.UIExtensions;
using UnityEngine;
using UnityEngine.UI;

namespace Game.View.VFX
{
    public sealed class VfxPlayer : MonoBehaviour
    {
        [SerializeField] private VfxCatalogSO catalog;
        [SerializeField] private Transform worldRoot;
        [SerializeField] private RectTransform uiVfxRoot;
        [SerializeField] private Material uiParticleMaterial;
        [SerializeField, Range(-32768, 32767)] private int uiSortingOrder = 150;

        private readonly Dictionary<VfxId, Queue<VfxInstance>> _pools = new();
        private readonly Dictionary<VfxId, Queue<VfxInstance>> _uiPools = new();
        private readonly Dictionary<VfxInstance, VfxId> _activeInstances = new();
        private readonly HashSet<VfxInstance> _uiInstances = new();
        private readonly Dictionary<VfxInstance, Vector3> _uiParticleBaseScales = new();
        private readonly Dictionary<VfxInstance, Material> _uiParticleMaterials = new();

        private Canvas _uiCanvas;
        private RectTransform _uiRoot;
        private Camera _uiCamera;
        private bool _isShuttingDown;

        private void Awake()
        {
            if (worldRoot == null)
                worldRoot = transform;

            Prewarm();
        }

        public void PlayAtWorld(VfxId id, Transform anchor = null, bool followAnchor = true)
        {
            if (!TryGetEntry(id, out VfxCatalogSO.Entry entry)) return;

            VfxInstance instance = GetInstance(entry);
            Transform targetRoot = worldRoot != null ? worldRoot : transform;

            if (anchor != null && followAnchor)
            {
                instance.transform.SetParent(anchor, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = instance.PrefabLocalScale * entry.ScaleMultiplier;
            }
            else
            {
                instance.transform.SetParent(targetRoot, false);
                instance.transform.position = anchor != null ? anchor.position : targetRoot.position;
                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = instance.PrefabLocalScale * entry.ScaleMultiplier;
            }

            StartInstance(id, entry, instance);
        }

        public void PlayAtUI(VfxId id, RectTransform target)
        {
            if (target == null)
            {
                Debug.LogWarning($"[VfxPlayer] Cannot play {id}: UI target is missing.", this);
                return;
            }

            RectTransform uiRoot = EnsureUiRoot();
            if (uiRoot == null) return;

            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetCamera, target.position);
            Canvas uiCanvas = uiRoot.GetComponentInParent<Canvas>();
            Camera uiCamera = uiCanvas != null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? uiCanvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    uiRoot,
                    screenPoint,
                    uiCamera,
                    out Vector2 localPoint))
            {
                Debug.LogWarning($"[VfxPlayer] Cannot convert {id} target to UI coordinates.", this);
                return;
            }

            if (!TryGetEntry(id, out VfxCatalogSO.Entry entry)) return;

            VfxInstance instance = GetUiInstance(entry);
            instance.transform.SetParent(uiRoot, false);
            instance.transform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            ApplyUiScale(instance, entry.ScaleMultiplier);

            StartInstance(id, entry, instance);
        }

        public void Stop(VfxId id)
        {
            List<VfxInstance> instances = new();
            foreach (KeyValuePair<VfxInstance, VfxId> pair in _activeInstances)
            {
                if (pair.Value == id)
                    instances.Add(pair.Key);
            }

            foreach (VfxInstance instance in instances)
            {
                if (_activeInstances.TryGetValue(instance, out VfxId activeId))
                {
                    instance.StopImmediately();
                    ReturnToPool(activeId, instance);
                }
            }
        }

        public void StopAll()
        {
            List<VfxInstance> instances = new(_activeInstances.Keys);

            foreach (VfxInstance instance in instances)
            {
                if (_activeInstances.TryGetValue(instance, out VfxId activeId))
                {
                    instance.StopImmediately();
                    ReturnToPool(activeId, instance);
                }
            }
        }

        private void Prewarm()
        {
            if (catalog == null || catalog.Entries == null) return;

            foreach (VfxCatalogSO.Entry entry in catalog.Entries)
            {
                if (entry == null || entry.Prefab == null) continue;
                if (entry.Id == VfxId.Win && uiVfxRoot != null) continue;

                for (int i = 0; i < entry.PrewarmCount; i++)
                {
                    VfxInstance instance = CreateInstance(entry);
                    ReturnToPool(entry.Id, instance);
                }
            }
        }

        private VfxInstance GetInstance(VfxCatalogSO.Entry entry)
        {
            Queue<VfxInstance> pool = GetPool(entry.Id);

            while (pool.Count > 0)
            {
                VfxInstance pooled = pool.Dequeue();
                if (pooled != null)
                    return pooled;
            }

            return CreateInstance(entry);
        }

        private VfxInstance GetUiInstance(VfxCatalogSO.Entry entry)
        {
            Queue<VfxInstance> pool = GetUiPool(entry.Id);

            while (pool.Count > 0)
            {
                VfxInstance pooled = pool.Dequeue();
                if (pooled != null)
                    return pooled;
            }

            return CreateUiInstance(entry);
        }

        private VfxInstance CreateInstance(VfxCatalogSO.Entry entry)
        {
            Transform parent = worldRoot != null ? worldRoot : transform;
            GameObject instanceObject = Instantiate(entry.Prefab, parent);
            Vector3 prefabLocalScale = instanceObject.transform.localScale;
            instanceObject.name = $"{entry.Prefab.name}_Runtime";
            instanceObject.SetActive(false);

            VfxInstance instance = instanceObject.GetComponent<VfxInstance>();
            if (instance == null)
                instance = instanceObject.AddComponent<VfxInstance>();

            instance.SetPrefabLocalScale(prefabLocalScale);

            return instance;
        }

        private VfxInstance CreateUiInstance(VfxCatalogSO.Entry entry)
        {
            Transform parent = uiVfxRoot != null ? uiVfxRoot : (_uiRoot != null ? _uiRoot : transform);
            GameObject instanceObject = new GameObject(
                $"{entry.Prefab.name}_UI_Runtime",
                typeof(RectTransform),
                typeof(CanvasRenderer));
            instanceObject.transform.SetParent(parent, false);

            UIParticle uiParticle = instanceObject.AddComponent<UIParticle>();
            uiParticle.SetParticleSystemPrefab(entry.Prefab);
            Material runtimeUiMaterial = CreateUiParticleMaterial(instanceObject);
            if (runtimeUiMaterial != null)
            {
                foreach (ParticleSystemRenderer renderer in instanceObject.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    renderer.sharedMaterial = runtimeUiMaterial;

                uiParticle.RefreshParticles();
            }

            VfxInstance instance = instanceObject.AddComponent<VfxInstance>();
            instance.SetPrefabLocalScale(Vector3.one);
            _uiInstances.Add(instance);
            _uiParticleBaseScales[instance] = uiParticle.scale3D;
            if (runtimeUiMaterial != null)
                _uiParticleMaterials[instance] = runtimeUiMaterial;

            instanceObject.SetActive(false);
            return instance;
        }

        private void StartInstance(VfxId id, VfxCatalogSO.Entry entry, VfxInstance instance)
        {
            if (instance == null) return;

            _activeInstances[instance] = id;
            instance.gameObject.SetActive(true);
            instance.Play(entry.FadeDelay, entry.FadeDuration, HandleInstanceCompleted);
        }

        private void HandleInstanceCompleted(VfxInstance instance)
        {
            if (instance == null) return;
            if (!_activeInstances.TryGetValue(instance, out VfxId id)) return;

            ReturnToPool(id, instance);
        }

        private void ReturnToPool(VfxId id, VfxInstance instance)
        {
            _activeInstances.Remove(instance);

            if (instance == null) return;
            if (_isShuttingDown)
            {
                Destroy(instance.gameObject);
                return;
            }

            instance.ResetForPool();
            bool isUiInstance = _uiInstances.Contains(instance);
            Transform targetRoot = isUiInstance
                ? (uiVfxRoot != null ? uiVfxRoot : (_uiRoot != null ? _uiRoot : transform))
                : (worldRoot != null ? worldRoot : transform);
            instance.transform.SetParent(targetRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            if (isUiInstance)
            {
                instance.transform.localScale = Vector3.one;
                ResetUiScale(instance);
            }
            else
            {
                instance.transform.localScale = instance.PrefabLocalScale;
            }
            instance.gameObject.SetActive(false);

            if (isUiInstance)
                GetUiPool(id).Enqueue(instance);
            else
                GetPool(id).Enqueue(instance);
        }

        private Queue<VfxInstance> GetPool(VfxId id)
        {
            if (!_pools.TryGetValue(id, out Queue<VfxInstance> pool))
            {
                pool = new Queue<VfxInstance>();
                _pools.Add(id, pool);
            }

            return pool;
        }

        private Queue<VfxInstance> GetUiPool(VfxId id)
        {
            if (!_uiPools.TryGetValue(id, out Queue<VfxInstance> pool))
            {
                pool = new Queue<VfxInstance>();
                _uiPools.Add(id, pool);
            }

            return pool;
        }

        private RectTransform EnsureUiRoot()
        {
            if (uiVfxRoot != null)
            {
                uiVfxRoot.SetAsLastSibling();
                return uiVfxRoot;
            }

            Canvas uiCanvas = EnsureUiCanvas();
            return uiCanvas != null ? _uiRoot : null;
        }

        private void ApplyUiScale(VfxInstance instance, float scaleMultiplier)
        {
            if (instance == null || !_uiParticleBaseScales.TryGetValue(instance, out Vector3 baseScale)) return;
            UIParticle uiParticle = instance.GetComponent<UIParticle>();
            if (uiParticle != null)
                uiParticle.scale3D = baseScale * scaleMultiplier;
        }

        private void ResetUiScale(VfxInstance instance)
        {
            if (instance == null || !_uiParticleBaseScales.TryGetValue(instance, out Vector3 baseScale)) return;
            UIParticle uiParticle = instance.GetComponent<UIParticle>();
            if (uiParticle != null)
                uiParticle.scale3D = baseScale;
        }

        private Material CreateUiParticleMaterial(GameObject instanceObject)
        {
            ParticleSystemRenderer sourceRenderer = instanceObject.GetComponentInChildren<ParticleSystemRenderer>(true);
            Material sourceMaterial = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;
            Material template = uiParticleMaterial;

            if (template == null)
            {
                Shader shader = Shader.Find("UI/Additive");
                if (shader == null)
                {
                    Debug.LogWarning("[VfxPlayer] UI particle material is missing and shader 'UI/Additive' was not found.", this);
                    return null;
                }

                template = new Material(shader);
            }

            Material material = new Material(template)
            {
                name = $"{instanceObject.name}_Material"
            };

            if (sourceMaterial != null)
            {
                Texture sourceTexture = sourceMaterial.HasProperty("_BaseMap")
                    ? sourceMaterial.GetTexture("_BaseMap")
                    : sourceMaterial.HasProperty("_MainTex")
                        ? sourceMaterial.GetTexture("_MainTex")
                        : null;
                if (sourceTexture != null && material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", sourceTexture);

                Color sourceColor = sourceMaterial.HasProperty("_BaseColor")
                    ? sourceMaterial.GetColor("_BaseColor")
                    : sourceMaterial.HasProperty("_Color")
                        ? sourceMaterial.GetColor("_Color")
                        : Color.white;
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", sourceColor);
            }

            return material;
        }

        private bool TryGetEntry(VfxId id, out VfxCatalogSO.Entry entry)
        {
            if (catalog != null && catalog.TryGet(id, out entry))
            {
                if (entry.Prefab != null)
                    return true;

                Debug.LogWarning($"[VfxPlayer] Catalog entry {id} has no prefab.", this);
            }
            else
            {
                Debug.LogWarning($"[VfxPlayer] Catalog entry {id} is missing.", this);
            }

            entry = null;
            return false;
        }

        private Canvas EnsureUiCanvas()
        {
            if (_uiCanvas != null) return _uiCanvas;

            _uiCamera = Camera.main;
            if (_uiCamera == null)
            {
                Debug.LogWarning("[VfxPlayer] Cannot create UI VFX canvas because Main Camera is missing.", this);
                return null;
            }

            CanvasScaler referenceScaler = FindFirstObjectByType<CanvasScaler>();
            GameObject canvasObject = new GameObject("[VFX_UI_Overlay]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            _uiCanvas = canvasObject.GetComponent<Canvas>();
            _uiCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _uiCanvas.worldCamera = _uiCamera;
            _uiCanvas.planeDistance = 5f;
            _uiCanvas.overrideSorting = true;
            _uiCanvas.sortingOrder = uiSortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (referenceScaler != null)
            {
                scaler.uiScaleMode = referenceScaler.uiScaleMode;
                scaler.referenceResolution = referenceScaler.referenceResolution;
                scaler.screenMatchMode = referenceScaler.screenMatchMode;
                scaler.matchWidthOrHeight = referenceScaler.matchWidthOrHeight;
                scaler.referencePixelsPerUnit = referenceScaler.referencePixelsPerUnit;
            }
            else
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(2778f, 1284f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
                scaler.referencePixelsPerUnit = 100f;
            }

            _uiRoot = canvasObject.GetComponent<RectTransform>();
            return _uiCanvas;
        }

        private void OnDestroy()
        {
            _isShuttingDown = true;

            foreach (VfxInstance instance in new List<VfxInstance>(_activeInstances.Keys))
            {
                if (instance != null)
                    Destroy(instance.gameObject);
            }

            _activeInstances.Clear();
            _pools.Clear();
            _uiPools.Clear();
            _uiInstances.Clear();
            _uiParticleBaseScales.Clear();

            foreach (Material material in _uiParticleMaterials.Values)
            {
                if (material != null)
                    Destroy(material);
            }

            _uiParticleMaterials.Clear();
        }
    }
}
