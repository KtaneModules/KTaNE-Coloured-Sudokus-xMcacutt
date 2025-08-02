using System.Linq;
using UnityEngine;

public class ClueLight : MonoBehaviour
{
    public bool colorblindActive;
    public GameObject LightParent;
    public MeshRenderer Light1;
    public MeshRenderer Light2;
    
    public void SetColor(Material baseMaterial, Color color1, Color color2)
    {
        Light1.material = new Material(baseMaterial) { color = color1 };
        Light2.material = new Material(baseMaterial) { color = color2 };
        if (!colorblindActive)
            return;
        Light1.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(color1);
        Light2.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(color2);
    }

    public void SetColor(Material baseMaterial, Color color)
    {
        Light1.material = new Material(baseMaterial) { color = color };
        Light2.material = new Material(baseMaterial) { color = color };
        if (!colorblindActive)
            return;
        Light1.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(color);
    }
}