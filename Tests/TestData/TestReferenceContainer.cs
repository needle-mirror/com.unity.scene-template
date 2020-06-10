using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestReferenceContainer : MonoBehaviour
{
    public Object MyReference;
    public Object[] MyReferenceArray;

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnValidate()
    {
        Debug.Log("OnValidate is triggered!");
    }

    void Awake()
    {
        Debug.Log("Awake is triggered!");
    }

    void OnEnable()
    {
        Debug.Log("OnEnable is triggered!");
    }

    void Start()
    {
        Debug.Log("Start is triggered!");
    }
}
