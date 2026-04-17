using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(MovementController))]
public class BotAgent : Agent
{
    private MovementController movementController;
    private BombController bombController;
    private Vector3 startingPosition;
    private GameObject[] allPlayers;

    // Sistema de vides intern (un episodi = una partida completa amb 3 vides)
    private int _lives = 3;
    private const int MaxLives = 3;

    // Cooldown per evitar spam de bombes
    private float _bombCooldown = 0f;
    private const float BombCooldownTime = 4f;

    // Per detectar si s'ha matat amb la seva pròpia bomba
    private bool _diedByOwnBomb = false;

    public override void Initialize()
    {
        movementController = GetComponent<MovementController>();
        bombController = GetComponent<BombController>();

        if (movementController != null)
            movementController.isBot = true;

        startingPosition = transform.localPosition;
        allPlayers = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log("[BotAgent] Initialize OK - " + gameObject.name);
    }

    private void Start()
    {
        Debug.Log("[BotAgent] Start - " + gameObject.name);
    }

    public override void OnEpisodeBegin()
    {
        // Reinici complet al principi de cada episodi (nova partida de 3 vides)
        _lives = MaxLives;
        _bombCooldown = 0f;

        Respawn();

        allPlayers = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log("[BotAgent] Nou episodi - Vides: " + _lives);
    }

    /// <summary>
    /// Respawn al punt inicial sense acabar l'episodi.
    /// </summary>
    private void Respawn()
    {
        if (movementController != null)
            movementController.CancelDeathSequence();

        if (bombController != null)
            bombController.CancelBombs();

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        transform.localPosition = startingPosition;

        if (movementController != null)
        {
            movementController.enabled = true;
            movementController.SetRemoteState("idle");
        }
        if (bombController != null)
            bombController.enabled = true;

        _diedByOwnBomb = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // El sensor visual principal ahora será el RayPerceptionSensor2D que añadiremos en Unity.
        // Aquí solo pasaremos 1 observación interna: ¿Tengo bomba disponible y sin cooldown?
        bool hasBomb = bombController != null && bombController.enabled
                       && bombController.bombAmount > 0 && _bombCooldown <= 0f;
        sensor.AddObservation(hasBomb ? 1f : 0f);

        // Nota visual: En el componente "Behavior Parameters" del Bot en Unity,
        // asegúrate de que el 'Space Size' dentro de Vector Observation sea de valor 1.
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        _bombCooldown -= Time.fixedDeltaTime;

        // Branch 0: Moviment (0=idle, 1=up, 2=down, 3=left, 4=right)
        int moveAction = actionBuffers.DiscreteActions[0];
        if (movementController != null && movementController.enabled)
        {
            string dirName = "idle";
            if (moveAction == 1)      dirName = "up";
            else if (moveAction == 2) dirName = "down";
            else if (moveAction == 3) dirName = "left";
            else if (moveAction == 4) dirName = "right";
            movementController.SetRemoteState(dirName);
        }

        // Branch 1: Bomba (0=no, 1=posar) amb cooldown
        int bombAction = actionBuffers.DiscreteActions[1];
        if (bombAction == 1 && bombController != null && bombController.enabled && _bombCooldown <= 0f)
        {
            bombController.RemotePlaceBomb();
            _bombCooldown = BombCooldownTime;
            // Sense penalització: el bot aprendrà sol que bombes prop d'ell = mort (-0.5/-1.5)
            // i bombes prop d'enemics = kill (+1.0)
        }

        // Recompensa per sobreviure cada pas
        AddReward(0.001f);
    }

    /// <summary>
    /// Cridat des de MovementController quan el bot és impactat per una explosió.
    /// Implementa el sistema de 3 vides: respawn si en queden, EndEpisode si no.
    /// </summary>
    public void OnBotDeath()
    {
        // MODO ENTRENAMIENTO: Respawn infinito sin límite de vidas, penalización completa.
        if (GameManager.Instance != null && GameManager.Instance.isTrainingMode)
        {
            float deathReward = _diedByOwnBomb ? -1.5f : -1.0f;
            AddReward(deathReward);
            Debug.Log("[BotAgent] Mort en entrenament. Reward: " + deathReward);
            Respawn();
            return;
        }

        // MODO PARTIDA NORMAL: Límite de 3 vidas
        _lives--;
        Debug.Log("[BotAgent] Mort! Vides restants: " + _lives);

        if (_lives <= 0)
        {
            float deathReward = _diedByOwnBomb ? -1.5f : -1.0f;
            AddReward(deathReward);
            Debug.Log("[BotAgent] Fi d'episodi. Reward: " + deathReward);
            EndEpisode();
        }
        else
        {
            float deathReward = _diedByOwnBomb ? -0.5f : -0.3f;
            AddReward(deathReward);
            Respawn();
        }
    }

    /// <summary>
    /// Marca que el bot ha mort per la seva pròpia bomba (penalització extra).
    /// Cridat des de MovementController si l'owner de l'explosió és ell mateix.
    /// </summary>
    public void MarkSelfKill()
    {
        _diedByOwnBomb = true;
    }

    /// <summary>
    /// Cridat des de GameManager quan el bot mata un enemic.
    /// </summary>
    public void OnBotKill()
    {
        AddReward(1.0f);
        Debug.Log("[BotAgent] Kill! Reward +1.0 — Vides: " + _lives);
    }

    private GameObject GetNearestEnemy()
    {
        GameObject nearest = null;
        float minDist = float.MaxValue;
        if (allPlayers == null) return null;
        foreach (GameObject p in allPlayers)
        {
            if (p == null || !p.activeSelf || p == gameObject) continue;
            float d = Vector3.Distance(transform.localPosition, p.transform.localPosition);
            if (d < minDist) { minDist = d; nearest = p; }
        }
        return nearest;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = 0; d[1] = 0;
        if (Input.GetKey(KeyCode.I))      d[0] = 1;
        else if (Input.GetKey(KeyCode.K)) d[0] = 2;
        else if (Input.GetKey(KeyCode.J)) d[0] = 3;
        else if (Input.GetKey(KeyCode.L)) d[0] = 4;
        if (Input.GetKey(KeyCode.B)) d[1] = 1;
    }
}
