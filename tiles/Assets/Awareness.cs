using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Awareness : MonoBehaviour
{
    Guard _guard;

    void Start()
    {
        // store a reference to the guard script this awareness works with
        _guard = transform.parent.GetComponent<Guard>();
    }

    // when something enters our awareness collider, let the guard know
    void OnTriggerEnter2D(Collider2D other)
    {
        _guard.OnAware(other.transform);
    }

    // when something exits our awareness collider, let the guard know
    void OnTriggerExit2D(Collider2D other)
    {
        _guard.OnUnaware(other.transform);
    }
}
