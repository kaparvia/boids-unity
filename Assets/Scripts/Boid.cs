using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Boid : MonoBehaviour
{
    // Rotation dampening
    [Range(0.1f, 2f)]
    [SerializeField] 
    private float smoothTime;
    private Vector3 currentVelocity;

    public Flock flock { get; set; }

    public Vector3 newDirection;
    public float speed;

    protected List<Boid> cohesionNeighbors;
    protected List<Boid> avoidanceNeighbors;
    protected List<Boid> alignmentNeighbors;
    protected List<Boid> predatorNeighbors;

    public void CalculateMove()
    {
        newDirection = calculateDirection();
        speed = updateSpeed();
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


    protected virtual Vector3 calculateDirection()
    {
        updateNeighbors();

        Vector3 cohesionVector = calculateCohesion() * flock.cohesionWeight;
        Vector3 avoidanceVector = calculateAvoidance() * flock.avoidanceWeight;
        Vector3 alignmentVector = calculateAlignment() * flock.alignmentWeight;
        // Vector3 predatorAvoidanceVector = CalculateHunterAvoidance() * flock.predatorAvoidanceWeight;
        Vector3 boundsVector = calculateBounds() * flock.boundsWeight;

        return cohesionVector + avoidanceVector + alignmentVector + boundsVector;
        //return cohesionVector + avoidanceVector + alignmentVector + predatorAvoidanceVector + boundsVector;
    }



    protected virtual float updateSpeed()
    {
        if (cohesionNeighbors.Count == 0) return this.speed;

        // Average speed over neighbors
        float newSpeed = 0;

        foreach (Boid boid in cohesionNeighbors)
        {
            newSpeed += boid.speed;
        }

        newSpeed /= cohesionNeighbors.Count;

        // Make sure we're within bounds
        newSpeed = Mathf.Clamp(newSpeed, flock.minSpeed, flock.maxSpeed);

        return newSpeed;
    }


    protected Vector3 calculateBounds()
    {
        Vector3 boundsVector = Vector3.zero;
        Vector3 fromCenter = flock.transform.position - this.transform.position;

        if (fromCenter.magnitude > (flock.domainRadius * 0.9f))
        {
            boundsVector = fromCenter.normalized;
        }

        return boundsVector;
    }

    protected Vector3 calculateCohesion()
    {
        if (cohesionNeighbors.Count == 0) return Vector3.zero;

        Vector3 cohesionVector = Vector3.zero;

        // Sum up positions of all neighbors to find the average center of the surrounding flock
        foreach(Boid boid in cohesionNeighbors)
        {
            cohesionVector += boid.transform.position;
        }

        // Average
        cohesionVector /= cohesionNeighbors.Count;

        // Point from current position towards flock center
        cohesionVector -= this.transform.position;

        // Normalize
        return cohesionVector.normalized;
    }

    protected Vector3 calculateAvoidance()
    {
        if (avoidanceNeighbors.Count == 0) return Vector3.zero;

        Vector3 avoidanceVector = Vector3.zero;

        // Sum up vectors pointing from boid to neighbors
        foreach (Boid boid in avoidanceNeighbors)
        {
            avoidanceVector += this.transform.position - boid.transform.position;
        }

        // Average
        avoidanceVector /= avoidanceNeighbors.Count;

        // Normalize
        return avoidanceVector.normalized;
    }

    protected Vector3 calculateAlignment()
    {
        if (alignmentNeighbors.Count == 0) return transform.forward;

        // By default keep pointing in the same direction
        Vector3 alignmentVector = transform.forward;

        // Sum up directions all surrounding boids are pointing
        foreach (Boid boid in alignmentNeighbors)
        {
            alignmentVector += boid.transform.forward;
        }

        // Average
        alignmentVector /= alignmentNeighbors.Count;

        // Normalize
        return alignmentVector.normalized;
    }

    protected Vector3 calculateHunterAvoidance()
    {
        if (predatorNeighbors.Count == 0) return Vector3.zero;

        Vector3 avoidanceVector = Vector3.zero;

        // Sum up vectors pointing from boid to neighbors
        foreach (Boid boid in predatorNeighbors)
        {
            avoidanceVector += this.transform.position - boid.transform.position;
        }

        // Average
        avoidanceVector /= predatorNeighbors.Count;

        // Normalize
        return avoidanceVector.normalized;
    }

    protected virtual void updateNeighbors()
    {
        cohesionNeighbors  = new List<Boid>();
        avoidanceNeighbors = new List<Boid>();
        alignmentNeighbors = new List<Boid>();
        //predatorNeighbors    = new List<Boid>();

        // Find normal boid neighbors
        foreach (Boid boid in flock.boids)
        {
            if (boid == this) continue;

            float distanceSqr = Vector3.SqrMagnitude(boid.transform.position - this.transform.position);

            if (distanceSqr < (flock.cohesionDistance * flock.cohesionDistance)) cohesionNeighbors.Add(boid);
            if (distanceSqr < (flock.avoidanceDistance * flock.avoidanceDistance)) avoidanceNeighbors.Add(boid);
            if (distanceSqr < (flock.alignmentDistance * flock.alignmentDistance)) alignmentNeighbors.Add(boid);
        }

        // Find predators
        //foreach (Boid boid in flock.predatorBoids)
        //{
        //    float distanceSqr = Vector3.SqrMagnitude(boid.transform.position - this.transform.position);
        //    if (distanceSqr < (flock.predatorAvoidanceDistance * flock.predatorAvoidanceDistance)) predatorNeighbors.Add(boid);
        //}
    }


    void OnCollisionEnter(Collision collision)
    {
        //if (!this.flock.isHunter) return;

        Debug.Log("********************** BangBang! ", collision.collider); 
    }

}
