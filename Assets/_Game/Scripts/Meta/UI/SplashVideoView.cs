using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Reproduce un VideoClip en un RawImage como splash de prueba (SplashartTest.mp4). Crea el
/// RenderTexture y el VideoPlayer en runtime (sin assets extra). Usado en CharacterDetail como
/// stand-in del splash demostrativo mientras no haya arte final (Phase 6).
/// </summary>
[RequireComponent(typeof(RawImage))]
public class SplashVideoView : MonoBehaviour
{
    [SerializeField] VideoClip clip;
    [SerializeField] int width = 640;
    [SerializeField] int height = 360;

    RawImage _raw;
    VideoPlayer _player;
    RenderTexture _rt;

    void Awake() => _raw = GetComponent<RawImage>();

    void OnEnable()
    {
        Setup();
        if (_player != null) _player.Play();
    }

    void OnDisable()
    {
        if (_player != null) _player.Stop();
    }

    void OnDestroy()
    {
        if (_rt != null) { _rt.Release(); Destroy(_rt); _rt = null; }
    }

    void Setup()
    {
        if (clip == null || _raw == null) return;

        if (_rt == null)
        {
            _rt = new RenderTexture(width, height, 0);
            _raw.texture = _rt;
            _raw.color = Color.white;
        }

        if (_player == null)
        {
            var existing = GetComponent<VideoPlayer>();
            _player = existing != null ? existing : gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.isLooping = true;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _rt;
            _player.source = VideoSource.VideoClip;
            _player.clip = clip;
            _player.audioOutputMode = VideoAudioOutputMode.None;
        }
    }
}
