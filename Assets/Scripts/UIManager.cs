using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_Text boidCountField;
    public TMP_Text predatorCountField;
    public Button addPredatorButton;
    public Button removePredatorButton;

    private int boidCount;
    private int predatorCount;

    private Flock flock;

    void Start()
    {
        flock = GameObject.Find("Flock").GetComponent<Flock>();

        addPredatorButton.onClick.AddListener(AddPredatorClick);
        removePredatorButton.onClick.AddListener(RemovePredatorClick);
    }

    void Update()
    {
        boidCountField.text = "Boids: " + boidCount.ToString();
        predatorCountField.text = "Hunters: " + predatorCount.ToString();
    }

    public void UpdateBoidCount()
    {
        boidCount = flock.boids.Count; ;
    }

    public void UpdatePredatorCount()
    {
        predatorCount = flock.predators.Count;
    }

    void AddPredatorClick()
    {
        flock.AddPredator();
    }

    void RemovePredatorClick()
    {
        flock.RemovePredator();
    }

}
