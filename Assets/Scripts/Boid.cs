using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    // Rotation dampening
    [Range(0.1f, 2f)]
    [SerializeField] 
    private float smoothTime;
    private Vector3 currentVelocity;

    public Flock flock;

    // Movement
    public Vector3 newDirection;
    public float speed;

    public virtual void CalculateMove()
    {
        // Not needed for Boids, all work done in Jobs system
    }

    public void Move()
    {
        // Dampen direction changes
        newDirection = Vector3.SmoothDamp(transform.forward, newDirection, ref this.currentVelocity, this.smoothTime);

        // Normalize so we get just direction
        newDirection = newDirection.normalized;

        // Update where we're pointing and move us forward
        transform.forward = newDirection;
        transform.position += newDirection * speed * Time.deltaTime;
    }
}
