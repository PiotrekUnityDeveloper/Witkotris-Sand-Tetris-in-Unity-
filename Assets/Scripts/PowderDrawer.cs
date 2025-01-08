using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowderDrawer : MonoBehaviour
{
    public SandSimulation sandSim;
    public bool canDraw = true;

    private List<SandSimulation.Element> elements = new List<SandSimulation.Element>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void StopAndClear()
    {
        canDraw = false;
        foreach(SandSimulation.Element e in elements)
        {
            if (e != null)
            {
                StartCoroutine(Remove(e));
            }
        }
    }

    private IEnumerator Remove(SandSimulation.Element e)
    {
        yield return new WaitForSecondsRealtime(Random.value / 2);
        (e as SandSimulation.TemporalPowder).Die();
    }

    // Update is called once per frame
    void Update()
    {
        if(canDraw && sandSim != null)
        {
            SandSimulation.Element elem;
            sandSim.CreateTempElement(this.transform.position, out elem);
            if(elem != null) elements.Add(elem);
            StartCoroutine(ElementRemover(elem));
        }
    }

    private IEnumerator ElementRemover(SandSimulation.Element element)
    {
        yield return new WaitForSecondsRealtime(25f);
        elements.Remove(element);
    }
}
