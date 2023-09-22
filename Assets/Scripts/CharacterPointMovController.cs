using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CharacterPointMovController : MonoBehaviour
{
    public static CharacterPointMovController currentPlayer;

    public NavMeshAgent agent;


    private void Awake()
    {
        currentPlayer = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoToPoint(Vector3 point)
    {
        agent.SetDestination(point);
    }
}
