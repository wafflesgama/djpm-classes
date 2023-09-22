using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchInputHandler : MonoBehaviour
{
    public MeshRenderer pointPlaceholder;

    public LayerMask rayMask = ~0;
    public float rayRange = 40;

    private RaycastHit hit;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //If tapped
        if (Input.touchCount == 1 && Input.touches[0].phase == TouchPhase.Ended)
        {
            var tapPos = Input.touches[0].position;
            ProcessTap(tapPos);
        }
    }

    void ProcessTap(Vector2 tapPos)
    {
        var ray = Camera.main.ScreenPointToRay(tapPos);
        bool hasHit = Physics.Raycast(ray, out hit, rayRange, rayMask, QueryTriggerInteraction.Ignore);

        if (!hasHit)
        {
            pointPlaceholder.gameObject.SetActive(false);
            return;
        };

        pointPlaceholder.gameObject.SetActive(true);
        pointPlaceholder.transform.position = hit.point;

        CharacterPointMovController.currentPlayer?.GoToPoint(hit.point);

    }
}
