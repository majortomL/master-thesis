using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class BlueprintTracker : MonoBehaviour
{
    public Text UpdateText;
    public Blueprint[] Blueprints;

    public int CompleteBlueprints = 0;

    private DateTime timeAllEnded;
    private bool allDone = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //int completeBlueprints = 0;

        //foreach(Blueprint blueprint in Blueprints)
        //{
        //    if (blueprint.isComplete)
        //    {
        //        completeBlueprints += 1;
        //    }
        //}

        UpdateText.text = CompleteBlueprints.ToString() + " / " + Blueprints.Length.ToString();

        if (CompleteBlueprints == Blueprints.Length && !allDone)
        {
            timeAllEnded = DateTime.Now;
            allDone = true;
            Debug.Log("All Blueprints done!");
        }

        if (allDone && (DateTime.Now - timeAllEnded).TotalSeconds > 5.0d)
        {
            Debug.Log("Quitting...");
            quitGame();
        }
    }

    void quitGame(){
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
