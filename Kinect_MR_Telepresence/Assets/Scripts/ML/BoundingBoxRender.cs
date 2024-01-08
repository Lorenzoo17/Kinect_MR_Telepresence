using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MediaPipe.BlazeFace;

public class BoundingBoxRender
{
    public Detection detection;
    private int width, height;

    public void SetDetection(Detection detection, int width, int height) {
        this.detection = detection;
        this.width = width;
        this.height = height;
    }

    public Vector2 GetBoundingBoxCenter() {
        return detection.center * new Vector2(width, height);
    }

    public Vector2 GetBoundingBoxSize() {
        return detection.extent * new Vector2(width, height);
    }
}
