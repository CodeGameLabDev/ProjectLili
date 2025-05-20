using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class TracingModule : MonoBehaviour
{
    
    public GameObject LetterGameObject;
    public GameObject letterObject;

    public FollowerPen followerPen;

    [Button("Start Tracing")]
    public void StartTracing()
    {
        letterObject = Instantiate(LetterGameObject, transform);
        letterObject.GetComponent<TracingController>().followerPen = followerPen;
    }
}
