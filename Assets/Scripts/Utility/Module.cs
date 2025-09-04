using System;
using UnityEngine;

public class Module : MonoBehaviour
{
    [NonSerialized] public KMBombInfo bomb;
    [NonSerialized] public KMAudio audio;
    [NonSerialized] public KMBombModule module;
    [NonSerialized] public KMSelectable moduleSelectable;
    [NonSerialized] public KMColorblindMode colorblindMode;
    [NonSerialized] public int moduleId;
    protected static int _moduleIdCounter = 1;
    protected bool _isSolved;
    
    private void Start()
    {
        module = GetComponent<KMBombModule>();
        audio = GetComponent<KMAudio>();
        bomb = GetComponent<KMBombInfo>();
        moduleSelectable = GetComponent<KMSelectable>();
        colorblindMode = GetComponent<KMColorblindMode>();
        moduleId = _moduleIdCounter++;
        ModuleStart();
    }
    
    protected virtual void ModuleStart() {}
}