using System.Collections.Generic;
using UnityEngine;

public class Predator : Boid
{
    protected List<Boid> preyNeighbors;
    protected Boid closestPrey;


    protected override Vector3 calculateDirection()
    {
        updateNeighbors();

        Vector3 boundsVector = calculateBounds() * flock.boundsWeight;
        Vector3 huntVector = calculateHunt() * flock.huntWeight;
        Vector3 terminalHuntVector = calculateTerminalHunt() * flock.terminalHuntWeight;

        return boundsVector + huntVector + terminalHuntVector;
    }

    protected override float updateSpeed()
    {
        if (preyNeighbors.Count == 0) return this.speed;

        // Average speed over neighbors
        float newSpeed = 0;

        foreach (Boid boid in preyNeighbors)
        {
            newSpeed += boid.speed;
        }

        newSpeed /= preyNeighbors.Count;

        // Predators go faster
        newSpeed *= 1.25f;

        // Make sure we're within bounds
        newSpeed = Mathf.Clamp(newSpeed, flock.minSpeedPredator, flock.maxSpeedPredator);

        return newSpeed;
    }

    private Vector3 calculateHunt()
    {
        if (preyNeighbors.Count == 0) return Vector3.zero;

        Vector3 preyVector = Vector3.zero;

        // Sum up positions of all neighbors to find the average center of the surrounding flock
        foreach (Boid boid in preyNeighbors)
        {
            preyVector += boid.transform.position;
        }

        // Average
        preyVector /= preyNeighbors.Count;

        // Point from current position towards flock center
        preyVector -= this.transform.position;

        // Normalize
        return preyVector.normalized;
    }

    private Vector3 calculateTerminalHunt()
    {
        if (closestPrey == null) return Vector3.zero;

        Vector3 huntVector = closestPrey.transform.position - this.transform.position;

        //if (huntVector.magnitude < 0.5) Debug.Log("Hunt " + closestPrey.name + " : " + huntVector.magnitude);

        // Normalize
        return huntVector.normalized;
    }

    protected override void updateNeighbors()
    {
        preyNeighbors = new List<Boid>();
        closestPrey = null;

        float closestDistanceSqr = 0;

        foreach (Boid boid in flock.boids)
        {
            float distanceSqr = Vector3.SqrMagnitude(boid.transform.position - this.transform.position);

            if (distanceSqr < (flock.huntDistance * flock.huntDistance)) preyNeighbors.Add(boid);

            if (distanceSqr < flock.terminalHuntDistance * flock.terminalHuntDistance)
            {
                if (!closestPrey)
                {
                    closestPrey = boid;
                    closestDistanceSqr = distanceSqr;
                }
                else if (distanceSqr < closestDistanceSqr)
                {
                    closestPrey = boid;
                    closestDistanceSqr = distanceSqr;
                }
            }
        }
    }

}
