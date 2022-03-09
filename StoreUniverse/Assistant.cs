using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using CommonSDK;
using System;
using UnityEngine.Events;

public class Assistant : Staff
{
    public static UnityAction IncreaseSpeed;
    [SerializeField, ReadOnly] private LevelManager _levelManager;
    [SerializeField, ReadOnly] protected NavMeshAgent _navMeshAgent;
    [ShowInInspector] private ArcadeMachine _targetMachine;
    public int BatteryCapacity => Managers.PlayerPrefManager.AssistantCapacity;

    #if UNITY_EDITOR
    protected override void SetRef(){
        base.SetRef();
        _levelManager = Managers.LevelManager;
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }
    #endif
    #region UNITY INIT
    protected override void OnEnable() {
        base.OnEnable();
        CommonSDK.GameManager.OnLevelStarted += OnLevelStarted;
        IncreaseSpeed += SpeedUp;
    }
    protected override void OnDisable() {
        base.OnDisable();
        CommonSDK.GameManager.OnLevelStarted -= OnLevelStarted;
        IncreaseSpeed -= SpeedUp;
    }
    #endregion
    #region EVENTS
    private void OnLevelStarted()
    {
        Activate();
    }        
    #endregion
    #region UNITY LOOP
    private void Update() {
        if ((_targetMachine != null && !_targetMachine.Needy && BatteryList.Count > 0) || 
        (_targetMachine == null && Managers.GameManager.GameState == eGameState.Playing))
        {
            refillAMachine();
            //_navMeshAgent.up
        }
    }
    #endregion
    #region PHYSICS
    protected override void OnTriggerEnter(Collider i_Collider) {
        base.OnTriggerEnter(i_Collider);
        switch (i_Collider.tag)
        {
            case nameof(eTags.AssistantBatteryArea):
                stopMoving();               
                for (int i = 0; i < BatteryCapacity; i++)
                {
                    GetBattery(_levelManager.CurrentLevel.BatteryStorage.GiveBattery());
                }
                refillAMachine();
                break;
            case nameof(eTags.CheckOut):
                break;
            case nameof(eTags.Arcade):
                if (_targetMachine.gameObject == i_Collider.gameObject)
                {
                    stopMoving();
                    StartCoroutine(_targetMachine.DelayedRefill(this));
                    getNewBatteries();
                }
                break;
            default:
                break;
        }
    }
    #endregion

    #region CLASS FUNCTIONS
    public void SpeedUp(){
        _navMeshAgent.speed = GameConfig.Assistant.Speed * Managers.PlayerPrefManager.AssistantSpeed;
    }
    public void Activate(){
        SpeedUp();
        startMoving(_levelManager.CurrentLevel.BatteryStorage.AssistantAreaPosition);
    }
    private void refillAMachine(){
        _targetMachine = _levelManager.GetRandomEmptyMachine();
        if (_targetMachine != null && BatteryList.Count > 0)
        {
            startMoving(_targetMachine.transform.position);   
        }
        else if(BatteryList.Count == 0)
        {
            getNewBatteries();
        }
        else
        {
            stopMoving();
        }
    }
    private void getNewBatteries(){
        startMoving(_levelManager.CurrentLevel.BatteryStorage.AssistantAreaPosition);
    }
    private void startMoving(Vector3 i_Vector)
    {
        _navMeshAgent.isStopped = false;
        _navMeshAgent.SetDestination(i_Vector);
        MovementTransition(1);
    }
    private void stopMoving(){
        MovementTransition(0);
        _navMeshAgent.isStopped = true;
    }
    protected override void ResetStaff()
    {
        base.ResetStaff();
        _targetMachine = null;
        stopMoving();
    }
    #endregion
}