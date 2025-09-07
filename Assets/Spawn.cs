using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class Spawn : MonoBehaviour
{
    [SerializeField] List<GameObject> prefabsToSpawn = new List<GameObject>();

    private Dictionary<string, GameObject> _prefabsDict = new Dictionary<string, GameObject>();

    private ARTrackedImageManager _trackedImageManager;

    private GameObject gameField;

    private List<GameObject> childFields = new List<GameObject>();

    private Dictionary<string, GameObject> _prefabsInstantiated = new Dictionary<string, GameObject>();

    private Dictionary<string, GameObject> _closestFieldToPrefab = new Dictionary<string, GameObject>();

    private Dictionary<string, bool> _hasFlooped = new Dictionary<string, bool>();

    private const int fieldNum = 4;

    private void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        _trackedImageManager.trackablesChanged.AddListener(OnImagesTrackedChanged);
        SetupSceneElements();
    }

    void OnDisable()
    {
        _trackedImageManager.trackablesChanged.RemoveListener(OnImagesTrackedChanged);
    }

    private void OnDestroy()
    {
        _trackedImageManager.trackablesChanged.RemoveListener(OnImagesTrackedChanged);
    }

    private void SetupSceneElements()
    {
        foreach (var prefab in prefabsToSpawn)
        {
            _prefabsDict.Add(prefab.name, prefab);
        }
    }

    public void SetGameField(GameObject field)
    {
        gameField = field;
        foreach (Transform child in field.transform)
        {
            childFields.Add(child.gameObject);
        }
    }

    private void OnImagesTrackedChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var updatedImage in eventArgs.updated)
        {
            UpdateTrackedImages(updatedImage);
        }
    }

    private void UpdateTrackedImages(ARTrackedImage trackedImage)
    {
        var cardName = trackedImage.referenceImage.name;
        if (_prefabsInstantiated.TryGetValue(cardName, out var spawnedPrefab))
        {
            if (spawnedPrefab.CompareTag("Landscape")) return;
            var closestChildField = GetClosestChildField(cardName);
            var isPerpendicular = IsPerpendicularInPlane(trackedImage.transform, closestChildField.transform);
            if (!isPerpendicular && _hasFlooped[cardName])
            {
                // Reset
                _hasFlooped[cardName] = false;
                return;
            }
            if (isPerpendicular && !_hasFlooped[cardName])
            {
                // Animate
                var animator = spawnedPrefab.GetComponent<Animator>();
                animator.SetTrigger("TriggerHop");
                _hasFlooped[cardName] = true;
                return;
            }
            return;
        }
        if (_prefabsDict.TryGetValue(cardName, out var prefab))
        {
            var spawn = _prefabsInstantiated[cardName] = Instantiate(prefab, trackedImage.transform);
            spawn.gameObject.SetActive(true);

            if (gameField != null)
            {
                GameObject closestChildField = childFields[0];
                Vector3 cardPos = trackedImage.transform.position;
                double minDist = Vector3.Distance(cardPos, closestChildField.transform.position) * 1000.0;
                for (int i = 1; i < fieldNum; i++)
                {
                    double dist = Vector3.Distance(cardPos, childFields[i].transform.position) * 1000.0;
                    if (dist < minDist)
                    {
                        closestChildField = childFields[i];
                        minDist = dist;
                    }
                }

                var closestChildFieldTransform = closestChildField.transform;
                _closestFieldToPrefab[cardName] = closestChildField;
                _hasFlooped[cardName] = false;

                if (spawn.CompareTag("Landscape"))
                {
                    SetFieldVisible(closestChildField, false);
                    spawn.gameObject.transform.parent = gameField.transform;
                    spawn.gameObject.transform.position = closestChildFieldTransform.position;
                    spawn.gameObject.transform.rotation = closestChildFieldTransform.rotation;
                    spawn.gameObject.transform.localScale = closestChildFieldTransform.localScale;
                    return;

                }

                spawn.gameObject.transform.position = closestChildFieldTransform.position;
                spawn.gameObject.transform.rotation = closestChildFieldTransform.rotation;
                spawn.gameObject.transform.parent = closestChildFieldTransform;
                spawn.gameObject.transform.localScale *= Vector3.Magnitude(closestChildFieldTransform.localScale) * Vector3.Magnitude(gameField.transform.localScale);
            }
        }

    }

    private GameObject GetClosestChildField(string key)
    {
        return _closestFieldToPrefab[key];
    }

    bool IsPerpendicularInPlane(Transform card, Transform field, float toleranceDeg = 20f)
    {
        float yawDelta = Mathf.Abs(Mathf.DeltaAngle(field.transform.eulerAngles.y, card.transform.eulerAngles.y));
        return Mathf.Abs(yawDelta - 90f) <= toleranceDeg;
    }
    
    void SetFieldVisible(GameObject field, bool visible)
    {
        foreach (var r in field.GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
    }
} 