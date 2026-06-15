using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector del CharacterPreviewRig: botones para spawnear y conducir al personaje en Play
/// (mover en 8 direcciones, atacar, disparar cada habilidad, oir un SFX, re-aplicar skin).
/// En edit mode solo muestra una nota: animaciones, SFX, habilidades y shaders corren en runtime.
/// </summary>
[CustomEditor(typeof(CharacterPreviewRig))]
public class CharacterPreviewRigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var rig = (CharacterPreviewRig)target;
        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Entra en Play para previsualizar. Animaciones, SFX, habilidades y shaders corren en runtime.",
                MessageType.Info);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Spawn / Respawn")) rig.Spawn();
            using (new EditorGUI.DisabledScope(!rig.HasInstance))
                if (GUILayout.Button("Despawn")) rig.Despawn();
        }

        using (new EditorGUI.DisabledScope(!rig.HasInstance))
        {
            EditorGUILayout.LabelField("Mover", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("NW")) rig.Move(new Vector2(-1f, 1f));
                if (GUILayout.Button("N"))  rig.Move(Vector2.up);
                if (GUILayout.Button("NE")) rig.Move(new Vector2(1f, 1f));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("W")) rig.Move(Vector2.left);
                if (GUILayout.Button("Stop")) rig.StopMove();
                if (GUILayout.Button("E")) rig.Move(Vector2.right);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("SW")) rig.Move(new Vector2(-1f, -1f));
                if (GUILayout.Button("S"))  rig.Move(Vector2.down);
                if (GUILayout.Button("SE")) rig.Move(new Vector2(1f, -1f));
            }

            EditorGUILayout.LabelField("Acciones", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ataque basico")) rig.Attack();
                if (GUILayout.Button("Q (slot 0)"))    rig.TriggerSlot(0);
                if (GUILayout.Button("E (slot 1)"))    rig.TriggerSlot(1);
                if (GUILayout.Button("R (slot 2)"))    rig.TriggerSlot(2);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reproducir SFX suelto")) rig.PreviewSfx();
                if (GUILayout.Button("Re-aplicar Skin"))       rig.ApplySkin();
            }

            EditorGUILayout.LabelField($"Training dummies ({rig.DummyCount})", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Spawn dummy enemigo")) rig.SpawnDummy(false);
                if (GUILayout.Button("Spawn dummy aliado")) rig.SpawnDummy(true);
                using (new EditorGUI.DisabledScope(rig.DummyCount == 0))
                    if (GUILayout.Button("Limpiar dummies")) rig.ClearDummies();
            }
            using (new EditorGUI.DisabledScope(rig.DummyCount == 0))
                if (GUILayout.Button("Simular golpe basico al dummy (carga ult +1)"))
                    rig.SimulateHitNearestDummy();
        }
    }
}
