using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowderDrawer : MonoBehaviour
{
    public SandSimulation sandSim;
    public bool canDraw = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(canDraw && sandSim != null)
        {
            sandSim.CreateTempElement(this.transform.position);
        }
    }
}
