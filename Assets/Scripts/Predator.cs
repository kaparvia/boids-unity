using System.Collections.Generic;
using UnityEngine;

public class Predator : Boid
{
    private Boid closestPrey;

    public override void CalculateMove()
    {
        newDirection = calculateDirection();

        // If we did not get new direction, keep the old one
        if (newDirection == Vector3.zero)
        {
            newDirection = transform.forward;
        }

        speed = updateSpeed();
    }

    private Vector3 calculateDirection()
    {
        findNeighbors();

        Vector3 boundsVector = calculateBounds() * flock.boundsWeight;
        Vector3 huntVector = calculateHunt() * flock.huntWeight;

        return boundsVector + huntVector;
    }

    private float updateSpeed()
    {
        if (!closestPrey) return this.speed;

        // Predators go faster
        float newSpeed = closestPrey.speed * 1.5f;

        // Make sure we're within bounds
        newSpeed = Mathf.Clamp(newSpeed, flock.minSpeedPredator, flock.maxSpeedPredator);

        return newSpeed;
    }

    private Vector3 calculateBounds()
    {
        Vector3 boundsVector = Vector3.zero;
        Vector3 fromCenter = flock.transform.position - this.transform.position;

        if (fromCenter.magnitude > (flock.domainRadius * 0.9f))
        {
            boundsVector = fromCenter.normalized;
        }

        return boundsVector;
    }


    private Vector3 calculateHunt()
    {
        if (closestPrey == null) return Vector3.zero;

        Vector3 huntVector = closestPrey.transform.position - this.transform.position;

        if (huntVector.magnitude < flock.killDistance)
        {
            flock.Kill(closestPrey);
        }

        // Normalize
        return huntVector.normalized;
    }

    private void findNeighbors()
    {
        closestPrey = null;

        float closestDistanceSqr = 0;

        foreach (Boid boid in flock.boids)
        {
            float distanceSqr = Vector3.SqrMagnitude(boid.transform.position - this.transform.position);

            if (distanceSqr < flock.huntDistance * flock.huntDistance)
            {
                if (!closestPrey || distanceSqr < closestDistanceSqr)
                {
                    closestPrey = boid;
                    closestDistanceSqr = distanceSqr;
                }
            }
        }

    }

}
