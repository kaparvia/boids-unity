using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;

public class Flock : MonoBehaviour
{
    private const float COLOR_VARIATION = 0.15f;

    private List<Material> materials = new();
    private UIManager uiManager;
    public Camera camera3D;

    [Header("Flock Setup")]

    [SerializeField] private Boid boidPrefab;
    [SerializeField] private Boid predatorPrefab;

    [Range(2f, 20f)]
    [SerializeField] public float domainRadius;

    [Range(1, 10000)]
    [SerializeField] private int numberOfBoids;

    [Range(0.1f, 10f)]
    [SerializeField] public float minSpeed;

    [Range(0.1f, 20f)]
    [SerializeField] public float maxSpeed;

    [Range(0f, 10f)]
    [SerializeField] public float viewRotationSpeed;

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

    [Range(0.05f, 1f)]
    [SerializeField] public float killDistance;


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

    public List<Predator> predators;
    public List<Boid> boids;

    private int predatorNameCounter;

    /*************************************************************************
     * UNITY METHODS
     ************************************************************************/
    void Start()
    {
        uiManager = GameObject.Find("Canvas").GetComponent<UIManager>();

        boids = new List<Boid>();
        predators = new List<Predator>();

        generateColors();

        // Initialize the flock
        for (int i = 0; i < numberOfBoids; i++)
        {
            Boid newBoid = createBoid("Boid " + i, false);
            boids.Add(newBoid);
        }

        predatorNameCounter = 1;
        for (int i = 0; i < numberOfPredators; i++)
        {
            Predator newBoid = (Predator)createBoid("Predator " + (predatorNameCounter++), true);
            predators.Add(newBoid);
        }

        // Update UI
        uiManager.UpdateBoidCount();
        uiManager.UpdatePredatorCount();
    }

    /*
     * Calculate new speed and direction for all boids but do not apply it yet. 
     * This way all results are based on the positions in the last frame.
     */
    void Update()
    {
        // Predators don't use Jobs system, there's only few of them
        foreach (Predator predator in predators)
        {
            predator.CalculateMove();
        }

        calculateBoidMoves();

        if (viewRotationSpeed > 0)
        {
            Vector3 target = Vector3.zero;
            camera3D.transform.LookAt(target);
            camera3D.transform.RotateAround(target, Vector3.up, viewRotationSpeed * Time.deltaTime);
            camera3D.transform.RotateAround(target, Vector3.right, viewRotationSpeed * Time.deltaTime);
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

        foreach (Boid predator in predators)
        {
            predator.Move();
        }
    }

    public void Kill(Boid boid)
    {
        boids.Remove(boid);
        Destroy(boid.gameObject);

        uiManager.UpdateBoidCount();
    }

    /*************************************************************************
     * CREATE BOIDS
     ************************************************************************/
    private Boid createBoid(string name, bool isPredator, bool isAtEdge = false)
    {
        Boid newBoid = Instantiate(
            isPredator ? predatorPrefab : boidPrefab,
            isAtEdge ? UnityEngine.Random.onUnitSphere * domainRadius  : UnityEngine.Random.insideUnitSphere * domainRadius,
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

        // Randomize material for boids
        if (!isPredator)
        {
            Material material = materials[UnityEngine.Random.Range(0, materials.Count - 1)];
            MeshRenderer renderer = newBoid.gameObject.GetComponentInChildren<MeshRenderer>();
            renderer.material = material;
        }

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

    /*************************************************************************
     * UPDATE BOIDS
     ************************************************************************/
    public void AddPredator()
    {
        Predator newPredator = (Predator)createBoid("Predator " + (predatorNameCounter++), true, true);
        predators.Add(newPredator);
        uiManager.UpdatePredatorCount();
    }

    public void RemovePredator()
    {
        if (predators.Count == 0) return;

        Predator predator = predators[0];
        predators.RemoveAt(0);

        Destroy(predator.gameObject);
        uiManager.UpdatePredatorCount();
    }

    private void calculateBoidMoves()
    {
        NativeArray<Vector3> boidPositionArray = new NativeArray<Vector3>(boids.Count, Allocator.TempJob);
        NativeArray<Vector3> boidDirectionArray = new NativeArray<Vector3>(boids.Count, Allocator.TempJob);
        NativeArray<float> boidSpeedArray = new NativeArray<float>(boids.Count, Allocator.TempJob);
        NativeArray<Vector3> predatorPositionArray = new NativeArray<Vector3>(predators.Count, Allocator.TempJob);

        NativeArray<Vector3> resultDirections = new NativeArray<Vector3>(boids.Count, Allocator.TempJob);
        NativeArray<float> resultSpeeds = new NativeArray<float>(boids.Count, Allocator.TempJob);

        // Copy boid information to the native arrays
        for (int i = 0; i < boids.Count; i++)
        {
            boidPositionArray[i] = boids[i].transform.position;
            boidDirectionArray[i] = boids[i].transform.forward;
            boidSpeedArray[i] = boids[i].speed;
        }

        for (int i = 0; i < predators.Count; i++)
        {
            predatorPositionArray[i] = predators[i].transform.position;
        }

        // Initialize job
        BoidMoveJob job = new BoidMoveJob
        {
            boidPositionArray = boidPositionArray,
            boidDirectionArray = boidDirectionArray,
            boidSpeedArray = boidSpeedArray,
            predatorPositionArray = predatorPositionArray,

            cohesionDistanceSqr = cohesionDistance * cohesionDistance,
            avoidanceDistanceSqr = avoidanceDistance * avoidanceDistance,
            alignmentDistanceSqr = alignmentDistance * alignmentDistance,
            predatorAvoidanceDistanceSqr = predatorAvoidanceDistance * predatorAvoidanceDistance,

            cohesionWeight = cohesionWeight,
            avoidanceWeight = avoidanceWeight,
            alignmentWeight = alignmentWeight,
            boundsWeight = boundsWeight,
            predatorAvoidanceWeight = predatorAvoidanceWeight,

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
        predatorPositionArray.Dispose();
        resultDirections.Dispose();
        resultSpeeds.Dispose();
    }
}
