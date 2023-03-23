using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


public class GHInput
    {
        public string componentName { get; set; }
        public string label { get; set; }
        public object value { get; set; }
        public object minValue { get; set; }
        public object maxValue { get; set; }
        public bool isValueList { get; set; }
        public List<object> listValues { get; set; }
        public List<string> listValueNames { get; set; }

        public GHInput(string _componentName, string _label)
        {
            label = _label;
            componentName = _componentName;
            isValueList = false;
            listValues = new List<object>();
            listValueNames = new List<string>();
        }

        public GHValue ToGHValue()
        {
            switch (componentName)
            {
                case "Point":
                    var array = (float[])value;
                    return new GHValue("RH_IN:" + label, new GHInnerValue("Rhino.Geometry.Point3d", "{\"X\":" + array[0] + ",\"Y\":" + array[1] + ",\"Z\":" + array[2] + "}"));
                case "Number":
                    return new GHValue("RH_IN:" + label, new GHInnerValue("System.Double", Convert.ToSingle(value).ToString()));
                case "Integer":
                    return new GHValue("RH_IN:" + label, new GHInnerValue("System.Integer", Convert.ToInt32(value).ToString()));
                case "Text":
                    return new GHValue("RH_IN:" + label, new GHInnerValue("System.String", value.ToString()));
                case "Boolean":
                    if (Convert.ToBoolean(value))
                        return new GHValue("RH_IN:" + label, new GHInnerValue("System.Boolean", "true"));
                    else
                        return new GHValue("RH_IN:" + label, new GHInnerValue("System.Boolean", "false"));
                default:
                    return null;
            }
        }

        public static List<GHValue> ToGHValueList(List<GHInput> ghInputList)
        {
            var ghValueList = new List<GHValue>();

            for (int i = 0; i < ghInputList.Count; i++)
            {
                ghValueList.Add(ghInputList[i].ToGHValue());
            }

            return ghValueList;
        }
    }

    public class GHData
    {
        public string algo { get; set; }
        public string pointer { get; set; }
        public List<GHValue> values { get; set; }

        public GHData()
        {
            values = new List<GHValue>();
        }
    }

    public class GHValue
    {
        public string ParamName { get; set; }
        public Dictionary<string, List<GHInnerValue>> InnerTree { get; set; }

        public GHValue()
        {
            InnerTree = new Dictionary<string, List<GHInnerValue>>();
        }

        public GHValue(string _ParamName, GHInnerValue innerValue)
        {
            ParamName = _ParamName;
            InnerTree = new Dictionary<string, List<GHInnerValue>>();
            InnerTree.Add("{0; }", new List<GHInnerValue> { innerValue });
        }
    }

    public class GHInnerValue
    {
        public string type { get; set; }
        public string data { get; set; }

        public GHInnerValue()
        {

        }

        public GHInnerValue(string _type, string _data)
        {
            data = _data;
            type = _type;
        }
    }

    public class Objective
    {
        public string name;
        public string category;
        public string units;
        public string type;
        public bool flip;
        public float minValue;
        public float maxValue;
        public float value;
        public float normalizedValue;

        public Objective(string _name, string _category, string _type)
        {
            name = _name;
            category = _category;
            type = _type;
            flip = false;

            if (type.Equals("Normalized"))
                units = "%";

        }

        // TODO: see whether to keep the normalized value as a struct member or to return in Normalize()
        public void Normalize()
        {
            float normalValue = value;

    //    if (type.Equals("Absolute"))  
    //    {
            if (!flip)
            {
                normalValue = (value - minValue) / (maxValue - minValue);
            }
            else
            {
                normalValue = 1 - ((value - minValue) / (maxValue - minValue));
            }
  //      }


        if (normalValue < 0)
        {
            Debug.LogError(name + normalValue);
            normalValue = 0;
        }

        if (normalValue > 1)
        {
            Debug.LogError(name + normalValue);
            normalValue = 1;
        }

        normalizedValue = normalValue; 
        }

    public float NormalizeForDisplay()
    {
        float normalValue = value;

        if (type.Equals("Absolute"))
        {
            if (!flip)
            {
                normalValue = (value - minValue) / (maxValue - minValue);

            }
            else
            {
                normalValue = 1 - ((value - minValue) / (maxValue - minValue));
            }
        }


        if (normalValue < 0)
        {
            //Debug.LogError(name + normalValue);
            normalValue = 0;
        }

        if (normalValue > 1)
        {
            //Debug.LogError(name + normalValue);
            normalValue = 1;
        }

        return normalValue;
    }



}

