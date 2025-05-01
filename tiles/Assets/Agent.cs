using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    // for collision
    private Vector2 SELF_EXTENTS;

    // movement control
    public float SPEED = 10.0f;
    private Vector2 _velocity = Vector2.zero;

    // pathfinding
    private MapManager _mmap;
    private List<Vector2> _path;
    private Vector2 _waypoint;

    // Vector2 _target = Vector2.zero;

    bool _hasTarget = false;

    // Start is called before the first frame update
    void Start()
    {
        // extents is the half width/height of the sprite in world units
        SELF_EXTENTS = (Vector3)transform.GetComponent<SpriteRenderer>().sprite.bounds.extents;

        _mmap = GameObject.Find("MapManager").GetComponent<MapManager>();
    }

    void Update()
    {
        // on click, call the pathfinding system to generate a new path from the agent to that location
        if (Input.GetMouseButtonDown(0)) {
            List<Vector2> tempPath;

            // note optional last argument is to turn on visual debugging
            // *** don't uncomment this until you start fixing the path search! it will infinite loop if you accidentally click ***
            tempPath = _mmap.getPath(transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition), true);
            // _hasTarget = true;
            
            if(tempPath != null)
            {
                _path = tempPath;
            }
        }

        // if the path is not null, move to the first waypoint in the path (just like back in lab01)
        if(_path != null)
        {
            if(_path.Count > 0)
            {
                _waypoint = _path[0];
                _hasTarget = true;
            }
        }
        // if you reach the first waypoint during this frame, remove it from the path
        // (keep in mind to check and move directly to the waypoint to avoid stepping past it)
        if(Mathf.Sqrt(Mathf.Pow(_waypoint.x - transform.position.x, 2) + Mathf.Pow(_waypoint.y - transform.position.y, 2)) < 0.1f && _hasTarget == true)
        {
            _path.RemoveAt(0);
            _hasTarget = false;
        }        

        // after moving, use the code below to fix any overlap with the blocking wall tiles

        // check collision with the wall tiles to avoid cutting corners
        Vector2 overlapResponse = _mmap.checkBlockedCollision(transform.position, SELF_EXTENTS);
        // use the overlapResponse vector to move yourself out of walls (it will be (0,0) if no overlap detected)
        transform.position = transform.position + (Vector3)overlapResponse;

        // ...
        if(_path != null) {
            Vector2 path = _waypoint - (Vector2)transform.position;
            float dist = path.magnitude;
            _velocity = path.normalized;
            float step = SPEED * Time.deltaTime;

            if(step > dist) {
                transform.position = _waypoint;
                // _hasTarget = false;
            } else {
                transform.position = (Vector2)transform.position + (_velocity * step);
            }
        }

    }
}
