using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SpriteBatchRecorder : MonoBehaviour
{
    [Header("Referencias")]
    public Camera renderCamera;
    public Animator animator;
    public Transform target;

    [Header("Render")]
    public int width = 512;
    public int height = 512;
    public int framesPerAnim = 24;

    [Header("Animaciones (nombre EXACTO del state)")]
    public List<string> animationStates = new List<string>()
    {
        "Idle",
        "Run"
    };

    [Header("Direcciones")]
    public bool use8Directions = false;

    Vector3[] dir4 = {
        new Vector3(0,180,0),
        new Vector3(0,0,0),
        new Vector3(0,90,0),
        new Vector3(0,-90,0)
    };

    string[] dir4Names = { "Down","Up","Right","Left" };

    Vector3[] dir8 = {
        new Vector3(0,180,0),
        new Vector3(0,135,0),
        new Vector3(0,90,0),
        new Vector3(0,45,0),
        new Vector3(0,0,0),
        new Vector3(0,-45,0),
        new Vector3(0,-90,0),
        new Vector3(0,-135,0)
    };

    string[] dir8Names = {
        "Down","DownRight","Right","UpRight",
        "Up","UpLeft","Left","DownLeft"
    };

    [ContextMenu("GENERAR SPRITES")]
    public void Generate()
    {
        var dirs = use8Directions ? dir8 : dir4;
        var names = use8Directions ? dir8Names : dir4Names;

        RenderTexture rt = new RenderTexture(width, height, 24);
        renderCamera.targetTexture = rt;

        string basePath = Application.dataPath + "/SpritesOutput/";
        Directory.CreateDirectory(basePath);

        foreach (var anim in animationStates)
        {
            // Forzar inicio de animación
            animator.Play(anim, 0, 0);
            animator.Update(0);

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            float clipLength = state.length;

            float delta = clipLength / framesPerAnim;

            for (int d = 0; d < dirs.Length; d++)
            {
                target.rotation = Quaternion.Euler(dirs[d]);

                string folder = basePath + anim + "_" + names[d] + "/";
                Directory.CreateDirectory(folder);

                // Reiniciar animación por dirección
                animator.Play(anim, 0, 0);
                animator.Update(0);

                for (int f = 0; f < framesPerAnim; f++)
                {
                    animator.Update(delta);

                    renderCamera.Render();

                    RenderTexture.active = rt;
                    Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();

                    string filename = $"{anim}_{names[d]}_{f}.png";
                    File.WriteAllBytes(folder + filename, tex.EncodeToPNG());
                }
            }
        }

        renderCamera.targetTexture = null;
        RenderTexture.active = null;

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif

        Debug.Log("Sprites generados correctamente.");
    }
}