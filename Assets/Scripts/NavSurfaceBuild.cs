using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavSurfaceBuild : MonoBehaviour
{
    public int c = 0;
    public GameObject ville;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        while (c < 100)
            c++;
        if (c == 100)
        {
            NavMeshSurface surface = ville.GetComponent<NavMeshSurface>();
            surface.BuildNavMesh();
            Destroy(this);
        }
    }
}
