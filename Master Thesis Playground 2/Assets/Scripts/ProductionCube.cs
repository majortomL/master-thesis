using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionCube : MonoBehaviour
{

    // Holds the main and subprocess of the production cube
    public string MainProcess;
    public string SubProcess;

    public Dictionary<string, Material[]> MaterialDictionary = new Dictionary<string, Material[]>();

    // Start is called before the first frame update
    void Start()
    {
        Material[] productionMaterials = { Resources.Load("Materials/Production Cubes/Manufacturing", typeof(Material)) as Material, Resources.Load("Materials/Production Cubes/ManufacturingOpaque", typeof(Material)) as Material };
        MaterialDictionary.Add("Manufacturing", productionMaterials);

        Material[] distributionMaterials = { Resources.Load("Materials/Production Cubes/Distribution", typeof(Material)) as Material, Resources.Load("Materials/Production Cubes/DistributionOpaque", typeof(Material)) as Material };
        MaterialDictionary.Add("Distribution", distributionMaterials);

        Material[] procurementMaterials = { Resources.Load("Materials/Production Cubes/Procurement", typeof(Material)) as Material, Resources.Load("Materials/Production Cubes/ProcurementOpaque", typeof(Material)) as Material };
        MaterialDictionary.Add("Procurement", procurementMaterials);

        Material[] otherMaterials = { Resources.Load("Materials/Production Cubes/Other", typeof(Material)) as Material, Resources.Load("Materials/Production Cubes/OtherOpaque", typeof(Material)) as Material };
        MaterialDictionary.Add("Other", otherMaterials);

        try // The origin cube does not need a material assigned, this is the simplest way around that
        {
            GetComponent<MeshRenderer>().material = MaterialDictionary[MainProcess][0];
        }
        catch (KeyNotFoundException)
        {
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}
