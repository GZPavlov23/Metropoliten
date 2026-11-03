using UnityEngine;

// A signal that can be set: red until it's cleared, then green.
public class SignalLight : MonoBehaviour
{
    public GameObject red;
    public GameObject green;

    [Tooltip("The colour the lamp throws on what's around it.")]
    public Light glow;

    [Tooltip("Another lamp on the same circuit, if there is one.")]
    public Light otherGlow;

    public Color greenGlow = new Color(0.25f, 1f, 0.5f);

    public void Clear()
    {
        red.SetActive(false);
        green.SetActive(true);
        glow.color = greenGlow;
        if (otherGlow != null) otherGlow.color = greenGlow;
    }
}
