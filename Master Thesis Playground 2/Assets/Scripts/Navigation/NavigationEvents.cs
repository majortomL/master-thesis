using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class NavigationEvents
{
    [System.Serializable]
    public class TransformEvent : UnityEvent<Transform> {}

    public static TransformEvent teleportButtonPressed = new TransformEvent();
}
