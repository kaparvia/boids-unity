using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;

public class Flock : MonoBehaviour
{
    private const float COLOR_VARIATION = 0.15f;

    [Header("Flock Setup")]

    [SerializeField] public bool useJobs;
    [SerializeField] private Boid boidPrefab;
    [SerializeField] private Boid predatorPrefab;

    [Range(2f, 20f)]
    [SerializeField] public float domainRadius;

    [Range(1, 10000)]
    [SerializeField] private int numberOfBoids = 100;

    [Range(0.1f, 10f)]
    [SerializeField] public float minSpeed;

    [Range(0.1f, 20f)]
    [SerializeField] public float maxSpeed;

    [Header("Predator Parameters")]

    [Range(0, 10)]
    [SerializeField] private int numberOfPredators = 0;

    [Range(0.1f, 10f)]
    [SerializeField] public float minSpeedPredator;

    [Range(0.1f, 20f)]
    [SerializeField] public float maxSpeedPredator;

    [Range(0.1f, 20f)]
    [SerializeField] public float huntDistance;

    [Range(0.1f, 20f)]
    [SerializeField] public float huntWeight;

    [Range(0.1f, 20f)]
    [SerializeField] public float terminalHuntDistance;

    [Range(0.1f, 20f)]
    [SerializeField] public float terminalHuntWeight;

    [Header("Detection Distances")]
    [Range(0.1f, 20f)]
    [SerializeField] public float cohesionDistance;

    [Range(0.1f, 2f)]
    [SerializeField] public float avoidanceDistance;

    [Range(0.1f, 20f)]
    [SerializeField] public float alignmentDistance;

    [Range(0.1f, 20f)]
    [SerializeField] public float predatorAvoidanceDistance;


    [Header("Behaviour Weights")]
    [Range(0f, 10f)]
    [SerializeField] public float cohesionWeight;

    [Range(0f, 10f)]
    [SerializeField] public float avoidanceWeight;

    [Range(0f, 10f)]
    [SerializeField] public float alignmentWeight;

    [Range(0f, 10f)]
    [SerializeField] public float boundsWeight;

    [Range(0f, 10f)]
    [SerializeField] public float predatorAvoidanceWeight;

    public List<Boid> boids;
    public List<Predator> predators;

    private List<Material> materials = new();

    void Start()
    {
        boids = new List<Boid>();
        predators = new List<Predator>();

        generateColors();

        // Initialize the flock
        for (int i = 0; i < numberOfBoids; i++)
        {
            Boid newBoid = createBoid("Boid " + i, false);
            boids.Add(newBoid);
        }

        for (int i = 0; i < numberOfPredators; i++)
        {
            Predator newBoid = (Predator)createBoid("Predator " + i, true);
            predators.Add(newBoid);
        }
    }

    /*
     * Calculate new speed and direction for all boids but do not apply it yet. 
     * This way all results are based on the positions in the last frame.
     */
    void Update()
    {
        if (useJobs)
        {
            updateWithJobs();
        } 
        else
        {
            foreach (Boid boid in boids)
            {
                boid.CalculateMove();
            }
        }

    }

    /*
     * Apply updated speed and direction
     */
    void LateUpdate()
    {
        foreach (Boid boid in boids)
        {
            boid.Move();
        }

        //foreach (Boid boid in predatorBoids)
        //{
        //    boid.Move();
        //}
    }

    private Boid createBoid(string name, bool isPredator)
    {
        Boid newBoid = Instantiate(
            isPredator ? predatorPrefab : boidPrefab,
            UnityEngine.Random.insideUnitSphere * domainRadius,
            Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360)),
            transform
        );

        newBoid.name = name;
        newBoid.flock = this;

        if (isPredator)
        {
            newBoid.speed = UnityEngine.Random.Range(minSpeedPredator, maxSpeedPredator);
        }
        else
        {
            newBoid.speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        }

        // Randomize material
        Material material = materials[UnityEngine.Random.Range(0, materials.Count - 1)];
        MeshRenderer renderer = newBoid.gameObject.GetComponentInChildren<MeshRenderer>();
        renderer.material = material;

        return newBoid;
    }

    private void generateColors()
    {
        for (int i = 0; i < 5; i++)
        {
            float H = 186 / 360f + UnityEngine.Random.Range(-COLOR_VARIATION / 2, COLOR_VARIATION / 2);
            float S = 43 / 100f;
            float V = 81 / 100f + UnityEngine.Random.Range(-COLOR_VARIATION / 2, COLOR_VARIATION / 2);

            Material material = new Material(Shader.Find("Standard"));
            material.SetColor("_Color", Color.HSVToRGB(H, S, V));
            material.enableInstancing = true;
            materials.Add(material);
        }
    }


    private void updateWithJobs()
    {
        NativeArray<Vector3> boidPositionArray = new NativeArray<Vector3>(boids.Count, Allocator.TempJob);
        NativeArray<Vector3> boidDirectionArray = new NativeArray<Vector3>(boids.Count, Allocator.TempJob);
        NativeArray<float> boidSpeedArray = new NativeArray<float>(boids.Count, Allocator.TempJob);

        NativeArray<Vector3> resultDirections = new NativeArray<Vector3>(boids.Count, Allocator.TempJob);
        NativeArray<float> resultSpeeds = new NativeArray<float>(boids.Count, Allocator.TempJob);

        // Copy boid information to the native arrays
        for (int i = 0; i < boids.Count; i++)
        {
            boidPositionArray[i] = boids[i].transform.position;
            boidDirectionArray[i] = boids[i].transform.forward;
            boidSpeedArray[i] = boids[i].speed;
        }

        // Initialize job
        CalculateBoidMoveJob job = new CalculateBoidMoveJob
        {
            boidPositionArray = boidPositionArray,
            boidDirectionArray = boidDirectionArray,
            boidSpeedArray = boidSpeedArray,

            cohesionDistanceSqr = cohesionDistance * cohesionDistance,
            avoidanceDistanceSqr = avoidanceDistance * avoidanceDistance,
            alignmentDistanceSqr = alignmentDistance * alignmentDistance,

            cohesionWeight = cohesionWeight,
            avoidanceWeight = avoidanceWeight,
            alignmentWeight = alignmentWeight,
            boundsWeight = boundsWeight,
            domainRadius = domainRadius,
            domainCenter = this.transform.position,

            // Results
            resultDirections = resultDirections,
            resultSpeeds = resultSpeeds,
        };

        JobHandle handle = job.Schedule(boids.Count, 20);
        handle.Complete();

        for (int i = 0; i < boids.Count; i++)
        {
            boids[i].newDirection = resultDirections[i];
            boids[i].speed = Mathf.Clamp(resultSpeeds[i], minSpeed, maxSpeed);
        }

        boidPositionArray.Dispose();
        boidDirectionArray.Dispose();
        boidSpeedArray.Dispose();
        resultDirections.Dispose();
        resultSpeeds.Dispose();
    }
}
