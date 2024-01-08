using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using MediaPipe.BlazeFace;

public class FCBlazeFace : MonoBehaviour {
    #region Editable attributes

    [SerializeField] FCSourceTexture _source = null;
    [SerializeField] ResourceSet _resources = null;
    [SerializeField, Range(0, 1)] float _threshold = 0.75f;
    [SerializeField] RawImage _previewUI = null;
    [SerializeField] Marker _markerPrefab = null;

    [SerializeField] private RealSenseManager rsManager;

    #endregion

    #region Private members

    FaceDetector _detector;
    Marker[] _markers = new Marker[16];

    private BoundingBoxRender boundingBox;

    #endregion

    #region MonoBehaviour implementation

    void Start() {
        // Face detector initialization
        _detector = new FaceDetector(_resources);

        // Marker population
        for (var i = 0; i < _markers.Length; i++)
            _markers[i] = Instantiate(_markerPrefab, _previewUI.transform);

        boundingBox = new BoundingBoxRender();
    }

    void OnDestroy()
      => _detector?.Dispose();

    void LateUpdate() {
        /*
        // Face detection
        _detector.ProcessImage(_source.Texture, _threshold);

        // Marker update
        var i = 0;

        foreach (var detection in _detector.Detections) {
            if (i == _markers.Length) break;
            var marker = _markers[i++];
            marker.detection = detection;
            marker.gameObject.SetActive(true);
        }

        for (; i < _markers.Length; i++)
            _markers[i].gameObject.SetActive(false);

        boundingBox.SetDetection(_markers[0].detection, rsManager.GetWidth(), rsManager.GetHeight());
        rsManager.SetBoundingBox(boundingBox.GetBoundingBoxCenter(), boundingBox.GetBoundingBoxSize());
        //Debug.Log(boundingBox.GetBoundingBoxCenter());
        //Debug.Log(boundingBox.GetBoundingBoxSize());

        // UI update
        _previewUI.texture = _source.Texture;
        */
    }

    public void ExecuteDetection() {
        // Face detection
        _detector.ProcessImage(_source.Texture, _threshold);

        // Marker update
        var i = 0;

        foreach (var detection in _detector.Detections) {
            if (i == _markers.Length) break;
            var marker = _markers[i++];
            marker.detection = detection;
            marker.gameObject.SetActive(true);
        }

        for (; i < _markers.Length; i++)
            _markers[i].gameObject.SetActive(false);

        boundingBox.SetDetection(_markers[0].detection, rsManager.GetWidth(), rsManager.GetHeight());
        rsManager.SetBoundingBox(boundingBox.GetBoundingBoxCenter(), boundingBox.GetBoundingBoxSize());
        //Debug.Log(boundingBox.GetBoundingBoxCenter());
        //Debug.Log(boundingBox.GetBoundingBoxSize());

        // UI update
        _previewUI.texture = _source.Texture;

        rsManager.FreeColorFromDepthTexture();
    }

    #endregion
}
