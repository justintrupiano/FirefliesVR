using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuralNetwork;
using System.Linq;
// using System;

public class FireflyScript : MonoBehaviour
{

  public ParticleSystem PS;

  public float timeSinceLastFlash;
  private float varienceInGroupFlashes;
  public float varienceFromMean;
  public float meanFlashTime;
  public float age;

  private multipleAgents multipleAgents;

  private int totalNumNeighbors;

  public NeuralNet  net;

  private double[]  inputData;
  private double[]  hiddenData;
  public double[]   outputData;

  public int        inputSize;
  public int        numHiddenLayers;
  public int        hiddenSize;
  public int        outputSize;

  public int        chosenIndex;
  public float      energy;
  public float      speed;

  private bool      flashBool;
  private bool      copyBool;
  private bool      stealBool;
  private bool      harvestBool;
  private bool      moveBool;
  private float     neighborFlashInfluence;
  private float     flashPotentalIncrease;

  public bool       drawSenseSphere = false;

  private float noise;
  private Vector3 dir;

  System.Random random = new System.Random();

  private Collider[] neighbors;
  public int mostFitNeighbor;
  public int numNeighborsFlashing;
  List<int> flashingAgents = new List<int>();
  public float flashPotental = 0.0f;
  // public GameObject sphere;

  void Start()
  {
    multipleAgents = gameObject.GetComponentInParent(typeof(multipleAgents)) as multipleAgents;

    flashBool =   multipleAgents.flashBool;
    copyBool =    multipleAgents.copyBool;
    stealBool =   multipleAgents.stealBool;
    harvestBool = multipleAgents.harvestBool;
    moveBool =    multipleAgents.moveBool;

    neighborFlashInfluence = multipleAgents.neighborFlashInfluence;
    flashPotentalIncrease = multipleAgents.flashPotentalIncrease;


    if(multipleAgents.randomLocation)
    {
      transform.localPosition = new Vector3( Random.Range(-multipleAgents.enviromentSize, multipleAgents.enviromentSize),
                                                Random.Range(-multipleAgents.enviromentSize/2, multipleAgents.enviromentSize/2),
                                                Random.Range(-multipleAgents.enviromentSize, multipleAgents.enviromentSize));
    }

    else
    {
      transform.localPosition = new Vector3( 0.0f, 0.0f, 0.0f);
    }

    noise = multipleAgents.noise;
    speed = multipleAgents.speed;
    // speed = 100.0f;
    energy = multipleAgents.StartEnergy;

    inputData = new double[inputSize];
    outputData = new double[outputSize];

    net = new NeuralNet(inputSize, numHiddenLayers, hiddenSize, outputSize);
    dir = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));


    populateNeighborArray();

  }

  void OnDrawGizmos()
  {
    if (drawSenseSphere)
    {
      Gizmos.color = new Color(255, 0, 0, 0.25f);
      Gizmos.DrawSphere(transform.position, multipleAgents.senseDistance);
    }

  }

  void Update(){

    increaseAge();
    timeSinceLastFlash += Time.deltaTime;

    // RaycastHit hit;
    //
    // if (Physics.Raycast(transform.position, Camera.main.transform.position - transform.position, out hit))
    // {
    //
    //
    //   Debug.DrawLine(transform.position, hit.point, Color.red);
    //
    //   if  (hit.transform.tag == "MainCamera")
    //   {
    //     transform.GetChild(1).gameObject.SetActive(true);
    //   }
    //   else
    //   {
    //     transform.GetChild(1).gameObject.SetActive(false);
    //   }
    // }
    //
    // float distToCamera = Vector3.Distance(Camera.main.transform.position, transform.position);
    // if (distToCamera > 50)
    // {
    //   transform.position = Camera.main.transform.position;
    // }



    flashingAgents = new List<int>();
    // countOthersFlashing();

    getFlashTimeMean();
    getVarience();

    inputData[0] = varienceFromMean;
    inputData[1] = meanFlashTime;

    // inputData[] = timeSinceLastFlash;
    // inputData[] = energy;
    // inputData[] = (numNeighborsFlashing > 0 && totalNumNeighbors > 0) ? (double)(numNeighborsFlashing/totalNumNeighbors) : 0;

    outputData = net.Compute(inputData);

    double totalOfAllOutputs = 0;
    chosenIndex = 0;

    foreach (double output in outputData)
    {
      // Debug.Log(output.ToString());
      totalOfAllOutputs += output;
    }


    double choice = (double)Random.Range(0.0f, (float)totalOfAllOutputs);
    double count = 0;

    for (int i = 0; i < outputSize; i++)
      {
        count += outputData[i];

        if (count >= choice)
        {
              chosenIndex = i;
              break;
        }
      }



    populateNeighborArray();



    switch (chosenIndex)
    {
      case 0:
        // if (flashBool && energy >= multipleAgents.flashCost && !PS.IsAlive())
        if (flashBool)
          {
            // Flash();
            flashPotental += flashPotentalIncrease;
            // timeSinceLastFlash = 0.0f;
            // spendEnergy(multipleAgents.flashCost);
            // Debug.Log("0: Flash");
          }

          if (flashPotental >= 1.0f && energy >= multipleAgents.flashCost && !PS.IsAlive())
          {
            Flash();
            flashPotental = 0.0f;
            timeSinceLastFlash = 0.0f;
            spendEnergy(multipleAgents.flashCost);

            foreach (Collider neighbor in neighbors)
              neighbor.GetComponent<FireflyScript>().flashPotental += flashPotentalIncrease;
          }
        break;

      case 1:
        if (copyBool && numNeighborsFlashing > 0 && energy >= multipleAgents.copyCost)
          {
            // int agent = flashingAgents[(int)Random.Range(0, flashingAgents.Count)];
            findMostFit();
            FireflyScript otherAgentScript = multipleAgents.agents[mostFitNeighbor].GetComponent<FireflyScript>();

            //// Both agents are replaced:
            NNCopy(net, otherAgentScript.net);
            age = 0.0f;
            otherAgentScript.age = 0.0f;
            // otherAgentScript.NNCopy(otherAgentScript.net, net);

            spendEnergy(multipleAgents.copyCost);
            // Debug.Log("1: Copy");
          }
        break;

      case 2:
        if (stealBool && numNeighborsFlashing > 0)
          {
            int agent = flashingAgents[(int)Random.Range(0, numNeighborsFlashing)];
            // FireflyScript otherAgentScript = multipleAgents.agents[agent].GetComponent<FireflyScript>();

            stealEnergy(agent, multipleAgents.stealAmount);

            //// Max energy at 1.0f
            energy = (energy > 1.0f) ? 1.0f : energy;
            // Debug.Log("2: Steal Energy");
          }
        break;

      case 3:
        if (harvestBool && energy < 1.0f)
          {
            harvestEnergy(multipleAgents.harvestAmount);
            // Debug.Log("3: Harvest Energy");
          }
        break;

      case 4:
      if (moveBool && energy >= multipleAgents.moveCost)
      {
        // Debug.Log("4: Move");
        move();
        spendEnergy(multipleAgents.moveCost);
      }
      break;

    }



    //// If flashing and > threshold of neighbors are flashing...
    //// ...add to replicateList

    // if (PS.IsAlive() && Random.Range(0.0f, 1.0f) < inputData[0])
    // {
    //   // Debug.Log(System.Array.IndexOf(multipleAgents.agents, gameObject));
    //   int thisIndex = System.Array.IndexOf(multipleAgents.agents, gameObject);
    //   multipleAgents.replicateList.Add(thisIndex);
    // }
    // if (!PS.IsAlive() && inputData[0] > multipleAgents.neighborThreshold)
    // {
    //   // Debug.Log(System.Array.IndexOf(multipleAgents.agents, gameObject));
    //   int thisIndex = System.Array.IndexOf(multipleAgents.agents, gameObject);
    //   multipleAgents.removeList.Add(thisIndex);
    // }
    // if (energy > 1.0f)
    // {
    //   Flash();
    //   spendEnergy();
    //
    // }

  }


  private void populateNeighborArray()
  {
    int layer = 1 << 9;
    neighbors = Physics.OverlapSphere(transform.position, multipleAgents.senseDistance, layer);
  }

  private void Flash()
  {

      PS.Emit(1);

      // RaycastHit hit;
      // // Vector3 ray = new Vector3();
      // // Debug.DrawLine(transform.position, Camera.main.transform.position, Color.red);
      //
      // if (Physics.Raycast(transform.position, Camera.main.transform.position - transform.position, out hit))
      // {
      //   // dir += hit.normal * 10;
      //
      //   if  (hit.transform.tag == "MainCamera")
      //   {
      //     // Debug.DrawLine(transform.position, hit.point, Color.red);
      //     Debug.DrawLine(transform.position, hit.transform.position, Color.red);
      //     PS.Emit(1);
      //   }
      //
      //
      // }
  }


  private void harvestEnergy(float harvestAmount)
  {
    // float dist = Vector3.Distance(new Vector3(0,0,0), transform.position);
    // energy += 1.0f-(dist/(multipleAgents.enviromentSize*2));
    energy += harvestAmount;
    // Debug.Log(energy);
  }


  private void spendEnergy(float costAmount)
  {
    energy -= costAmount;
  }


  private void stealEnergy(int otherAgent, float stealAmount)
  {
     float agentEnergy = multipleAgents.agents[otherAgent].GetComponent<FireflyScript>().energy;
     if (agentEnergy >= stealAmount)
     {
       agentEnergy -= stealAmount;
       energy += stealAmount;
     }

  }


  private void countOthersFlashing(){
    int numNeighborsFlashing = 0;
    int layer = 1 << 9;

    Collider[] hitColliders = Physics.OverlapSphere(transform.position, multipleAgents.senseDistance, layer);



    foreach (Collider c in hitColliders)
    {
      if (c.transform.gameObject != gameObject)
      {
        if (c.GetComponent<FireflyScript>().PS.IsAlive())
        {

          flashingAgents.Add(multipleAgents.agents.ToList().IndexOf(c.transform.gameObject));
          numNeighborsFlashing+=1;
        }
      }
    }
  }





  private void getFlashTimeMean(){
    float totalNeighborFlashTime = 0.0f;
    // float currentLowest = -1.0f;

    numNeighborsFlashing = 0;
    totalNumNeighbors = 0;

    foreach (Collider neighbor in neighbors)
    {
      if (neighbor.transform.gameObject != gameObject)
      {
        FireflyScript neighborScript = neighbor.GetComponent<FireflyScript>();
        totalNeighborFlashTime += neighborScript.timeSinceLastFlash;
        totalNumNeighbors += 1;
        //// Find neighbor with lowest varienceFromMean
        // if (currentLowest == -1.0f || neighborScript.varienceFromMean < currentLowest)
        // {
        //   currentLowest = neighborScript.varienceFromMean;
        //   mostFitNeighbor = multipleAgents.agents.ToList().IndexOf(neighbor.transform.gameObject);
        // }

        if (neighborScript.PS.IsAlive())
        {
          numNeighborsFlashing+=1;
          // flashPotental += neighborFlashInfluence;
          flashingAgents.Add(multipleAgents.agents.ToList().IndexOf(neighbor.transform.gameObject));
        }
      }
    }
    meanFlashTime = (totalNumNeighbors > 0) ? totalNeighborFlashTime / totalNumNeighbors : 0;
    // varienceFromMean = (totalNumNeighbors > 0) ? Mathf.Abs(meanFlashTime - timeSinceLastFlash) : 0;
  }


    private void getVarience(){
      float sumOfSquaredMeans = 0.0f;

      foreach (Collider neighbor in neighbors)
      {
        if (neighbor.transform.gameObject != gameObject)
        {
          FireflyScript neighborScript = neighbor.GetComponent<FireflyScript>();
          float thisDiff = neighborScript.timeSinceLastFlash - neighborScript.meanFlashTime;
          sumOfSquaredMeans += Mathf.Pow(thisDiff, 2);
        }
      }

      varienceFromMean = (totalNumNeighbors > 0) ? sumOfSquaredMeans / totalNumNeighbors : 0;
    }


    private void findMostFit()
    {
      //// Find neighbor with lowest varienceFromMean

      float currentLowest = -1.0f;

      foreach (Collider neighbor in neighbors)
      {
        if (neighbor.transform.gameObject != gameObject)
          {
          FireflyScript neighborScript = neighbor.GetComponent<FireflyScript>();
          if (currentLowest == -1.0f || neighborScript.varienceFromMean < currentLowest)
          {
            currentLowest = neighborScript.varienceFromMean;
            mostFitNeighbor = multipleAgents.agents.ToList().IndexOf(neighbor.transform.gameObject);
          }
        }
      }
    }


void increaseAge()
{
  age += multipleAgents.increaseAgeRate;

  if (age >= 1.0f)
  {
    RandomizeWeightsAndBias(net);
    age = 0.0f;
  }

}


void move()
{
  RaycastHit hit;

  dir += new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
  // dir += (transform.TransformDirection(Vector3.forward) - transform.position).normalized;

  Vector3[] rays = new Vector3[5];

  for (int i = 0; i < rays.Length; i++)
  {
    rays[i] = transform.position;
  }

  rays[1].x += 10;
  rays[2].x -= 10;
  rays[3].y += 10;
  rays[4].y -= 10;

  foreach (Vector3 ray in rays)
  {
    int dist = 100;
    int layer = 1 << 8; //// Bitshift to Boundries Layer

    if (Physics.Raycast(ray, transform.TransformDirection(Vector3.forward), out hit, dist))
    {

      // if (hit.transform.gameObject.layer == layer)
      // {
        dir += hit.normal * 10;
        // Debug.DrawLine(ray, hit.point, Color.red);
      // }


    }
  }

  Quaternion rotation = Quaternion.LookRotation(dir);
  transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime);
  transform.position += transform.forward * speed * Time.deltaTime;

}

void PrintWeightsAndBias(NeuralNet NN){

  //// Input Layer
  foreach (Neuron neuron in NN.InputLayer){
    Debug.Log(neuron.Bias);
    foreach(Synapse synapse in neuron.InputSynapses){
      Debug.Log(synapse.Weight);
    }
    foreach(Synapse synapse in neuron.OutputSynapses){
      Debug.Log(synapse.Weight);
    }
  }

  //// Hidden Layers
  foreach(List<Neuron> hiddenLayer in NN.HiddenLayers){
    foreach (Neuron neuron in hiddenLayer){
      Debug.Log(neuron.Bias);
      foreach(Synapse synapse in neuron.InputSynapses){
        Debug.Log(synapse.Weight);
      }
      foreach(Synapse synapse in neuron.OutputSynapses){
        Debug.Log(synapse.Weight);
      }
    }
  }

  //// Output Layer
  foreach (Neuron neuron in NN.OutputLayer){
    Debug.Log(neuron.Bias);
    foreach(Synapse synapse in neuron.InputSynapses){
      Debug.Log(synapse.Weight);
    }
    foreach(Synapse synapse in neuron.OutputSynapses){
      Debug.Log(synapse.Weight);
    }
  }
}



void RandomizeWeightsAndBias(NeuralNet NN){

  //// Input Layer
  foreach (Neuron neuron in NN.InputLayer){
    neuron.Bias = Random.Range(-1.0f, 1.0f);

    foreach(Synapse synapse in neuron.InputSynapses)
      synapse.Weight = Random.Range(-1.0f, 1.0f);

    foreach(Synapse synapse in neuron.OutputSynapses)
      synapse.Weight = Random.Range(-1.0f, 1.0f);

  }

  //// Hidden Layers
  foreach(List<Neuron> hiddenLayer in NN.HiddenLayers){
    foreach (Neuron neuron in hiddenLayer){
      neuron.Bias = Random.Range(-1.0f, 1.0f);
      foreach(Synapse synapse in neuron.InputSynapses){
        synapse.Weight = Random.Range(-1.0f, 1.0f);
      }
      foreach(Synapse synapse in neuron.OutputSynapses){
        synapse.Weight = Random.Range(-1.0f, 1.0f);
      }
    }
  }

  //// Output Layer
  foreach (Neuron neuron in NN.OutputLayer){
    neuron.Bias = Random.Range(-1.0f, 1.0f);
    foreach(Synapse synapse in neuron.InputSynapses){
      synapse.Weight = Random.Range(-1.0f, 1.0f);
    }
    foreach(Synapse synapse in neuron.OutputSynapses){
      synapse.Weight = Random.Range(-1.0f, 1.0f);
    }
  }
}


  public void DirectCopyNN(NeuralNet NN_A, NeuralNet NN_B)
  {
    // FireflyScript otherAgentScript = multipleAgents.agents[parentAgent].GetComponent<FireflyScript>();
    //// INPUT LAYERS ////
    for (int y = 0; y < hiddenSize; y++) /// NUMBER OF HIDDEN NODES
    {
      for (int z = 0; z < inputSize; z++) /// NUMBER OF INPUT NODES
      {
        NN_A.InputLayer[z].OutputSynapses[y].Weight = NN_B.InputLayer[z].OutputSynapses[y].Weight;
        NN_A.HiddenLayers[0][y].InputSynapses[z].Weight = NN_B.HiddenLayers[0][y].InputSynapses[z].Weight;
      }
    }
    ///// Biases
    for (int x = 0; x < inputSize; x++)
    {
      NN_A.InputLayer[x].Bias = NN_B.InputLayer[x].Bias;
    }


    //// HIDDEN LAYERS ////
    for (int x = 0; x < numHiddenLayers-1; x++)
    {  /// NUMBER OF HIDDEN LAYERS
      for (int y = 0; y < hiddenSize; y++)
      {       /// NUMBER OF HIDDEN NODES
        for (int z = 0; z < hiddenSize; z++){     /// NUMBER OF OUTPUT NODES
            NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight = NN_B.HiddenLayers[x][y].OutputSynapses[z].Weight;
            //// Copy output of one layer to inputs of next layer.
            NN_A.HiddenLayers[x+1][z].InputSynapses[y].Weight = NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight;
          }
        }
      }
    //// HIDDEN BIASES
    for (int x = 0; x < numHiddenLayers; x++) /// NUMBER OF HIDDEN LAYERS
      {
        for (int y = 0; y < numHiddenLayers; y++) /// NUMBER OF HIDDEN LAYERS
          {
            NN_A.HiddenLayers[x][y].Bias = NN_B.HiddenLayers[x][y].Bias;
          }
        }


    //// OUTPUT LAYERS
    for (int x = 0; x < hiddenSize; x++){
      for (int y = 0; y < hiddenSize; y++){   /// NUMBER OF HIDDEN NODES
        for (int z = 0; z < outputSize; z++){ /// NUMBER OF OUTPUT NODES
          NN_A.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight = NN_B.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight;
        }
      }
    }
    //// OUTPUT BIASES
    for (int x = 0; x < outputSize; x++){ /// NUMBER OF OUTPUT NODES
      NN_A.OutputLayer[x].Bias = NN_B.OutputLayer[x].Bias;
    }
  }


  public void NoisyCopyNN(NeuralNet NN_A, NeuralNet NN_B)
  {
    // FireflyScript otherAgentScript = multipleAgents.agents[parentAgent].GetComponent<FireflyScript>();

    //// INPUT LAYERS ////
    for (int y = 0; y < hiddenSize; y++) /// NUMBER OF HIDDEN NODES
    {
      for (int z = 0; z < inputSize; z++) /// NUMBER OF INPUT NODES
      {
        NN_A.InputLayer[z].OutputSynapses[y].Weight = NN_B.InputLayer[z].OutputSynapses[y].Weight;
        NN_A.HiddenLayers[0][y].InputSynapses[z].Weight = NN_B.HiddenLayers[0][y].InputSynapses[z].Weight;
        //// Noise
        NN_A.InputLayer[z].OutputSynapses[y].Weight += Random.Range(-noise, noise);
        NN_A.HiddenLayers[0][y].InputSynapses[z].Weight += Random.Range(-noise, noise);
      }
    }
    ///// Biases
    for (int x = 0; x < inputSize; x++)
    {
      NN_A.InputLayer[x].Bias = NN_B.InputLayer[x].Bias;
      //// Noise
      NN_A.InputLayer[x].Bias += Random.Range(-noise, noise);
    }


    //// HIDDEN LAYERS ////
    for (int x = 0; x < numHiddenLayers-1; x++)
    {  /// NUMBER OF HIDDEN LAYERS
      for (int y = 0; y < hiddenSize; y++)
      {       /// NUMBER OF HIDDEN NODES
        for (int z = 0; z < hiddenSize; z++){     /// NUMBER OF OUTPUT NODES
            NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight = NN_B.HiddenLayers[x][y].OutputSynapses[z].Weight;
            //// Copy output of one layer to inputs of next layer.
            NN_A.HiddenLayers[x+1][z].InputSynapses[y].Weight = net.HiddenLayers[x][y].OutputSynapses[z].Weight;

            //// Noise
            NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight  += Random.Range(-noise, noise);
            NN_A.HiddenLayers[x+1][z].InputSynapses[y].Weight += Random.Range(-noise, noise);
          }
        }
      }
    //// HIDDEN BIASES
    for (int x = 0; x < numHiddenLayers; x++) /// NUMBER OF HIDDEN LAYERS
      {
        for (int y = 0; y < numHiddenLayers; y++) /// NUMBER OF HIDDEN LAYERS
          {
            NN_A.HiddenLayers[x][y].Bias = NN_B.HiddenLayers[x][y].Bias;

            //// Noise
            NN_A.HiddenLayers[x][y].Bias += Random.Range(-noise, noise);
          }
        }


    //// OUTPUT LAYERS
    for (int x = 0; x < hiddenSize; x++){
      for (int y = 0; y < hiddenSize; y++){   /// NUMBER OF HIDDEN NODES
        for (int z = 0; z < outputSize; z++){ /// NUMBER OF OUTPUT NODES
          NN_A.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight = NN_B.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight;

          //// Noise
          NN_A.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight += Random.Range(-noise, noise);
        }
      }
    }
    //// OUTPUT BIASES
    for (int x = 0; x < outputSize; x++){ /// NUMBER OF OUTPUT NODES
      NN_A.OutputLayer[x].Bias = NN_B.OutputLayer[x].Bias;

      //// Noise
      NN_A.OutputLayer[x].Bias += Random.Range(-noise, noise);
    }
  }

  public void memeticCopy(NeuralNet NN_A, NeuralNet NN_B)
  {
    // FireflyScript otherAgentScript = multipleAgents.agents[parentAgent].GetComponent<FireflyScript>();

    int x = numHiddenLayers-1;
    for (int y = 0; y < hiddenSize; y++)
    {       /// NUMBER OF HIDDEN NODES
      for (int z = 0; z < hiddenSize; z++){     /// NUMBER OF OUTPUT NODES
          NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight = NN_B.HiddenLayers[x][y].OutputSynapses[z].Weight;

          //// Noise
          NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight  += Random.Range(-noise, noise);
        }
      }

    //// HIDDEN BIASES
    for (int y = 0; y < numHiddenLayers; y++) /// NUMBER OF HIDDEN LAYERS
      {
        NN_A.HiddenLayers[x][y].Bias = NN_B.HiddenLayers[x][y].Bias;

        //// Noise
        NN_A.HiddenLayers[x][y].Bias += Random.Range(-noise, noise);
      }
  }


  private void RandomNNCopy(NeuralNet NN_A, NeuralNet NN_B){
    int x = 0;
    int y = 0;
    int z = 0;

    //// Input Later x:0  y:hiddenSize  z:inputSize
    x = 0;
    // y = (int)Random.Range(0, hiddenSize);
    // z = (int)Random.Range(0, inputSize);
    y = random.Next(hiddenSize);
    z = random.Next(inputSize);
    // NN.HiddenLayers[0][y].InputSynapses[z].Weight = random.Range(-1.0f, 1.0f);
    // FireflyScript otherAgentScript = multipleAgents.agents[parentAgent].GetComponent<FireflyScript>();

    NN_A.HiddenLayers[x][y].InputSynapses[z].Weight = NN_B.HiddenLayers[x][y].InputSynapses[z].Weight;


    //// Hidden Layers x:numHiddenLayers-1  y:hiddenSize  z:hiddenSize
    x = 0;
    y = random.Next(hiddenSize);
    z = random.Next(hiddenSize);
    // NN.HiddenLayers[x][y].OutputSynapses[z].Weight = random.Range(-1.0f, 1.0f);
    NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight = NN_B.HiddenLayers[x][y].OutputSynapses[z].Weight;
    //// Copy output of one layer to inputs of next layer.
    NN_A.HiddenLayers[x+1][z].InputSynapses[y].Weight = NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight;

    //// x:numHiddenLayers-1  y:  z:

    x = random.Next(numHiddenLayers-1);
    y = random.Next(hiddenSize);
    z = random.Next(outputSize);
    // NN.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight = Random.Range(-1.0f, 1.0f);
    NN_A.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight = NN_B.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight;

  }





  public void NNCopy(NeuralNet NN_A, NeuralNet NN_B) ///
  {
    bool copyOther = (Random.Range(0.0f,1.0f) > 0.5f) ? true : false;
    // FireflyScript otherAgentScript = multipleAgents.agents[parentAgent].GetComponent<FireflyScript>();
    //// INPUT LAYERS ////
    for (int y = 0; y < hiddenSize; y++) /// NUMBER OF HIDDEN NODES
    {
      for (int z = 0; z < inputSize; z++) /// NUMBER OF INPUT NODES
      {
        if (Random.Range(0.0f, 1.0f) < noise)
          copyOther = !copyOther;
        if (copyOther)
        {
          NN_A.InputLayer[z].OutputSynapses[y].Weight = NN_B.InputLayer[z].OutputSynapses[y].Weight;
          NN_A.HiddenLayers[0][y].InputSynapses[z].Weight = NN_B.HiddenLayers[0][y].InputSynapses[z].Weight;

          // if (Random.Range(0.0f, 1.0f) < noise)
          //   NN_A.InputLayer[z].OutputSynapses[y].Weight += Random.Range(-0.1f, 0.1f);

          // if (Random.Range(0.0f, 1.0f) < noise)
          //   NN_A.HiddenLayers[0][y].InputSynapses[z].Weight += Random.Range(-0.1f, 0.1f);
        }
      }
    }
    ///// Biases
    for (int x = 0; x < inputSize; x++)
    {
      if (Random.Range(0.0f, 1.0f) < noise)
        copyOther = !copyOther;
      if (copyOther)
        NN_A.InputLayer[x].Bias = NN_B.InputLayer[x].Bias;

        // if (Random.Range(0.0f, 1.0f) < noise)
        //   NN_A.InputLayer[x].Bias += Random.Range(-0.1f, 0.1f);
    }


    //// HIDDEN LAYERS ////
    for (int x = 0; x < numHiddenLayers-1; x++)
    {  /// NUMBER OF HIDDEN LAYERS
      for (int y = 0; y < hiddenSize; y++)
      {       /// NUMBER OF HIDDEN NODES
        for (int z = 0; z < hiddenSize; z++){     /// NUMBER OF OUTPUT NODES
            if (Random.Range(0.0f, 1.0f) < noise)
              copyOther = !copyOther;
            if (copyOther)
            {
              NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight = NN_B.HiddenLayers[x][y].OutputSynapses[z].Weight;

              // if (Random.Range(0.0f, 1.0f) < noise)
              // NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight += Random.Range(-0.1f, 0.1f);

              //// Copy output of one layer to inputs of next layer.
              NN_A.HiddenLayers[x+1][z].InputSynapses[y].Weight = NN_A.HiddenLayers[x][y].OutputSynapses[z].Weight;
            }
          }
        }
      }
    //// HIDDEN BIASES
    for (int x = 0; x < numHiddenLayers; x++) /// NUMBER OF HIDDEN LAYERS
      {
        for (int y = 0; y < numHiddenLayers; y++) /// NUMBER OF HIDDEN LAYERS
          {
            if (Random.Range(0.0f, 1.0f) < noise)
              copyOther = !copyOther;
            if (copyOther)
            {
              NN_A.HiddenLayers[x][y].Bias = NN_B.HiddenLayers[x][y].Bias;

              // if (Random.Range(0.0f, 1.0f) < noise)
              //   NN_A.HiddenLayers[x][y].Bias += Random.Range(-0.1f, 0.1f);
            }
          }
        }


    //// OUTPUT LAYERS
    for (int x = 0; x < hiddenSize; x++){
      for (int y = 0; y < hiddenSize; y++){   /// NUMBER OF HIDDEN NODES
        for (int z = 0; z < outputSize; z++){ /// NUMBER OF OUTPUT NODES
          if (Random.Range(0.0f, 1.0f) < noise)
            copyOther = !copyOther;
          if (copyOther)
          {
            NN_A.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight = NN_B.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight;

            // if (Random.Range(0.0f, 1.0f) < noise)
            //   NN_A.HiddenLayers[numHiddenLayers-1][y].OutputSynapses[z].Weight += Random.Range(-0.1f, 0.1f);
          }
        }
      }
    }
    //// OUTPUT BIASES
    for (int x = 0; x < outputSize; x++){ /// NUMBER OF OUTPUT NODES
      if (Random.Range(0.0f, 1.0f) < noise)
        copyOther = !copyOther;
      if (copyOther)
      {
        NN_A.OutputLayer[x].Bias = NN_B.OutputLayer[x].Bias;

        // if (Random.Range(0.0f, 1.0f) < noise)
        //   NN_A.OutputLayer[x].Bias += Random.Range(-0.1f, 0.1f);
      }
    }
  }





  private void randomRotate()
  {
    for (int i = 0; i < 3; i++)
    {
      Debug.Log(transform.rotation[i]);
    }
    // float x;
    //     x += Time.deltaTime * 10;
    //     transform.rotation = Quaternion.Euler(x,0,0);
    // float x = Random.Range()
    // if (Random.Range(0, 1) > 0.95f)
    //   transform.rotation = new Vector3(Random.Range(0,1), Random.Range(0,1), Random.Range(0,1));

  }




}
