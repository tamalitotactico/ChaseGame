using UnityEngine;

/// <summary>
/// Cache central de indices y mascaras de layers usadas por gameplay.
/// Evita llamar LayerMask.NameToLayer por frame/colision (es una busqueda de
/// string en el engine). Los valores se resuelven una sola vez de forma perezosa.
///
/// El cache se reinicia al entrar en Play Mode para soportar "Enter Play Mode
/// without Domain Reload" (donde los static persisten entre sesiones).
/// </summary>
public static class GameLayers
{
    const string WallName = "Wall";

    const int Unresolved = -2; // distinto de -1 (layer inexistente)

    static int  _wall = Unresolved;
    static int  _wallMask;
    static bool _wallMaskResolved;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache()
    {
        _wall = Unresolved;
        _wallMaskResolved = false;
    }

    /// <summary>Indice del layer "Wall" (-1 si no existe en el proyecto).</summary>
    public static int Wall
    {
        get
        {
            if (_wall == Unresolved) _wall = LayerMask.NameToLayer(WallName);
            return _wall;
        }
    }

    /// <summary>Mascara (bitfield) con solo el layer "Wall". 0 si no existe.</summary>
    public static int WallMask
    {
        get
        {
            if (!_wallMaskResolved)
            {
                int w = Wall;
                _wallMask = w >= 0 ? (1 << w) : 0;
                _wallMaskResolved = true;
            }
            return _wallMask;
        }
    }
}
