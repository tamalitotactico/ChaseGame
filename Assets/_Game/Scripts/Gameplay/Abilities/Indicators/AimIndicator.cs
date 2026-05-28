using UnityEngine;

/// <summary>
/// Componente visual que se muestra durante el aim de una habilidad.
/// AbilityIndicatorView gestiona el lifecycle automaticamente — no instanciar
/// ni destruir desde la ability.
///
/// ====== PARA ANADIR UN INDICADOR A UNA ABILITY NUEVA ======
///
/// Paso 1 — Exponer las dimensiones en tu AbilityData (subclase):
///   Si tu ability tiene su propio campo de rango/radio, sobreescribe la propiedad:
///       public override float IndicatorRange  => miCampoDeRango;
///       public override float IndicatorRadius => miRadioDeAoE;
///   Si no tienes campo propio, deja los fallbacks de AbilityData base y setea
///   'indicatorRange' / 'indicatorRadius' directamente en el Inspector del SO.
///
/// Paso 2 — Elegir el prefab de indicador segun el tipo de habilidad:
///   ArrowIndicator.prefab    Lee IndicatorRange (largo) e IndicatorWidth (grosor).
///                            Usar para: proyectil, dash, cualquier ability lineal.
///   CircleIndicator.prefab   Lee IndicatorRadius. Usar Range Indicator.png (anillo
///                            de rango) o Heal Indicator.png (relleno de AoE).
///   TeleportIndicator.prefab Lee IndicatorRange (distancia) e IndicatorRadius (AoE).
///                            Muestra flecha + circulo en el punto de llegada.
///   ConeIndicator.prefab     Lee IndicatorRange (escala uniforme). El angulo del
///                            cono esta bakeado en el sprite.
///
/// Paso 3 — Asignar en el Inspector:
///   En el SO de tu ability, campo 'indicatorPrefab' = el prefab elegido en Paso 2.
///   Para ArrowIndicator y TeleportIndicator, configura tambien 'indicatorWidth'.
///   Listo. AbilityIndicatorView instancia/destruye el indicador sin codigo extra.
///
/// ====== PARA CREAR UN TIPO DE INDICADOR NUEVO ======
///
/// 1. Subclasifica AimIndicator.
/// 2. Implementa Tick(origin, direction) — posiciona y escala tus SpriteRenderers.
///    Lee _data.IndicatorRange / IndicatorRadius / IndicatorWidth en cada Tick
///    (no cachear en Begin — si el SO cambia en runtime, el indicador lo refleja).
/// 3. Sobreescribe Begin(owner, data) para guardar la referencia a data; End() si
///    necesitas cleanup.
/// 4. Crea un prefab con el componente en el root. AbilityIndicatorView lo encuentra
///    via GetComponent<AimIndicator>() al instanciar el prefab.
/// </summary>
public abstract class AimIndicator : MonoBehaviour
{
    /// <summary>Owner y data de la ability activa. Setup pesado de una sola vez.</summary>
    public virtual void Begin(Character owner, AbilityData data) { }

    /// <summary>Cada frame durante el aim. origin = posicion del caster, direction = dir actual del aim.</summary>
    public abstract void Tick(Vector2 origin, Vector2 direction);

    /// <summary>Llamado antes de Destroy. Usar para cleanup (VFX, audio, etc).</summary>
    public virtual void End() { }
}
