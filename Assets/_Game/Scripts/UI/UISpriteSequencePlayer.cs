using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reproduce una secuencia de Sprites sobre un Image (flipbook). El indice de frame se DERIVA del
/// tiempo transcurrido (no de un acumulador por-frame): es independiente del framerate y no acumula
/// drift ante stutter. Reutilizable (burbuja de emote, previews, etc).
///
/// Si frames es null/vacio no hace nada (respeta el sprite que ya tenga el Image). Pensado para
/// frames del MISMO tamaño: al intercambiar solo cambia la textura, sin reconstruir geometria.
/// </summary>
[RequireComponent(typeof(Image))]
public class UISpriteSequencePlayer : MonoBehaviour
{
    Image    _img;
    Sprite[] _frames;
    float    _fps;
    bool     _loop;
    float    _startTime;
    bool     _playing;

    void Awake() { _img = GetComponent<Image>(); }

    /// <summary>Arranca la reproduccion desde el frame 0. fps se clampa a >= 1.</summary>
    public void Play(Sprite[] frames, float fps, bool loop)
    {
        if (frames == null || frames.Length == 0) { _playing = false; return; }
        if (_img == null) _img = GetComponent<Image>();
        _frames    = frames;
        _fps       = Mathf.Max(1f, fps);
        _loop      = loop;
        _startTime = Time.time;
        _playing   = true;
        _img.sprite  = _frames[0];
        _img.enabled = true;
    }

    public void Stop() => _playing = false;

    void Update()
    {
        if (!_playing || _frames == null) return;

        int i = Mathf.FloorToInt((Time.time - _startTime) * _fps);
        if (_loop) i %= _frames.Length;
        else if (i >= _frames.Length) { i = _frames.Length - 1; _playing = false; }

        var s = _frames[i];
        if (s != null && _img.sprite != s) _img.sprite = s;
    }
}
