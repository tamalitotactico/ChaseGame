using UnityEngine;

public class Util_TestVFX : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() {
    if (Input.GetKeyDown(KeyCode.T))
        GetComponent<StatusEffectController>().Apply(new StunnedEffect(3f));
    if (Input.GetKeyDown(KeyCode.Y))
        GetComponent<StatusEffectController>().Apply(new SlowedEffect(5f, 0.4f));
}

}
