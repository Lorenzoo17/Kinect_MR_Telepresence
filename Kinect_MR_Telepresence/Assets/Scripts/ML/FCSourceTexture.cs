using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FCSourceTexture : MonoBehaviour
{
    public Texture Texture => OutputBuffer;

    [SerializeField] private RealSenseManager rsManager;

    [SerializeField] RenderTexture _outputTexture = null;
    [SerializeField] Vector2Int _outputResolution = new Vector2Int(1920, 1080);

    RenderTexture _buffer;
    RenderTexture OutputBuffer
      => _outputTexture != null ? _outputTexture : _buffer;
    void Blit(Texture source, bool vflip = false) {
        if (source == null) return;

        var aspect1 = (float)source.width / source.height;
        var aspect2 = (float)OutputBuffer.width / OutputBuffer.height;

        var scale = new Vector2(aspect2 / aspect1, aspect1 / aspect2);
        scale = Vector2.Min(Vector2.one, scale);
        if (vflip) scale.y *= -1;

        var offset = (Vector2.one - scale) / 2;
        
        Graphics.Blit(source, OutputBuffer, scale, offset);
    }

    void Start() {
        // Allocate a render texture if no output texture has been given.
        if (_outputTexture == null)
            _buffer = new RenderTexture
              (_outputResolution.x, _outputResolution.y, 0);
    }

    void Update() {
        //Blit(rsManager.GetTextureColorFromDepth(), true);
    }

    public void ExecuteBlit() {
        Blit(rsManager.GetTextureColorFromDepth(), true);
    }
}
