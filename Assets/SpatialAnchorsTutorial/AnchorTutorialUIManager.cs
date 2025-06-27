using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug; // Å© Unity ÇÃ Debug Çñæé¶

public class AnchorManager : MonoBehaviour
{
    [SerializeField] private GameObject _saveableAnchorPrefab;
    [SerializeField] private GameObject _saveablePreview;
    [SerializeField] private Transform _saveableTransform;

    [SerializeField] private GameObject _nonSaveableAnchorPrefab;
    [SerializeField] private GameObject _nonSaveablePreview;
    [SerializeField] private Transform _nonSaveableTransform;

    private List<OVRSpatialAnchor> _anchorInstances = new(); // Active instances
    private HashSet<Guid> _anchorUuids = new();              // For persistent IDs
    private Action<bool, OVRSpatialAnchor.UnboundAnchor> _onLocalized;

    public static AnchorManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _onLocalized = OnLocalized;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) // green anchor
        {
            var go = Instantiate(_saveableAnchorPrefab, _saveableTransform.position, _saveableTransform.rotation);
            SetupAnchorAsync(go.AddComponent<OVRSpatialAnchor>(), saveAnchor: true);
        }
        else if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) // red anchor
        {
            var go = Instantiate(_nonSaveableAnchorPrefab, _nonSaveableTransform.position, _nonSaveableTransform.rotation);
            SetupAnchorAsync(go.AddComponent<OVRSpatialAnchor>(), saveAnchor: false);
        }

        if (OVRInput.GetDown(OVRInput.Button.Three)) // X button
        {
            foreach (var anchor in _anchorInstances)
            {
                Destroy(anchor.gameObject);
            }
            _anchorInstances.Clear();
        }

        if (OVRInput.GetDown(OVRInput.Button.One)) // A button
        {
            LoadAllAnchors();
        }

        if (OVRInput.GetDown(OVRInput.Button.Four)) // Y button
        {
            EraseAllAnchors();
        }
    }

    private async void SetupAnchorAsync(OVRSpatialAnchor anchor, bool saveAnchor)
    {
        if (!await anchor.WhenLocalizedAsync())
        {
            Debug.LogError("Unable to create anchor.");
            Destroy(anchor.gameObject);
            return;
        }

        _anchorInstances.Add(anchor);

        if (saveAnchor && (await anchor.SaveAnchorAsync()).Success)
        {
            _anchorUuids.Add(anchor.Uuid);
        }
    }

    public async void LoadAllAnchors()
    {
        var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(_anchorUuids, unboundAnchors);

        if (result.Success)
        {
            foreach (var anchor in unboundAnchors)
            {
                anchor.LocalizeAsync().ContinueWith(_onLocalized, anchor);
            }
        }
        else
        {
            Debug.LogError($"Load anchors failed with {result.Status}.");
        }
    }

    private void OnLocalized(bool success, OVRSpatialAnchor.UnboundAnchor unboundAnchor)
    {
        var pose = unboundAnchor.Pose;
        var go = Instantiate(_saveableAnchorPrefab, pose.position, pose.rotation);
        var anchor = go.AddComponent<OVRSpatialAnchor>();
        unboundAnchor.BindTo(anchor);
        _anchorInstances.Add(anchor);
    }

    public async void EraseAllAnchors()
    {
        var result = await OVRSpatialAnchor.EraseAnchorsAsync(anchors: null, uuids: _anchorUuids);
        if (result.Success)
        {
            _anchorUuids.Clear();
            Debug.Log("Anchors erased.");
        }
        else
        {
            Debug.LogError($"Anchors NOT erased {result.Status}");
        }
    }
}
