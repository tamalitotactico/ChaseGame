/// <summary>
/// Cualquier objeto del mapa con el que un Character puede interactuar
/// (revival items, collectibles, doors, switches).
/// </summary>
public interface IInteractable
{
    /// <summary>Indica si este interactable acepta la interaccion ahora mismo.</summary>
    bool CanInteract(Character interactor);

    /// <summary>Comienza la interaccion (channeling, instant, etc.).</summary>
    void BeginInteract(Character interactor);

    /// <summary>Cancela una interaccion en curso (ej. el player se alejo).</summary>
    void CancelInteract(Character interactor);
}
