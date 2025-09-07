using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(ARRaycastManager), typeof(ARPlaneManager))]
public class PlaceField : MonoBehaviour
{
    [SerializeField]
    private GameObject fieldPrefab;

    private ARRaycastManager _aRRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private ARPlaneManager _arPlaneManager;
    private GameObject fieldInstantiated;
    private Spawn spawn;

    private void Awake()
    {
        _aRRaycastManager = GetComponent<ARRaycastManager>();
        _arPlaneManager = GetComponent<ARPlaneManager>();
        spawn = GetComponent<Spawn>();
    }

    private void OnEnable()
    {
        EnhancedTouch.EnhancedTouchSupport.Enable();
        EnhancedTouch.TouchSimulation.Enable();
        EnhancedTouch.Touch.onFingerDown += SpawnField;
    }

    private void OnDisable()
    {
        EnhancedTouch.EnhancedTouchSupport.Disable();
        EnhancedTouch.TouchSimulation.Disable();
        EnhancedTouch.Touch.onFingerDown -= SpawnField;
    }

    private void SpawnField(EnhancedTouch.Finger finger)
    {
        if (finger.index != 0) return;
        if (_aRRaycastManager.Raycast(finger.currentTouch.screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            ARRaycastHit hit = hits[0];
            Pose pose = hit.pose;
            if (_arPlaneManager.GetPlane(hit.trackableId).alignment == PlaneAlignment.HorizontalUp)
            {
                if (fieldInstantiated == null)
                {
                    fieldInstantiated = Instantiate(fieldPrefab, pose.position, pose.rotation);
                    spawn.SetGameField(fieldInstantiated);
                }
                else
                {
                    fieldInstantiated.transform.position = pose.position;
                    fieldInstantiated.transform.rotation = pose.rotation;
                }
            }    
        }
    }
}
