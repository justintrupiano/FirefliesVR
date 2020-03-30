using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;
using System.IO;

public class multipleAgents : MonoBehaviour
{
  static System.Random random = new System.Random();

  public GameObject agentPrefab;
  public GameObject[] agents;
  public int numAgents;
  public int enviromentSize;
  public bool randomLocation;

  public bool flashBool = true;
  public bool copyBool = true;
  public bool stealBool = true;
  public bool harvestBool = true;
  public bool moveBool = true;
  public float speed = 50.0f;
  public float increaseAgeRate;
  public int senseDistance;



  [SerializeField]
  private int currentlyFlashing;

  [SerializeField]
  private List<int> flashingList = new List<int>();

  public List<List<int>> chosenIndices = new List<List<int>>();

  [Range(0.0f, 1.0f)]
  public float StartEnergy = 0.0f;

  [Range(0.0f, 1.0f)]
  public float harvestAmount = 0.1f;

  [Range(0.0f, 1.0f)]
  public float stealAmount = 0.1f;

  [Range(0.0f, 1.0f)]
  public float copyCost = 0.05f;

  [Range(0.0f, 1.0f)]
  public float flashCost = 0.1f;

  [Range(0.0f, 1.0f)]
  public float moveCost = 0.1f;

  [Range(0.0f, 0.01f)]
  public float noise = 0.005f;

  [Range(0.0f, 0.01f)]
  public float neighborFlashInfluence = 0.05f;

  [Range(0.0f, 0.01f)]
  public float flashPotentalIncrease = 0.05f;

  public float count;

  public bool InvokeRepeatingBool = false;
  public bool recording = true;

  void Start()
  {
    //// Fill gameobject 'agents' with whatever prefab is definded above.
    agents = new GameObject[numAgents];

    for (int i = 0; i < numAgents; i++)
    {
      agents[i] = Instantiate(agentPrefab);
      agents[i].transform.parent = gameObject.transform;
      agents[i].name = "agent_" + i.ToString("D6");
    }

    // List<int> thisList = new List<int>();
    // thisList.Add(0);
    // thisList.Add(1);
    // thisList.Add(2);
    // thisList.Add(3);
    // thisList.Add(4);
    //
    // chosenIndices.Add(thisList);


  }

  void Update()
  {

    if (InvokeRepeatingBool)
    {
      InvokeRepeating("Save", 0.0f, 0.1f);
      InvokeRepeatingBool = false;
    }

    if (count >= 0.0f && recording)
    {
      count -= Time.deltaTime;
    }

    else{
      CancelInvoke();
      recording = false;
    }


    currentlyFlashing = 0;

    foreach (GameObject agent in agents)
    {
      FireflyScript agentScript = agent.GetComponent<FireflyScript>();
      if (agentScript.PS.IsAlive())
        currentlyFlashing += 1;

      // thisLoop[agentScript.chosenIndex] += 1;
    }
    // flashingList.Add(currentlyFlashing);
    // chosenIndices.Add(thisLoop);
    // Debug.Log(thisLoop[0]);
    // Debug.Log(thisLoop[1]);
    // Debug.Log(thisLoop[2]);
    // Debug.Log(thisLoop[3]);
    // Debug.Log(thisLoop[4]);

  }

  public string fileName;
  void Save(){
    string path = Application.dataPath + fileName;
    // string path = Application.dataPath + DateTime.Now;

    if (!File.Exists(path))
      File.WriteAllText(path, "Num Flashing Every Second \n\n");

    string content = currentlyFlashing.ToString() + "\n";
    File.AppendAllText(path, content);

  }

  // System.IO.File.WriteAllLines("SavedLists.txt", Lists.verbList);


}
