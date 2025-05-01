using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : MonoBehaviour
{
    private float SPEED = 8.0f;
    private Vector2 SELF_EXTENTS;
    private MapManager _mmap;
    private Transform _target = null;
    Vector2 _home;

    List<Vector2> path;

    void Start()
    {
        // extents is the half width/height of the sprite in world units
        SELF_EXTENTS = (Vector3)transform.GetComponent<SpriteRenderer>().sprite.bounds.extents;

        // reference to the map manager for pathing
        _mmap = GameObject.Find("MapManager").GetComponent<MapManager>();

        // keep initial position as home to leash back to
        _home = transform.position;

        // Keep path between frame updates
        path = null;
    }

    void Update()
    {
        // List<Vector2> path = null;

        // if we can see the target, repath every frame as it moves (could optimize)
        if (_target != null)
        {
            path = _mmap.getPath(transform.position, _target.position);
        }

        // if no target or no path to target, path back home
        if (path == null)
        {
            path = _mmap.getPath(transform.position, _home);
        }

        // if we have a path, move to the next waypoint in it
        if (path != null)
        {
            if(path.Count == 0)
            {
                path = null;
            }
            else
            {
                // extract the next waypoint
                Vector2 waypoint = path[0];

                // standard movement integration
                Vector2 wpath = (waypoint - (Vector2)transform.position);
                float step_length = SPEED * Time.deltaTime;
                if (step_length > wpath.magnitude) {
                    // close enough, jump to waypoint to avoid jitter
                    transform.position = (Vector3)waypoint;
                    
                    // Remove waypoint once reached
                    path.RemoveAt(0);
                } else {
                    // or move towards waypoint
                    Vector2 step = wpath.normalized * step_length;
                    transform.position = transform.position + (Vector3)(step);

                    // enforce wall collision, adjust our position out of any overlap
                    Vector2 response = _mmap.checkBlockedCollision(transform.position, SELF_EXTENTS);
                    transform.position = transform.position + (Vector3)response;
                }
            }
            /*// extract the next waypoint
            Vector2 waypoint = path[0];

            // standard movement integration
            Vector2 wpath = (waypoint - (Vector2)transform.position);
            float step_length = SPEED * Time.deltaTime;
            if (step_length > wpath.magnitude) {
                // close enough, jump to waypoint to avoid jitter
                transform.position = (Vector3)waypoint;
                
                // Remove waypoint once reached
                path.RemoveAt(0);
            } else {
                // or move towards waypoint
                Vector2 step = wpath.normalized * step_length;
                transform.position = transform.position + (Vector3)(step);

                // enforce wall collision, adjust our position out of any overlap
                Vector2 response = _mmap.checkBlockedCollision(transform.position, SELF_EXTENTS);
                transform.position = transform.position + (Vector3)response;
            }*/
        }
    }

    public void OnAware(Transform agent)
    {
        // called by awareness when agent is seen
        _target = agent;
    }

    public void OnUnaware(Transform agent)
    {
        // called by awareness when agent is no longer seen (naive, only imagines one agent exists)
        _target = null;
    }

    // on touch the player, kill it (anything really, but only the player is there to trigger this)
    void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(other.gameObject);
    }
}
