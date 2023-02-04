using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
public struct BoidMoveJob : IJobParallelFor
{
    private const int MAX_NEIGHBORS = 50;

    // Boid information from the last frame
    [ReadOnly] public NativeArray<Vector3> boidPositionArray;
    [ReadOnly] public NativeArray<Vector3> boidDirectionArray;
    [ReadOnly] public NativeArray<float> boidSpeedArray;
    [ReadOnly] public NativeArray<Vector3> predatorPositionArray;

    // Boid algorithm parameters
    [ReadOnly] public float cohesionDistanceSqr;
    [ReadOnly] public float avoidanceDistanceSqr;
    [ReadOnly] public float alignmentDistanceSqr;
    [ReadOnly] public float predatorAvoidanceDistanceSqr;

    [ReadOnly] public float cohesionWeight;
    [ReadOnly] public float avoidanceWeight;
    [ReadOnly] public float alignmentWeight;
    [ReadOnly] public float boundsWeight;
    [ReadOnly] public float predatorAvoidanceWeight;

    // Flock geometry
    [ReadOnly] public float domainRadius;
    [ReadOnly] public Vector3 domainCenter;

    // Results
    public NativeArray<Vector3> resultDirections;
    public NativeArray<float> resultSpeeds;

    public void Execute(int currentBoidIndex)
    {
        var neighbors = findNeighbors(currentBoidIndex);
        var predators = findPredators(currentBoidIndex);

        Vector3 cohesionVector = calculateCohesion(currentBoidIndex, neighbors.cohesion) * cohesionWeight;
        Vector3 avoidanceVector = calculateAvoidance(currentBoidIndex, neighbors.avoidance) * avoidanceWeight;
        Vector3 alignmentVector = calculateAlignment(currentBoidIndex, neighbors.alignment) * alignmentWeight;
        Vector3 fleeVector = calculatePredatorAvoidance(currentBoidIndex, predators) * predatorAvoidanceWeight;
        Vector3 boundsVector = calculateBounds(currentBoidIndex) * boundsWeight;

        float newSpeed = calculateSpeedForBoid(currentBoidIndex, neighbors.cohesion);

        resultDirections[currentBoidIndex] = cohesionVector + avoidanceVector + alignmentVector + boundsVector + fleeVector;
        resultSpeeds[currentBoidIndex] = newSpeed;
    }

    private float calculateSpeedForBoid(int currentBoidIndex, NativeList<int> neighborIndexes)
    {
        if (neighborIndexes.Length == 0) return boidSpeedArray[currentBoidIndex];

        // Average speed over neighbors
        float newSpeed = 0;

        foreach (int i in neighborIndexes)
        {
            newSpeed += boidSpeedArray[i];
        }

        return newSpeed / neighborIndexes.Length;
    }

    private Vector3 calculateCohesion(int currentBoidIndex, NativeList<int> neighborIndexes)
    {
        if (neighborIndexes.Length == 0) return Vector3.zero;

        Vector3 cohesionVector = Vector3.zero;

        // Sum up positions of all neighbors to find the average center of the surrounding flock
        foreach (int index in neighborIndexes)
        {
            cohesionVector += boidPositionArray[index];
        }

        // Average
        cohesionVector /= neighborIndexes.Length;

        // Point from current position towards flock center
        cohesionVector -= boidPositionArray[currentBoidIndex];

        // Normalize
        return cohesionVector.normalized;
    }

    private Vector3 calculateAvoidance(int currentBoidIndex, NativeList<int> neighborIndexes)
    {
        if (neighborIndexes.Length == 0) return Vector3.zero;

        Vector3 avoidanceVector = Vector3.zero;

        // Sum up vectors pointing from boid to neighbors
        foreach (int index in neighborIndexes)
        {
            avoidanceVector += boidPositionArray[currentBoidIndex] - boidPositionArray[index];
        }

        // Average
        avoidanceVector /= neighborIndexes.Length;

        // Normalize
        return avoidanceVector.normalized;
    }

    private Vector3 calculateAlignment(int currentBoidIndex, NativeList<int> neighborIndexes)
    {
        if (neighborIndexes.Length == 0) return boidDirectionArray[currentBoidIndex].normalized;

        // By default keep pointing in the same direction
        Vector3 alignmentVector = boidDirectionArray[currentBoidIndex].normalized;

        // Sum up directions all surrounding boids are pointing
        foreach (int index in neighborIndexes)
        {
            alignmentVector += boidDirectionArray[index].normalized;
        }

        // Average
        alignmentVector /= neighborIndexes.Length;

        // Normalize
        return alignmentVector.normalized;
    }


    private Vector3 calculateBounds(int currentBoidIndex)
    {
        Vector3 boundsVector = Vector3.zero;
        Vector3 fromCenter = domainCenter - boidPositionArray[currentBoidIndex];

        if (fromCenter.magnitude > (domainRadius * 0.9f))
        {
            boundsVector = fromCenter.normalized;
        }

        return boundsVector;
    }

    private Vector3 calculatePredatorAvoidance(int currentBoidIndex, NativeList<int> predatorIndexes)
    {
        if (predatorIndexes.Length == 0) return Vector3.zero;

        Vector3 avoidanceVector = Vector3.zero;

        // Sum up vectors pointing from boid to neighbors
        foreach (int predatorIndex in predatorIndexes)
        {
            avoidanceVector += boidPositionArray[currentBoidIndex] - boidPositionArray[predatorIndex];
        }

        // Average
        avoidanceVector /= predatorIndexes.Length;

        // Normalize
        return avoidanceVector.normalized;
    }
    private (NativeList<int> cohesion, NativeList<int> avoidance, NativeList<int> alignment) findNeighbors(int boidIndex)
    {
        NativeList<int> cohesionNeighbors = new NativeList<int>(MAX_NEIGHBORS, Allocator.Temp);
        NativeList<int> avoidanceNeighbors = new NativeList<int>(MAX_NEIGHBORS, Allocator.Temp);
        NativeList<int> alignmentNeighbors = new NativeList<int>(MAX_NEIGHBORS, Allocator.Temp);

        for (int i = 0; i < boidPositionArray.Length; i++)
        {
            // Don't compare to self
            if (i == boidIndex) continue;

            float distanceSqr = Vector3.SqrMagnitude(boidPositionArray[i] - boidPositionArray[boidIndex]);

            if (distanceSqr < cohesionDistanceSqr  && cohesionNeighbors.Length  <= MAX_NEIGHBORS) cohesionNeighbors.Add(i);
            if (distanceSqr < avoidanceDistanceSqr && avoidanceNeighbors.Length <= MAX_NEIGHBORS) avoidanceNeighbors.Add(i);
            if (distanceSqr < alignmentDistanceSqr && alignmentNeighbors.Length <= MAX_NEIGHBORS) alignmentNeighbors.Add(i);
        }

        return (cohesionNeighbors, avoidanceNeighbors, alignmentNeighbors);
    }

    private NativeList<int> findPredators(int boidIndex)
    {
        NativeList<int> predators = new NativeList<int>(predatorPositionArray.Length, Allocator.Temp);

        for (int i = 0; i < predatorPositionArray.Length; i++)
        {
            float distanceSqr = Vector3.SqrMagnitude(predatorPositionArray[i] - boidPositionArray[boidIndex]);

            if (distanceSqr < predatorAvoidanceDistanceSqr) predators.Add(i);
        }

        return predators;
    }

}