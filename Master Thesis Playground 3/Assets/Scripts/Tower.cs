using UnityEngine;
using UnityEngine.Serialization;

public class Tower : MonoBehaviour
{
    public int height = 10;
    public int objectsPerRow = 20;
    public float radius = 1.0f;
    public float additionalRotation = 22.5f;
    public int breakForce = 15;
    public Material elementMaterial;
    
    private float elementScale = 0.25f;
    private float elementMass = 0.5f;
    
    private GameObject[,] elements;

    private GameObject elementConfig; 
    void Start()
    {
        elements = new GameObject[height, objectsPerRow];
        PrepareElementConfig();
        CreateTower();
        elementConfig.SetActive(false);
        JoinElements();
    }
    
    void PrepareElementConfig()
    {
        // prepare the element based on a primitive and configure its attributes
        GameObject newElement = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newElement.name = "TowerElementConfig";
        newElement.transform.localScale = new Vector3(elementScale, elementScale, elementScale);
        newElement.GetComponent<MeshRenderer>().material = elementMaterial;
        Rigidbody rigidbody =  newElement.AddComponent<Rigidbody>();
        rigidbody.mass = elementMass;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        elementConfig = newElement;
    }

    void CreateTower()
    {
        float yOffset = elementConfig.transform.localScale.y * 0.5f; // needed to instantiate object in on top of previous one
        for (int row = 0; row < height; row++)
        {
            // apply some alternating rotation to created object to give structure more variation
            float rotation = additionalRotation;
            if (row % 2 == 0)
            {
                rotation *= -1.0f;
            }
            for (int i = 0; i < objectsPerRow; i++)
            {
                // create new objects in a circle
                float angle = i * Mathf.PI * 2 / objectsPerRow;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                Vector3 pos = transform.position + new Vector3(x, yOffset, z);
                float angleDegrees = (-angle + rotation) * Mathf.Rad2Deg;
                Quaternion rot = Quaternion.Euler(0,  angleDegrees, 0);
                GameObject newObject = Instantiate(elementConfig, pos, rot);
                newObject.name = "Element" + '-' + row + "-" + i;
                newObject.transform.parent = transform;
                elements[row, i] = newObject; // store object to later add joints
            }
            yOffset += elementConfig.transform.localScale.y;
        }
    }

    void JoinElements()
    {
        // connects each element with the one to it's right and above
        // to give structure more stability
        
        int rows = elements.GetLength(0);
        int columns = elements.GetLength(1);
        
        for (int row = 0; row < rows; row++)
        {
            for (int i = 0; i < columns; i++)
            {
                
                GameObject element = elements[row, i];
                if (element == null) continue;
                
                // connect with next brick in line
                GameObject neighborRight;
                if (i == columns - 1)
                {
                    // connect last one with first
                    neighborRight = elements[row, 0];
                }
                else
                {
                    neighborRight = elements[row, i + 1];
                }
                CreateJoint(element, neighborRight);
                
                // connect with brick above
                if (row == rows - 1) continue; // skip for top row, cannot be connected to anything
                GameObject neighborTop = elements[row + 1, i];
                CreateJoint(element, neighborTop);
            }
        }
    }

    FixedJoint CreateJoint(GameObject origin, GameObject connectedObject)
    {
        FixedJoint joint = origin.AddComponent<FixedJoint>();
        joint.connectedBody = connectedObject.GetComponent<Rigidbody>();
        joint.breakForce = breakForce;
        return joint;
    }
}
