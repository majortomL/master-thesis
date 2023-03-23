using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System;
using System.IO;
using System.Xml;

// Not usable on android builds
//using NPOI.SS.UserModel;
//using NPOI.XSSF.UserModel;

using UnityEngine;
using UnityEngine.EventSystems;

using GH_IO;
using GH_IO.Serialization;
using Newtonsoft.Json;
using RestSharp;

using Photon.Pun;

public class GHLoader : MonoBehaviourPun
{
    public string ghFile = "";
    public string ghInputFile = "";
    public string MOOoutputFile = "";
    public string MOOoutput_worksheet = "";

    public string serviceUrl = "http://localhost:8081";
    public GameObject meshObjPrefab;
    public GameObject transparentMeshObjPrefab;

    [Serializable]
    public struct NamedMaterial
    {
        public string name;
        public Material[] material;
    }
    public NamedMaterial[] materials;

    public Dictionary<string, Material[]> meshMaterials = new Dictionary<string, Material[]>();

    public bool playOnDesktop;

    public Transform meshParent;
    public GameObject machinePrefab;

    private string encodedData = "";
    private List<GHInput> ghInputs = new List<GHInput>();

    //TODO: inputSets from the xml file are depricated
    // private List<List<GHInput>> ghInputSets = new List<List<GHInput>>();

    private Dictionary<int, List<GHInput>> MOOInputSets = new Dictionary<int, List<GHInput>>();
    private Dictionary<int, List<Objective>> MOOObjectives = new Dictionary<int, List<Objective>>();


    private bool loaded = false;
    private bool makeMiniatureModel = false;
    private Dictionary<string, List<Rhino.Geometry.Mesh>> rhinoMeshes = new Dictionary<string, List<Rhino.Geometry.Mesh>>();

    List<Objective> Objectives = new List<Objective>();
    List<string> objNames = new List<string>();
    List<float> objValues = new List<float>();

    // Lists for the Cubes Main and Subprocesses
    List<String> mainProcesses = new List<String>();
    List<String> subProcesses = new List<String>();

    // Script Import for the Miniaturizer
    public GameObject MiniatureModelizer;

    private GH_Archive archive;
    private RestClient client;

    // Not needed in the user study
    //MainMenu UImenu;

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            // Not needed in the user study
            //UImenu = transform.GetComponent<MainMenu>();

            if (PhotonNetwork.IsMasterClient)
            {
                ReadGHFile();
                ReadGHInputs(getPath() + ghInputFile);
                //ReadGHInputSets(getPath() + ghInputFile);
                ReadObjectives(getPath() + ghInputFile);
                
                // Not needed in the user study
                //ReadExcel(getPath() + MOOoutputFile);

                this.photonView.RPC("CreateInput", RpcTarget.All);

                //Only send the first calculate call upon request
                // SendGHData();
            }

            GameObject eventSystem = GameObject.Find("EventSystem");
            if (playOnDesktop)
            {
                //eventSystem.GetComponent<OVRInputModule>().enabled = false;
            }
            else
            {
                eventSystem.GetComponent<StandaloneInputModule>().enabled = false;
                eventSystem.GetComponent<BaseInput>().enabled = false;
            }

            ReadMaterials();
        }

        if (PhotonNetwork.IsMasterClient)
        {
            //  InitializeMachines();            
        }
    }

    private string getPath()
    {
#if UNITY_EDITOR
        return Application.streamingAssetsPath;
#elif UNITY_ANDROID
        return Application.persistentDataPath;
#elif UNITY_STANDALONE_WIN
        return Application.streamingAssetsPath;
#else  
        return "";
#endif
    }


    void ReadMaterials()
    {
        for (int i = 0; i < materials.Length; i++)
        {
            meshMaterials.Add(materials[i].name, materials[i].material);
        }
    }

    void ReadGHFile()
    {
        client = new RestClient(serviceUrl);

        archive = new GH_Archive();
        archive.ReadFromFile(Application.streamingAssetsPath + "\\" + ghFile);

        var root = archive.GetRootNode;
        var def = root.FindChunk("Definition") as GH_Chunk;
        var objs = def.FindChunk("DefinitionObjects") as GH_Chunk;

        if (objs != null)
        {
            int count = objs.GetInt32("ObjectCount");

            var inputGuids = new List<Guid>();
            var inputNames = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var obj = objs.FindChunk("Object", i) as GH_Chunk;
                var container = obj.FindChunk("Container") as GH_Chunk;

                var name = container.GetString("Name");
                if (name == "Group")
                {
                    var nickname = container.GetString("NickName");
                    if (nickname.IndexOf("RH_IN:") != -1)
                    {
                        var inputname = nickname.Replace("RH_IN:", "");
                        var itemguid = container.GetGuid("ID", 0);
                        inputNames.Add(inputname);
                        inputGuids.Add(itemguid);
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                var obj = objs.FindChunk("Object", i) as GH_Chunk;
                var container = obj.FindChunk("Container") as GH_Chunk;

                var instanceguid = container.GetGuid("InstanceGuid");
                if (inputGuids.Contains(instanceguid))
                {
                    var index = inputGuids.IndexOf(instanceguid);
                    var inputName = inputNames[index];
                    var componentName = container.GetString("Name");
                    this.photonView.RPC("AddInput", RpcTarget.All, componentName, inputName);
                }
            }
        }
    }

    void ReadGHInputs(string fileToRead)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(fileToRead);

        XmlNodeList inputs = doc.DocumentElement.SelectNodes("/Inputs/Input");

        foreach (XmlNode input in inputs)
        {
            string name = (input.SelectSingleNode("Name").InnerText);
            XmlNode xmlValue = input.SelectSingleNode("Value");

            foreach (GHInput ghi in ghInputs)
            {
                if (ghi.label.Equals(name))
                {
                    //  Debug.Log("match found for " + ghi.label);
                    switch (ghi.componentName)
                    {
                        case "Point":
                            ghi.value = new float[] { float.Parse(input.SelectSingleNode("Value").SelectSingleNode("x").InnerText), float.Parse(input.SelectSingleNode("Value").SelectSingleNode("y").InnerText), float.Parse(input.SelectSingleNode("Value").SelectSingleNode("z").InnerText) };
                            break;
                        case "Number":
                            ghi.value = float.Parse(xmlValue.SelectSingleNode("currentValue").InnerText);
                            if (xmlValue.SelectSingleNode("minValue") != null && xmlValue.SelectSingleNode("maxValue") != null)
                            {
                                ghi.minValue = float.Parse(xmlValue.SelectSingleNode("minValue").InnerText);
                                ghi.maxValue = float.Parse(xmlValue.SelectSingleNode("maxValue").InnerText);

                                this.photonView.RPC("SetIntervalInput", RpcTarget.Others, ghi.label, ghi.value, ghi.minValue, ghi.maxValue);
                                //  this.photonView.RPC("SetIntervalInput", RpcTarget.All, ghi.label, ghi.value, ghi.minValue, ghi.maxValue);
                            }
                            if (xmlValue.SelectSingleNode("valueList") != null)
                            {
                                ghi.isValueList = true;
                                ghi.listValues.Clear();

                                var valueList = xmlValue.SelectSingleNode("valueList").SelectNodes("item");
                                foreach (XmlNode n in valueList)
                                {
                                    ghi.listValues.Add(float.Parse(n.InnerText));
                                }

                                var nameList = xmlValue.SelectSingleNode("valueNameList");
                                if (nameList != null)
                                {
                                    var valueNameList = nameList.SelectNodes("item");
                                    foreach (XmlNode n in valueNameList)
                                    {
                                        ghi.listValueNames.Add(n.InnerText);
                                    }
                                }

                                this.photonView.RPC("SetValueListInput", RpcTarget.Others, ghi.label, ghi.value, ghi.listValues.ToArray(), ghi.listValueNames.ToArray());
                                //  this.photonView.RPC("SetInputValue", RpcTarget.Others, ghi.label, ghi.value);
                            }

                            //    Debug.Log(ghi.label + ghi.value);
                            break;
                        case "Integer":
                            ghi.value = int.Parse(xmlValue.SelectSingleNode("currentValue").InnerText);
                            if (xmlValue.SelectSingleNode("minValue") != null && xmlValue.SelectSingleNode("maxValue") != null)
                            {
                                ghi.minValue = int.Parse(xmlValue.SelectSingleNode("minValue").InnerText);
                                ghi.maxValue = int.Parse(xmlValue.SelectSingleNode("maxValue").InnerText);

                                this.photonView.RPC("SetIntervalInput", RpcTarget.Others, ghi.label, ghi.value, ghi.minValue, ghi.maxValue);
                            }
                            if (xmlValue.SelectSingleNode("valueList") != null)
                            {
                                ghi.isValueList = true;
                                ghi.listValues.Clear();

                                var valueList = xmlValue.SelectSingleNode("valueList").SelectNodes("item");
                                foreach (XmlNode n in valueList)
                                {
                                    ghi.listValues.Add(int.Parse(n.InnerText));
                                }

                                var nameList = xmlValue.SelectSingleNode("valueNameList");
                                if (nameList != null)
                                {
                                    var valueNameList = nameList.SelectNodes("item");
                                    foreach (XmlNode n in valueNameList)
                                    {
                                        ghi.listValueNames.Add(n.InnerText);
                                    }
                                }

                                this.photonView.RPC("SetValueListInput", RpcTarget.Others, ghi.label, ghi.value, ghi.listValues.ToArray(), ghi.listValueNames.ToArray());
                            }
                            //     Debug.Log(ghi.label + ghi.value);
                            break;
                        case "Text":
                            ghi.value = xmlValue.SelectSingleNode("currentValue").InnerText;
                            if (xmlValue.SelectSingleNode("valueList") != null)
                            {
                                ghi.isValueList = true;
                                ghi.listValues.Clear();

                                var valueList = xmlValue.SelectSingleNode("valueList").SelectNodes("item");
                                foreach (XmlNode n in valueList)
                                {
                                    ghi.listValues.Add(n.InnerText);
                                }

                                var nameList = xmlValue.SelectSingleNode("valueNameList");
                                if (nameList != null)
                                {
                                    var valueNameList = nameList.SelectNodes("item");
                                    foreach (XmlNode n in valueNameList)
                                    {
                                        ghi.listValueNames.Add(n.InnerText);
                                    }
                                }

                                this.photonView.RPC("SetValueListInput", RpcTarget.Others, ghi.label, (object)ghi.value, ghi.listValues.ToArray(), ghi.listValueNames.ToArray());
                            }
                            //  Debug.Log(ghi.label + ghi.value);
                            break;
                        case "Boolean":
                            ghi.value = bool.Parse(xmlValue.SelectSingleNode("currentValue").InnerText);
                            this.photonView.RPC("SetInputValue", RpcTarget.Others, ghi.label, ghi.value);
                            // Debug.Log(ghi.label + ghi.value);
                            break;
                        default:
                            break;
                    }
                }

                continue;
            }
        }
    }

    void ReadGHInputSets(string fileToRead)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(fileToRead);

        XmlNodeList inputSets = doc.DocumentElement.SelectNodes("/Inputs/InputSet");

        foreach (XmlNode inputS in inputSets)
        {
            XmlNodeList inps = inputS.SelectNodes("Input");
            List<string> toSend = new List<string>();

            foreach (XmlNode inp in inps)
            {
                string name = (inp.SelectSingleNode("Name").InnerText);
                XmlNode xmlValue = inp.SelectSingleNode("Value");

                int ind = GetGHInputByName(name);
                if (ind > (-1))
                {
                    toSend.Add(ghInputs[ind].componentName);
                    toSend.Add(ghInputs[ind].label);
                    toSend.Add(xmlValue.InnerText);
                }
            }
            this.photonView.RPC("AddInputSet", RpcTarget.All, toSend.ToArray());
        }
    }

    // TODO: see how this function and the definition of Objective needs to be changed 
    // Min and max values are probably not needed; only the normalized values calculated from the results of MOO
    void ReadObjectives(string fileToRead)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(fileToRead);

        XmlNodeList objectives = doc.DocumentElement.SelectNodes("/Inputs/Objective");

        foreach (XmlNode o in objectives)
        {
            string name = o.SelectSingleNode("Name").InnerText;
            string category = o.SelectSingleNode("Cat").InnerText;
            string unit = o.SelectSingleNode("Unit").InnerText;
            string type = o.SelectSingleNode("Type").InnerText;
            bool flip = bool.Parse(o.SelectSingleNode("Flip").InnerText);

            this.photonView.RPC("AddObjective", RpcTarget.All, name, category, unit, type, flip);
        }
    }



    int GetGHInputByName(string inpName)
    {
        foreach (GHInput inp in ghInputs)
        {
            if (inp.label.Equals(inpName))
            {
                return ghInputs.IndexOf(inp);
            }
        }
        return -1;
    }

    int GetObjectiveByName(string objName)
    {
        foreach (Objective o in Objectives)
        {
            if (o.name.Equals(objName))
            {
                return Objectives.IndexOf(o);
            }
        }
        return -1;
    }

    void SaveGHInputs(List<GHInput> inputs, string fileToSave)
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        XmlWriter writer = XmlWriter.Create(fileToSave, settings);

        writer.WriteStartElement("Inputs");

        foreach (GHInput ghi in ghInputs)
        {
            writer.WriteStartElement("Input");

            writer.WriteElementString("Name", ghi.label);

            switch (ghi.componentName)
            {
                case "Point":
                    writer.WriteStartElement("Value");
                    writer.WriteElementString("x", ((float[])ghi.value)[0].ToString());
                    writer.WriteElementString("y", ((float[])ghi.value)[1].ToString());
                    writer.WriteElementString("z", ((float[])ghi.value)[2].ToString());
                    writer.WriteEndElement(); // for Value
                    break;
                default:
                    writer.WriteElementString("Value", ghi.value.ToString());
                    break;

            }

            writer.WriteEndElement(); // for Input
        }

        writer.WriteEndElement(); // for Inputs

        writer.Flush();
        writer.Close();
    }

    public void SetGHInputValue(int ghinputIndex, object value, bool calculateNow)
    {
        var input = ghInputs[ghinputIndex];
        switch (input.componentName)
        {
            case "Point":
                //this.photonView.RPC("SendWholePointGHInput", RpcTarget.All, ghinputIndex, (float[]) value);
                Debug.Log("point needs further implementation");
                break;
            case "Integer":
                var tmpValue = value;
                if (value.GetType() == typeof(System.Single))
                {
                    tmpValue = Convert.ToInt32(value);
                }
                this.photonView.RPC("SendIntGHInput", RpcTarget.All, ghinputIndex, (int)tmpValue, calculateNow);
                break;
            case "Number":
                //this.photonView.RPC("SendDoubleGHInput", RpcTarget.All, ghinputIndex, (float)value, calculateNow);
                this.photonView.RPC("SendDoubleGHInput", RpcTarget.All, ghinputIndex, Convert.ToSingle(value), calculateNow);
                break;
            case "Text":
                //this.photonView.RPC("SendStringGHInput", RpcTarget.All, ghinputIndex, (string)value, calculateNow);
                this.photonView.RPC("SendStringGHInput", RpcTarget.All, ghinputIndex, value.ToString(), calculateNow);
                break;
            case "Boolean":
                this.photonView.RPC("SendBoolGHInput", RpcTarget.All, ghinputIndex, (bool)value, calculateNow);
                break;
            default:
                Debug.Log("Unknow parameter type");
                break;
        }
    }

    private void InitializeMachines()
    {
        PhotonNetwork.Instantiate(this.machinePrefab.name, new Vector3(1f, 1f, 1f), Quaternion.identity, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (makeMiniatureModel)
        {
            // Commented out for user study
            //photonView.RPC("createMiniatureModel", RpcTarget.All);
        }
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (loaded && rhinoMeshes.Count > 0)
            {
                // Commented out for user study
                //this.photonView.RPC("DestroyMeshes", RpcTarget.All);


                //foreach (KeyValuePair<string, List<Rhino.Geometry.Mesh>> pair in rhinoMeshes)
                //{
                for (int j = 0; j < rhinoMeshes.Count; j++)
                {
                    KeyValuePair<string, List<Rhino.Geometry.Mesh>> pair = rhinoMeshes.ElementAt(j);
                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        var vertices = pair.Value[i].Vertices.ToList().ConvertAll(new Converter<Rhino.Geometry.Point3f, Vector3>(Point3fToVector3)).ToArray();
                        var normals = pair.Value[i].Normals.ToList().ConvertAll(new Converter<Rhino.Geometry.Vector3f, Vector3>(Vector3fToVector3)).ToArray();
                        var triangleList = new List<int>();

                        foreach (var face in pair.Value[i].Faces)
                        {
                            if (face.IsTriangle)
                            {
                                triangleList.Add(face.A);
                                triangleList.Add(face.B);
                                triangleList.Add(face.C);

                                triangleList.Add(face.C);
                                triangleList.Add(face.B);
                                triangleList.Add(face.A);
                            }
                            else
                            {
                                triangleList.Add(face.A);
                                triangleList.Add(face.B);
                                triangleList.Add(face.C);

                                triangleList.Add(face.C);
                                triangleList.Add(face.D);
                                triangleList.Add(face.A);

                                triangleList.Add(face.C);
                                triangleList.Add(face.B);
                                triangleList.Add(face.A);

                                triangleList.Add(face.A);
                                triangleList.Add(face.D);
                                triangleList.Add(face.C);
                            }
                        }
                        this.photonView.RPC("CreateMesh", RpcTarget.All, pair.Key, vertices, normals, triangleList.ToArray());
                    }
                }
                photonView.RPC("SendObjectives", RpcTarget.All, objNames.ToArray(), objValues.ToArray());

                //Activate the CALCULATE button for the next calculation 
                photonView.RPC("SetCalculateButton", RpcTarget.All, true);
                photonView.RPC("DisplayObjectives", RpcTarget.All);

                loaded = false;
                makeMiniatureModel = true;
            }
        }
    }

    /*IEnumerator SendMesh()
    {
        foreach (KeyValuePair<string, List<Rhino.Geometry.Mesh>> pair in rhinoMeshes)
        {
            for (int i = 0; i < pair.Value.Count; i++)
            {
                var vertices = pair.Value[i].Vertices.ToList().ConvertAll(new Converter<Rhino.Geometry.Point3f, Vector3>(Point3fToVector3)).ToArray();
                var normals = pair.Value[i].Normals.ToList().ConvertAll(new Converter<Rhino.Geometry.Vector3f, Vector3>(Vector3fToVector3)).ToArray();

                var triangleList = new List<int>();

                //     Debug.Log(meshes[i].Faces.Count);

                foreach (var face in pair.Value[i].Faces)
                {
                    if (face.IsTriangle)
                    {
                        triangleList.Add(face.A);
                        triangleList.Add(face.B);
                        triangleList.Add(face.C);

                        triangleList.Add(face.C);
                        triangleList.Add(face.B);
                        triangleList.Add(face.A);
                    }
                    else
                    {
                        triangleList.Add(face.A);
                        triangleList.Add(face.B);
                        triangleList.Add(face.C);

                        triangleList.Add(face.C);
                        triangleList.Add(face.D);
                        triangleList.Add(face.A);

                        triangleList.Add(face.C);
                        triangleList.Add(face.B);
                        triangleList.Add(face.A);

                        triangleList.Add(face.A);
                        triangleList.Add(face.D);
                        triangleList.Add(face.C);
                    }
                }

                this.photonView.RPC("CreateMesh", RpcTarget.All, pair.Key, vertices, normals, triangleList.ToArray());

                yield return null;
            }
        }
    }*/


    public static Vector3 Point3fToVector3(Rhino.Geometry.Point3f pf)
    {
        return new Vector3(pf.X, pf.Z, pf.Y);
    }

    public static Vector3 Vector3fToVector3(Rhino.Geometry.Vector3f pf)
    {
        return new Vector3(pf.X, pf.Z, pf.Y);
    }


    private void SendGHData()
    {
        if (loaded == false)
        {
            //  loaded = true;

            //    meshes.Clear();
            foreach (KeyValuePair<string, List<Rhino.Geometry.Mesh>> pair in rhinoMeshes)
            {
                pair.Value.Clear();
            }
            rhinoMeshes.Clear();
            objNames.Clear();
            objValues.Clear();


            // productionMeshes.Clear();

            var request = new RestRequest("/grasshopper/", Method.POST);

            var json = ParseToJson();

            //  System.IO.File.WriteAllText(@"C:\Dev\BimFlexi\GHLoaderVR\Assets\StreamingAssets\test.txt", json);

            request.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;

            try
            {
                client.ExecuteAsync(request, response =>
                {
                    Debug.Log("RESPONSE " + response.StatusCode);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        DeserializeRhinoResponse(response.Content);
                    }
                });
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }
    }

    private string ParseToJson()
    {
        var bytes = archive.Serialize_Binary();
        encodedData = Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks);


        var ghData = new GHData();
        ghData.algo = encodedData;
        ghData.values = GHInput.ToGHValueList(ghInputs);

        var json = JsonConvert.SerializeObject(ghData);

        return json;
    }

    private void DeserializeRhinoResponse(string responsecontent)
    {
        //File.WriteAllText("jsonResponse.txt", responsecontent);

        long size = 0;
        using (Stream s = new MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(s, responsecontent);
            size = s.Length;
        }

        Debug.Log("json string size " + size);

        var ghOut = JsonConvert.DeserializeObject<GHData>(responsecontent);

        foreach (var ghOutVal in ghOut.values)
        {
            string paramName = ghOutVal.ParamName.Replace("RH_OUT:", "");
            //  Debug.Log("OUTPUT " + paramName);

            foreach (var set in ghOutVal.InnerTree)
            {
                var ghInnerVals = set.Value;

                foreach (var ghInnerVal in ghInnerVals)
                {
                    if (ghInnerVal.type == "Rhino.Geometry.Mesh")
                    {
                        var mesh = JsonConvert.DeserializeObject<Rhino.Geometry.Mesh>(ghInnerVal.data);

                        if (!rhinoMeshes.ContainsKey(paramName))
                        {
                            List<Rhino.Geometry.Mesh> paramMeshes = new List<Rhino.Geometry.Mesh>();
                            rhinoMeshes.Add(paramName, paramMeshes);
                        }

                        rhinoMeshes[paramName].Add(mesh);

                    }
                    else if (ghInnerVal.type == "System.Double")
                    {
                        float numberOutput = (float)JsonConvert.DeserializeObject<System.Double>(ghInnerVal.data);

                        objNames.Add(paramName);
                        objValues.Add(numberOutput);
                    }
                    // Filters for Output of String type and sorts into the according Lists for Main and Sub Processes
                    else if (ghInnerVal.type == "System.String")
                    {
                        String stringOutput = (String)JsonConvert.DeserializeObject<System.String>(ghInnerVal.data);

                        if (paramName == "MainProcess")
                        {
                            mainProcesses.Add(stringOutput);
                        }
                        else if (paramName == "SubProcess")
                        {
                            subProcesses.Add(stringOutput);
                        }
                    }
                }
            }
        }

        SendProcesses();

        loaded = true; // IMPORTANT: only declare loaded true here so that Unity mesh creation does not start before all Rhino meshes are deserialized
        // IMPORTANT: do not add any functionality here since it interrupts unpacking
        if (rhinoMeshes.Count == 0)
        {
            loaded = false;
        }

    }

    private void SendProcesses()
    {
        String mainProcessesString = string.Join("§", mainProcesses);
        String subProcessesString = string.Join("§", subProcesses);
        String processesString = mainProcessesString + "#" + subProcessesString;

        this.photonView.RPC("SynchronizeProcesses", RpcTarget.Others, processesString);
    }

    [PunRPC]
    private void SynchronizeProcesses(String processes) // Synchronizes the processes lists, "." as separator between the two variables, "," as separator between list items
    {
        string mainProcessesString = processes.Split('#')[0];
        string subProcessesString = processes.Split('#')[1];

        mainProcesses = mainProcessesString.Split('§').ToList();
        subProcesses = subProcessesString.Split('§').ToList();
    }


    private MeshRenderer SetupMesh(GameObject prefab, Transform parent, Vector3[] vertices, Vector3[] normals, int[] triangles)
    {
        var go = (GameObject)Instantiate(prefab);
        var unityMesh = go.GetComponent<MeshFilter>().mesh;
        unityMesh.vertices = vertices;
        unityMesh.normals = normals;
        unityMesh.triangles = triangles;
        go.transform.SetParent(parent.transform);
        Renderer meshRenderer = go.GetComponent<MeshRenderer>();
        unityMesh.RecalculateBounds();

        return (MeshRenderer)meshRenderer;
    }


    [PunRPC]
    public void DestroyMeshes()
    {
        MiniatureModelizer.GetComponent<MiniatureModelizer>().DestroyMiniatureModel();
        var objs = GameObject.FindGameObjectsWithTag("Object");
        //foreach (var obj in objs)
        //{
        //    Destroy(obj);
        //}

        GameObject meshParent = GameObject.Find("MeshParent");
        foreach (Transform child in meshParent.transform)
        {
            foreach (Transform childchild in child)
            {
                Destroy(childchild.gameObject);
            }
        }
    }

    [PunRPC]
    public void CreateMesh(string group, Vector3[] vertices, Vector3[] normals, int[] triangles)
    {
        GameObject parent = GameObject.Find(group);
        // Create an empty parent for all meshes belonging to one output
        if (!parent)
        {
            parent = new GameObject();
            parent.name = group;
            parent.transform.position = Vector3.zero;
            parent.transform.rotation = Quaternion.identity;
            parent.transform.parent = meshParent;
        }

        if (!group.Equals("ProductionCubes"))
        {
            var unityMeshRenderer = SetupMesh(meshObjPrefab, parent.transform, vertices, normals, triangles);
            unityMeshRenderer.material = meshMaterials["Default"][0];

            foreach (KeyValuePair<string, Material[]> pair in meshMaterials)
            {
                if (group.Equals(pair.Key))
                {
                    switch (group)
                    {
                        case "PrimaryLoadBearingStructure":
                            int inputInd = GetGHInputByName("PrimaryStructureType");
                            int inputValue = Int32.Parse((string)ghInputs[inputInd].value);
                            unityMeshRenderer.material = pair.Value[inputValue - 1];
                            break;
                        case "SecondaryLoadBearingStructure":
                            inputInd = GetGHInputByName("SecondaryStructureType");
                            inputValue = Int32.Parse((string)ghInputs[inputInd].value);
                            unityMeshRenderer.material = pair.Value[inputValue - 1];
                            break;
                        default:
                            unityMeshRenderer.material = pair.Value[0];
                            break;
                    }


                }
            }
        }
        else
        {
            var unityMeshRenderer = SetupMesh(transparentMeshObjPrefab, parent.transform, vertices, normals, triangles);
            unityMeshRenderer.material = meshMaterials["Default"][0];

            // Set the name of the new production Cube to its sub Process
            //unityMeshRenderer.gameObject.name = 

            foreach (KeyValuePair<string, Material[]> pair in meshMaterials)
            {
                if (group.Equals(pair.Key))
                {
                    unityMeshRenderer.material = pair.Value[0];
                    break;
                }
            }

            // Sets the names of the production cubes
            GameObject productionCubes = GameObject.Find("ProductionCubes");

            for (int i = 0; i < productionCubes.transform.childCount; i++) // Iterates through all the production cubes 
            {
                //Debug.Log(subProcesses.ElementAt<String>(i));
                GameObject productionCube = productionCubes.transform.GetChild(i).gameObject;

                if (productionCube.GetComponent<ProductionCube>() == null)
                {
                    productionCube.AddComponent<ProductionCube>();

                    // Setting the variables for the Cubes' purposes
                    productionCube.GetComponent<ProductionCube>().MainProcess = mainProcesses.ElementAt<String>(i);
                    productionCube.GetComponent<ProductionCube>().SubProcess = subProcesses.ElementAt<String>(i);
                }

                // Setting the Cubes' names according to their main processes
                productionCube.name = mainProcesses.ElementAt<String>(i);

                // Setting the Cubes' tags for making them teleportable
                //if (productionCube.tag != "Teleportable")
                //{
                //    productionCube.tag = "Teleportable";
                //}
            }
        }
    }

    [PunRPC]
    public void AddInput(string i_componentName, string i_label)
    {
        var input = new GHInput(i_componentName, i_label);
        ghInputs.Add(input);
    }

    // TODO: add new MOOInputSet
    [PunRPC]
    public void AddInputSet(string[] i_values)
    {
        List<GHInput> newInputList = new List<GHInput>();
        for (int i = 0; i < (i_values.Length - 2); i += 3)
        {
            string componentName = i_values[i];
            string label = i_values[i + 1];
            string value = i_values[i + 2];
            GHInput newInput = new GHInput(componentName, label);

            switch (componentName)
            {
                case "Point":
                    Debug.LogError("Point u");
                    break;
                case "Number":
                    newInput.value = float.Parse(value);
                    break;
                case "Integer":
                    newInput.value = int.Parse(value);
                    break;
                case "Text":
                    newInput.value = value;
                    break;
                case "Boolean":
                    newInput.value = bool.Parse(value);
                    break;
                default:
                    break;
            }

            foreach (var inp in ghInputs)
            {
                if (inp.label.Equals(label))
                {
                    if (inp.listValueNames.Count > 0)
                    {
                        newInput.listValueNames = new List<string>(inp.listValueNames);
                        newInput.listValues = new List<object>(inp.listValues);
                    }
                    break;
                }
            }

            newInputList.Add(newInput);
        }

        //TODO: adapt to MOOInputSets
        // ghInputSets.Add(newInputList);
    }


    [PunRPC]
    void AddObjective(string name, string category, string units, string type, bool flip)
    {
        var o = new Objective(name, category, type);

        o.flip = flip;
        o.units = units;

        /*
        if (o.type.Equals("Absolute"))
        {
            o.minValue = minValue;
            o.maxValue = maxValue;
        }
        */

        Objectives.Add(o);
    }

    [PunRPC]
    void DisplayObjectives()
    {
        // Not needed in the user study
        //UImenu.DisplayObjectives(Objectives);
    }

    [PunRPC]
    void SendObjectives(string[] names, float[] values)
    {
        for (int i = 0; i < names.Length; i++)
        {
            foreach (var o in Objectives)
            {
                if (o.name.Equals(names[i]))
                {
                    o.value = values[i];
                    break;
                }
            }
        }
    }

    [PunRPC]
    public void SetValueListInput(string i_label, object i_value, object[] i_values, string[] value_names)
    {
        foreach (GHInput inp in ghInputs)
        {
            if (inp.label.Equals(i_label))
            {
                inp.value = i_value;
                inp.isValueList = true;
                inp.listValues = i_values.ToList<object>();
                inp.listValueNames = value_names.ToList<string>();
                break;
            }
        }
    }

    [PunRPC]
    public void SetIntervalInput(string i_label, object i_value, object i_minValue, object i_maxValue)
    {
        foreach (GHInput inp in ghInputs)
        {
            if (inp.label.Equals(i_label))
            {
                inp.value = i_value;
                inp.minValue = i_minValue;
                inp.maxValue = i_maxValue;
                break;
            }
        }
    }

    [PunRPC]
    public void SetInputValue(string i_label, object i_value)
    {
        foreach (GHInput inp in ghInputs)
        {
            if (inp.label.Equals(i_label))
            {
                inp.value = i_value;
                break;
            }
        }
    }


    [PunRPC]
    void SendIntGHInput(int ghinputIndex, int value, bool calculateNow)
    {
        ghInputs[ghinputIndex].value = value;
        // Not needed in the user study
        //UImenu.UpdateSlider(ghinputIndex, value);
        //UImenu.UpdateButton(ghinputIndex, value);
    }

    [PunRPC]
    void SetSetButtons(int buttonIndex)
    {
        // Not needed in the user study
        //UImenu.UpdateSetButtons(buttonIndex);
    }

    [PunRPC]
    void SendStringGHInput(int ghinputIndex, string value, bool calculateNow)
    {
        ghInputs.ElementAt(ghinputIndex).value = value;
        // Not needed in the user study
        //UImenu.UpdateButton(ghinputIndex, value);
    }

    [PunRPC]
    void SendBoolGHInput(int ghinputIndex, bool value, bool calculateNow)
    {
        ghInputs.ElementAt(ghinputIndex).value = value;
        // Not needed in the user study
        //UImenu.UpdateToggle(ghinputIndex, value);
    }

    [PunRPC]
    void SendDoubleGHInput(int ghinputIndex, float value, bool calculateNow)
    {
        ghInputs.ElementAt(ghinputIndex).value = value;

        // Not needed in the user study
        //UImenu.UpdateSlider(ghinputIndex, value);
        //UImenu.UpdateButton(ghinputIndex, value);
    }

    [PunRPC]
    void SendPointGHInput(int ghinputIndex, float value, int coordinate, bool calculateNow)
    {
        float[] newValue = (float[])ghInputs.ElementAt(ghinputIndex).value;
        newValue[coordinate] = value;
        ghInputs.ElementAt(ghinputIndex).value = newValue;
    }

    [PunRPC]
    void SendWholePointGHInput(int ghinputIndex, float[] value, bool calculateNow)
    {
        ghInputs.ElementAt(ghinputIndex).value = value;

    }

    [PunRPC]
    public void UpdateWeightSlider(string objectiveName, float value)
    {
        // Not needed in the user study
        //UImenu.UpdateWeightSlider(objectiveName, value);
    }

    [PunRPC]
    public void SetFitnessValue(string name, float value)
    {
        // fitness = value;
        // Debug.Log("FITNESS " + fitness);
    }

    [PunRPC]
    public void SetNumberOutputs(object[] outputs)
    {
        //  UImenu.SetNumberOutputs(outputs);
        //   UImenu.UpdateObjectivesChart(outputs);

    }

    [PunRPC]
    public void CreateInput()
    {
        // Not needed in the user study
        //UImenu.CreateInputCanvas(ghInputs, Objectives);
    }

    [PunRPC]
    public void DisplayOptimalSets(int[] setIDs)
    {
        // Not needed in the user study
        //UImenu.DisplayOptimalSetButtons(setIDs);
    }

    [PunRPC]
    public void CalculateStructure()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            //Deactivate the CALCULATE button 
            this.photonView.RPC("SetCalculateButton", RpcTarget.All, false);
            SendGHData();
        }
    }

    [PunRPC]
    public void SetCalculateButton(bool state)
    {
        // Not needed in the user study
        //UImenu.SetCalculateButton(state);
    }

    public void Calculate()
    {
        this.photonView.RPC("CalculateStructure", RpcTarget.All);
    }


    public void RequestSetInputsFromSet(int setID)
    {
        this.photonView.RPC("SetInputsFromSet", RpcTarget.All, setID);
    }

    [PunRPC]
    public void SetInputsFromSet(int setID)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (MOOInputSets.ContainsKey(setID))
            {
                foreach (GHInput setInp in MOOInputSets[setID])
                {
                    int realInputInd = GetGHInputByName(setInp.label);
                    var value = setInp.value;

                    SetGHInputValue(realInputInd, value, false);
                }
            }
        }
    }


    // Not applicable on user study build
    // TODO: this currently only happens on the host, sync the MOOObjectives to all clients (or always perform MOO calculations on the host?)
    //private void ReadExcel(string filename)
    //{
    //    try
    //    {
    //        // output area in the excel file
    //        string workSheetName = MOOoutput_worksheet;
    //        // Read in from the excel file
    //        XSSFWorkbook xlWorkbook;

    //        using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    //        {
    //            //Print("Open excel file.\n");
    //            xlWorkbook = new XSSFWorkbook(file);
    //        }

    //        ISheet xlWorksheet = xlWorkbook.GetSheet(workSheetName);
    //        int rowStartNum = 0;
    //        int columnStartNum = 0;

    //        // auxiliary dics to help save inputs and objectives from excel sheet rows and colums into correct entries
    //        // int corresponds to the column number
    //        Dictionary<int, string> inputNames = new Dictionary<int, string>();
    //        Dictionary<int, string> objectiveNames = new Dictionary<int, string>();
    //        int IDcolumnNum = -1;

    //        // read header
    //        var headerRow = xlWorksheet.GetRow(rowStartNum);
    //        int headerColumnNum = columnStartNum;
    //        var headerCell = headerRow.GetCell(headerColumnNum);
    //        while (headerCell != null && headerCell.CellType != CellType.Blank && !headerCell.ToString().Equals(""))
    //        {
    //            //Debug.Log(headerCell.ToString());
    //            if (headerCell.ToString().Equals("ID"))
    //            {
    //                IDcolumnNum = headerColumnNum;
    //                //Debug.Log("ID column " + IDcolumnNum);
    //            }

    //            // look for the cell name in the list of inputs
    //            foreach (var inp in ghInputs)
    //            {
    //                if (inp.label.Equals(headerCell.ToString()))
    //                {
    //                    inputNames.Add(headerColumnNum, inp.label);
    //                    break;
    //                }
    //            }
    //            foreach (var o in Objectives)
    //            {
    //                if (o.name.Equals(headerCell.ToString()))
    //                {
    //                    objectiveNames.Add(headerColumnNum, o.name);
    //                    break;
    //                }
    //            }

    //            headerColumnNum++;
    //            headerCell = headerRow.GetCell(headerColumnNum);
    //        }

    //        // read data
    //        int rowNum = rowStartNum + 1;
    //        var row = xlWorksheet.GetRow(rowNum);

    //        while (row != null)
    //        {
    //            List<GHInput> MOOinputs = new List<GHInput>();
    //            List<Objective> MOOobjectives = new List<Objective>();
    //            int outputID = -1;

    //            bool emptyRow = true;
    //            int columnNum = columnStartNum;
    //            var cell = row.GetCell(columnNum);

    //            while (cell != null && cell.CellType == CellType.Numeric)
    //            {
    //                if (columnNum == IDcolumnNum)
    //                {
    //                    outputID = (int)cell.NumericCellValue;
    //                }
    //                if (inputNames.ContainsKey(columnNum))
    //                {
    //                    var value = cell.NumericCellValue;

    //                    var inputName = inputNames[columnNum];
    //                    var inp = ghInputs.ElementAt(GetGHInputByName(inputName));
    //                    var ghInp = new GHInput(inp.componentName, inputName);
    //                    ghInp.value = value;
    //                    MOOinputs.Add(ghInp);
    //                }
    //                if (objectiveNames.ContainsKey(columnNum))
    //                {
    //                    var value = cell.NumericCellValue;

    //                    var objName = objectiveNames[columnNum];
    //                    var o = Objectives.ElementAt(GetObjectiveByName(objName));
    //                    var obj = new Objective(objName, o.category, o.type);
    //                    obj.value = (float)value;
    //                    MOOobjectives.Add(obj);
    //                }

    //                columnNum++;
    //                cell = row.GetCell(columnNum);
    //                emptyRow = false;
    //            }

    //            MOOInputSets.Add(outputID, MOOinputs);
    //            MOOObjectives.Add(outputID, MOOobjectives);

    //            // if the row was not empty, write the data to the output
    //            // otherwise break the loop, because the remaining rows will
    //            // will be empty too
    //            if (!emptyRow)
    //            {
    //                rowNum++;
    //                row = xlWorksheet.GetRow(rowNum);
    //            }
    //            else
    //            {
    //                break;
    //            }

    //        }

    //        /*
    //        Debug.Log("MOO input sets " + MOOInputSets.Count);
    //        foreach(KeyValuePair<int, List<GHInput>> set in MOOInputSets)
    //        { Debug.Log(set.Key + " " + set.Value.Count); }
    //        Debug.Log("MOO objective sets " + MOOObjectives.Count);
    //        foreach (KeyValuePair<int, List<Objective>> set in MOOObjectives)
    //        { Debug.Log(set.Key + " " + set.Value.Count); }
    //        */

    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogError(ex.ToString());
    //    }

    //    NormalizeObjectives();
    //}

    private void NormalizeObjectives()
    {
        int numberOfObjectives = Objectives.Count; // number of objectives, each dict entry in MOOObjectives should contain as many

        for (int i = 0; i < numberOfObjectives; i++)
        {
            Objectives[i].minValue = System.Single.MaxValue;
            Objectives[i].maxValue = -System.Single.MaxValue;
        }

        foreach (KeyValuePair<int, List<Objective>> pair in MOOObjectives)
        {
            var objList = pair.Value;
            foreach (Objective o in objList)
            {
                var name = o.name;

                foreach (var obj in Objectives)
                {
                    if (obj.name.Equals(name))
                    {
                        // TODO: use the current min and mac values of every objective from the Objectives list as a temporary container - they should be tru min and max un the end 
                        obj.minValue = Math.Min(obj.minValue, o.value);
                        obj.maxValue = Math.Max(obj.maxValue, o.value);
                    }
                }
            }
        }

        foreach (KeyValuePair<int, List<Objective>> pair in MOOObjectives)
        {
            var objList = pair.Value;
            foreach (Objective o in objList)
            {
                var name = o.name;

                foreach (var obj in Objectives)
                {
                    if (obj.name.Equals(name))
                    {
                        o.minValue = obj.minValue;
                        o.maxValue = obj.maxValue;
                        o.Normalize();
                    }
                }
            }
        }

        List<string> names = new List<string>();
        List<float> min = new List<float>();
        List<float> max = new List<float>();

        foreach (Objective o in Objectives)
        {
            names.Add(o.name);
            min.Add(o.minValue);
            max.Add(o.maxValue);

            Debug.Log(o.name + " min " + o.minValue + " max " + o.maxValue);
        }

        this.photonView.RPC("UpdateObjectivesMinMaxValues", RpcTarget.Others, names.ToArray(), min.ToArray(), max.ToArray());
    }

    [PunRPC]
    public void UpdateObjectivesMinMaxValues(string[] names, float[] min, float[] max)
    {
        for (int i = 0; i < names.Length; i++)
        {
            foreach (Objective o in Objectives)
            {
                if (o.name.Equals(names[i]))
                {
                    o.minValue = min[i];
                    o.maxValue = max[i];
                    break;
                }
            }
        }
    }


    private List<int> WeighAndSortObjectives(Dictionary<string, float> weights)
    {
        List<int> optimalSetIDs = new List<int>();

        List<int> keys = new List<int>();
        List<float> values = new List<float>();


        foreach (KeyValuePair<int, List<Objective>> pair in MOOObjectives)
        {
            // optimalSetIDs.Add(pair.Key); // TODO: this is mock filling of the list only, add the actual weighing and sorting algorithm

            float cumulativeWeight = 0.0f;

            foreach (Objective o in pair.Value)
            {
                foreach (KeyValuePair<string, float> ppair in weights)
                {
                    if (ppair.Key.Equals(o.name))
                    {
                        cumulativeWeight += o.normalizedValue * ppair.Value;
                    }
                }
            }

            keys.Add(pair.Key);
            values.Add(cumulativeWeight);
            // Debug.Log("CUMULATIVE WEIGHT FOR SET " + pair.Key + " " + cumulativeWeight);
        }

        float[] keysArray = values.ToArray();
        int[] valuesArray = keys.ToArray();

        Array.Sort(keysArray, valuesArray);
        //  Array.Reverse(valuesArray);
        //  Array.Reverse(keysArray);

        int counter = -1;
        if (valuesArray.Length <= 5)
            counter = valuesArray.Length;
        else
            counter = 5;

        for (int i = 0; i < counter; i++)
        {
            Debug.Log(i + " ID " + valuesArray[i] + " obj " + keysArray[i]);
            optimalSetIDs.Add(valuesArray[i]);
        }

        return optimalSetIDs;
    }

    [PunRPC]
    public void DisplayWeightedParamSets()
    {
        if (PhotonNetwork.IsMasterClient) // do we do this on one machine only?
        {
            // Not needed in the user study
            //var IDs = WeighAndSortObjectives(UImenu.GetObjectivesWeights());
            //this.photonView.RPC("DisplayOptimalSets", RpcTarget.All, IDs.ToArray());
        }
    }

    public void WeightedParamSetsRequest()
    {
        this.photonView.RPC("DisplayWeightedParamSets", RpcTarget.All);
    }


    [PunRPC]
    private void createMiniatureModel()
    {
        MiniatureModelizer.GetComponent<MiniatureModelizer>().CreateMiniatureModel();
        makeMiniatureModel = false;
    }


}

