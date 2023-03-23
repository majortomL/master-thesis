using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class MiniatureModelizerUserStudy : MonoBehaviourPunCallbacks
{
    public GameObject Table;
    public GameObject MeshParent;
    public string TeleportableTargetParentName;
    public string AddBoxColliderToThis = "";
    [Tooltip("Note: You need to create this tag in your project first!")]
    public string TeleportableTargetTag;
    public float ratio = 1.0f;
    public float TableHeight;
    public GameObject PositionIndicator;
    public GameObject PositionIndicatorTooltip;
    public UnityEngine.UI.Text PositionIndicatorTooltipText1;
    public UnityEngine.UI.Text PositionIndicatorTooltipText2;
    public GameObject TooltipPrefab;
    public GameObject EyeCenter;
    public GameObject OriginModelCube;

    private Bounds templateBounds;
    private float tableExtent;
    public GameObject model;
    private Bounds modelBounds;
    private string meshParentName;
    private List<GameObject> tooltips = new List<GameObject>();
    public bool modelDone = false;

    // Start is called before the first frame update
    void Start()
    {
        // TODO: add support for non-quadratic tables
        tableExtent = Table.GetComponent<Renderer>().bounds.extents.x;
        meshParentName = MeshParent.name;
        PositionIndicator.SetActive(false);
        PositionIndicatorTooltip.SetActive(false);

        attachTooltip(OriginModelCube.gameObject);
        modelDone = true;
    }

    // Update is called once per frame
    void Update()
    {
        markModelPosition(MeshParent);
        if (modelDone)
        {
            if (tooltips.Count == 0) // Add the origin location
            {
                attachTooltip(OriginModelCube);
            }

            Transform modelCubes = model.transform.Find("ProductionCubes");
            foreach (Transform cube in modelCubes)
            {
                attachTooltip(cube.gameObject);
            }

            modelDone = false;
        }
    }

    // This is not called anymore in the user study
    //public void CreateMiniatureModel()
    //{
    //    MeshParent = null;
    //    MeshParent = GameObject.Find(meshParentName);

    //    addBoxColliderToProductionCubes(MeshParent);

    //    templateBounds = new Bounds();
    //    Renderer[] renderers = MeshParent.GetComponentsInChildren<Renderer>();
    //    foreach (Renderer renderer in renderers)
    //    {
    //        if (renderer.enabled)
    //        {
    //            templateBounds = renderer.bounds;
    //            break;
    //        }
    //    }

    //    foreach (Renderer renderer in renderers)
    //    {
    //        if (renderer.enabled)
    //        {
    //            templateBounds.Encapsulate(renderer.bounds);
    //        }
    //    }

    //    float largestExtent = 0.0f;

    //    if (templateBounds.extents.x > largestExtent)
    //    {
    //        largestExtent = templateBounds.extents.x;
    //    }

    //    if (templateBounds.extents.z > largestExtent)
    //    {
    //        largestExtent = templateBounds.extents.z;
    //    }

    //    float scaleFactor = tableExtent / largestExtent * 9 / 10;
    //    ratio = scaleFactor;

    //    model = Instantiate(MeshParent);
    //    model.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
    //    model.name = "Model";

    //    modelBounds = new Bounds();
    //    Renderer[] modelRenderers = model.GetComponentsInChildren<Renderer>();

    //    foreach (Renderer modelRenderer in modelRenderers)
    //    {
    //        if (modelRenderer.enabled)
    //        {
    //            modelBounds = modelRenderer.bounds;
    //        }
    //    }

    //    foreach (Renderer modelRenderer in modelRenderers)
    //    {
    //        if (modelRenderer.enabled)
    //        {
    //            modelBounds.Encapsulate(modelRenderer.bounds);
    //        }
    //    }


    //    Bounds tableBounds = Table.GetComponent<Renderer>().bounds;
    //    TableHeight = 2 * tableBounds.extents.y;

    //    model.transform.position += new Vector3(
    //        tableBounds.center.x - modelBounds.center.x,
    //        tableBounds.center.y + tableBounds.extents.y - (modelBounds.center.y - modelBounds.extents.y) + 0.001f, // add 0.001f to combat z-fighting
    //        tableBounds.center.z - modelBounds.center.z
    //    );

    //    // Change tags of the production cubes to enable them for teleportation
    //    Transform teleportableTargetParent = model.transform.Find(TeleportableTargetParentName);

    //    foreach (Transform child in teleportableTargetParent)
    //    {
    //        child.tag = TeleportableTargetTag;
    //        //child.gameObject.AddComponent<BoxCollider>();
    //    }

    //    model.transform.SetParent(gameObject.transform);

    //    if (AddBoxColliderToThis != "")
    //    {
    //        addBoxColliderToObject(MeshParent);
    //    }

    //    modelDone = true;
    //}



    // This is not called anymore in the user study

    //public void DestroyMiniatureModel()
    //{
    //    GameObject.Destroy(model);
    //    destroyTooltips();
    //}

    // This ist not called anymore in the user study
    //private void addBoxColliderToProductionCubes(GameObject meshParent)
    //{
    //    Transform productionCubes = meshParent.transform.FindChildRecursive("ProductionCubes");
    //    foreach (Transform cube in productionCubes)
    //    {
    //        cube.gameObject.AddComponent<BoxCollider>();
    //    }
    //}

    // This ist not called anymore in the user study
    //private void addBoxColliderToObject(GameObject meshParent)
    //{
    //    Transform columns = meshParent.transform.FindChildRecursive(AddBoxColliderToThis);
    //    foreach (Transform column in columns)
    //    {
    //        column.gameObject.AddComponent<BoxCollider>();
    //    }
    //}

    // This is still needed in the user study
    private void markModelPosition(GameObject meshParent)
    {
        Transform productionCubes = MeshParent.transform.FindChildRecursive("ProductionCubes");
        if (productionCubes == null || MeshParent == null)
        {
            return;
        }
        foreach (Transform cube in productionCubes)
        {
            if (cube.GetComponent<BoxCollider>() != null && cube.GetComponent<BoxCollider>().bounds.Contains(Table.transform.position))
            {
                GameObject modelCube = findProductionCubeWithSubProcess(cube.GetComponent<ProductionCube>().SubProcess, model);
                Vector3 modelCubePosition = modelCube.GetComponent<BoxCollider>().bounds.center;
                PositionIndicator.transform.position = new Vector3(modelCubePosition.x, PositionIndicator.transform.position.y, modelCubePosition.z);
                PositionIndicator.SetActive(true);
                PositionIndicatorTooltip.transform.position = new Vector3(modelCubePosition.x, PositionIndicatorTooltip.transform.position.y, modelCubePosition.z);
                PositionIndicatorTooltip.SetActive(true);
                PositionIndicatorTooltipText1.text = cube.GetComponent<ProductionCube>().MainProcess;
                PositionIndicatorTooltipText2.text = cube.GetComponent<ProductionCube>().SubProcess;
                return;
            }
        }
        PositionIndicator.SetActive(false);
        PositionIndicatorTooltip.SetActive(false);
    }

    // This is still needed in the user study
    private GameObject findProductionCubeWithSubProcess(string subProcess, GameObject searchParent)
    {
        Transform productionCubes = searchParent.transform.FindChildRecursive("ProductionCubes");

        foreach (Transform productionCube in productionCubes)
        {
            if (productionCube.GetComponent<ProductionCube>().SubProcess == subProcess)
            {
                return productionCube.gameObject;
            }
        }
        return null;
    }

    // This is still needed in the user study
    private void attachTooltip(GameObject modelCube)
    {
        Bounds modelCubeBounds = modelCube.GetComponent<BoxCollider>().bounds;
        GameObject tooltip = Instantiate(TooltipPrefab, modelCubeBounds.center + new Vector3(0, 0.4f, 0), Quaternion.identity);
        tooltip.transform.SetParent(transform);
        tooltip.SetActive(false);
        UnityEngine.UI.Text tooltipText1 = tooltip.transform.Find("Canvas").Find("TooltipText1").GetComponent<UnityEngine.UI.Text>();
        UnityEngine.UI.Text tooltipText2 = tooltip.transform.Find("Canvas").Find("TooltipText2").GetComponent<UnityEngine.UI.Text>();

        tooltipText1.text = modelCube.GetComponent<ProductionCube>().MainProcess;
        tooltipText2.text = modelCube.GetComponent<ProductionCube>().SubProcess;

        tooltip.GetComponent<ToolTip>().eyeCenter = EyeCenter;
        tooltips.Add(tooltip);
    }

    // This is not called anymore in the user study
    //private void destroyTooltips()
    //{
    //    foreach (GameObject tooltip in tooltips)
    //    {
    //        GameObject.Destroy(tooltip);
    //    }
    //    tooltips.Clear();
    //}

    // This is still needed in the user study
    public void activateTooltip(string subProcess)
    {
        foreach (GameObject tooltip in tooltips)
        {
            if (tooltip.transform.Find("Canvas").Find("TooltipText2").GetComponent<UnityEngine.UI.Text>().text == subProcess)
            {
                tooltip.SetActive(true);
            }
        }
    }

    // This is still needed in the user study
    public void deactivateTooltip(string subProcess)
    {
        foreach (GameObject tooltip in tooltips)
        {
            if (tooltip.transform.Find("Canvas").Find("TooltipText2").GetComponent<UnityEngine.UI.Text>().text == subProcess)
            {
                tooltip.SetActive(false);
            }
        }
    }

    // This is still needed in the user study
    public GameObject findTooltipWithSubProcess(string subProcess)
    {
        foreach (GameObject tooltip in tooltips)
        {
            if (tooltip.transform.Find("Canvas").Find("TooltipText2").GetComponent<UnityEngine.UI.Text>().text == subProcess)
            {
                return tooltip;
            }
        }
        return null;
    }
}
