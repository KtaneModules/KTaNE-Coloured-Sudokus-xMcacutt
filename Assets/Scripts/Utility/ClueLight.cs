using System.Linq;
using UnityEngine;

public class ClueLight : MonoBehaviour
{
    public bool colorblindActive;
    public GameObject LightParent;
    public MeshRenderer Light1;
    public MeshRenderer Light2;
    
    public void SetColor(Material baseMaterial, Color color1, Color color2, bool babyMode, int val1, int val2)
    {
        Light1.material = new Material(baseMaterial) { color = color1 };
        Light2.material = new Material(baseMaterial) { color = color2 };
        if (!colorblindActive && !babyMode)
            return;
        Light1.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(color1, babyMode ? val1.ToString() : null);
        Light2.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(color2, babyMode ? val2.ToString() : null);
    }

    public void SetColor(Material baseMaterial, Color color, bool babyMode, int val)
    {
        Light1.material = new Material(baseMaterial) { color = color };
        Light2.material = new Material(baseMaterial) { color = color };
        if (!colorblindActive && !babyMode)
            return; 
        Light1.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(color, babyMode ? val.ToString() : null);
    }
}