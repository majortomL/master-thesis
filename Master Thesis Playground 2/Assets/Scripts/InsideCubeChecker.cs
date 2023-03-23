using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InsideCubeChecker : MonoBehaviour
{
    public GameObject MeshParent;
    public Material DistributionTransparentMaterial;
    public Material ManufacturingTransparentMaterial;
    public Material ProcurementTransparentMaterial;
    public Material OtherTransparentMaterial;
    public Material DistributionOpaqueMaterial;
    public Material ManufacturingOpaqueMaterial;
    public Material ProcurementOpaqueMaterial;
    public Material OtherOpaqueMaterial;

    private bool insideACube = false;
    private GameObject cubeIAmInside;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        changeInsideCubeMaterial();
    }

    private void changeInsideCubeMaterial()
    {
        insideACube = false;
        Transform productionCubes = MeshParent.transform.Find("ProductionCubes");
        if (productionCubes)
        {
            foreach (Transform cube in productionCubes)
            {
                if (cube.GetComponent<BoxCollider>() != null && cube.GetComponent<BoxCollider>().bounds.Contains(transform.position))
                { // Render nontransparent
                    cube.GetComponent<Renderer>().material = DistributionOpaqueMaterial;
                    switch (cube.GetComponent<ProductionCube>().MainProcess)
                    {
                        case "Distribution":
                            cube.GetComponent<Renderer>().material = DistributionOpaqueMaterial;
                            break;
                        case "Manufacturing":
                            cube.GetComponent<Renderer>().material = ManufacturingOpaqueMaterial;
                            break;
                        case "Procurement":
                            cube.GetComponent<Renderer>().material = ProcurementOpaqueMaterial;
                            break;
                        case "Other":
                            cube.GetComponent<Renderer>().material = OtherOpaqueMaterial;
                            break;
                    }
                    cubeIAmInside = cube.gameObject;
                    insideACube = true;
                }
                else
                { // Render transparent
                    if (cube.GetComponent<Renderer>().enabled)
                    {
                        switch (cube.GetComponent<ProductionCube>().MainProcess)
                        {
                            case "Distribution":
                                cube.GetComponent<Renderer>().material = DistributionTransparentMaterial;
                                break;
                            case "Manufacturing":
                                cube.GetComponent<Renderer>().material = ManufacturingTransparentMaterial;
                                break;
                            case "Procurement":
                                cube.GetComponent<Renderer>().material = ProcurementTransparentMaterial;
                                break;
                            case "Other":
                                cube.GetComponent<Renderer>().material = OtherTransparentMaterial;
                                break;
                        }
                    }
                }
            }


            if (insideACube)
            { // Disable all other cubes
                Debug.Log("inside a cube");
                foreach (Transform cube in productionCubes)
                {
                    if (cube.gameObject.GetInstanceID() != cubeIAmInside.gameObject.GetInstanceID())
                    {
                        cube.GetComponent<Renderer>().enabled = false;
                    } else
                    {
                        cube.GetComponent<Renderer>().enabled = true;
                    }
                }
            }
            if (!insideACube)
            { // Enable all cubes
                foreach(Transform cube in productionCubes)
                {
                    cube.GetComponent<Renderer>().enabled = true;
                }
            }
        }
    }
}
