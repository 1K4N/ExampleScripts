using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using CommonSDK;
using System;

public class Staff : MonoBehaviour
{
    [SerializeField, ReadOnly] protected Animator Animator;
    [SerializeField, HideInInspector] protected PoolManager PoolManager;
    [SerializeField, HideInInspector] protected GameConfig GameConfig;
    [SerializeField, HideInInspector] protected Managers Managers;
    [SerializeField, HideInInspector] protected Vector3 StartPosition;
    [SerializeField, HideInInspector] protected Vector3 StartScale;
    [SerializeField, ReadOnly] protected Transform BatteryParent;
    [SerializeField, ReadOnly] protected Transform Model;
    [SerializeField] protected int CollectCapacity = 1;

    private Battery _battery;
    private Vector3 _batteryPosition;
    protected List<Battery> BatteryList = new List<Battery>();
    private Sequence _batterySequence;
    private Sequence _batteryCollectBounce;
    protected Tween AnimationSlowDown;
    protected float Speed;
    protected float Velocity;
    protected bool IsRunning;
    public float CollectRank => Managers.PlayerPrefManager.PlayerLoadRank;
    public int BatteryCount => BatteryList.Count;

    #if UNITY_EDITOR
    [Button]
    protected virtual void SetRef(){
        Animator = GetComponentInChildren<Animator>();
        Managers = Managers.Instance;
        PoolManager = Managers.PoolManager;
        GameConfig = GameConfig.Instance;
        StartPosition = transform.position;
        BatteryParent = transform.FindDeepChild<Transform>("Batteries");
        Model = transform.FindDeepChild<Transform>("Model");
        StartScale = Model.localScale;
    }
    private void OnValidate() {
        SetRef();
    }
    #endif
    #region UNITY_INIT
    protected virtual void OnEnable() {
        CommonSDK.GameManager.OnLevelLoaded += OnLevelLoaded;
    }
    protected virtual void OnDisable() {
        CommonSDK.GameManager.OnLevelLoaded -= OnLevelLoaded;
    }
    #endregion
    #region Events
    private void OnLevelLoaded()
    {
        ResetStaff();
    }
    #endregion
    #region Physics
    protected virtual void OnTriggerEnter(Collider i_Collider) {
        switch (i_Collider.tag)
        {
            case nameof(eTags.BatterySection):
                break;
            case nameof(eTags.CheckOut):
                break;
            default:
                break;
        }
    }
    #endregion
    #region Class Functions
    public void GiveBattery(Battery i_Battery){
        _battery = BatteryList[BatteryList.Count - 1];
        _battery.transform.SetParent(null);
        moveBattery(_battery, i_Battery);
        BatteryList.Remove(_battery);
    }
    private void moveBattery(Battery i_SourceBattery, Battery i_TargetBattery){
        DOTween.Sequence()
            .Append(i_SourceBattery.Model.DOMove(i_TargetBattery.Model.position, GameConfig.Battery.MachineRechargeTween.Duration)
                .SetEase(GameConfig.Battery.MachineRechargeTween.Ease))
               .Join(i_SourceBattery.UpDown.DOLocalMove(i_SourceBattery.UpDown.InverseTransformPoint(i_SourceBattery.UpDown.position + Vector3.up * GameConfig.Battery.MoveUp.Value), GameConfig.Battery.MachineRechargeTween.Duration / 2)
                .SetEase(GameConfig.Battery.MoveUp.Ease)
                .SetLoops(2, LoopType.Yoyo))
            .Join(i_SourceBattery.Rotator.transform.DORotateQuaternion(i_TargetBattery.Rotator.rotation, GameConfig.Battery.MachineRechargeTween.Duration)
                .SetEase(GameConfig.Battery.MachineRechargeTween.Ease))
            .Join(i_SourceBattery.transform.DOScale(i_TargetBattery.transform.localScale, GameConfig.Battery.MachineRechargeTween.Duration)
                .SetEase(GameConfig.Battery.MachineRechargeTween.Ease))
            .OnComplete(()=> {
                i_TargetBattery.gameObject.SetActive(true);
                PoolManager.BatteryPool.Release(i_SourceBattery);
            });
    }
    public void GetBattery(Battery i_Battery){
        i_Battery.transform.SetParent(BatteryParent);
        _batteryPosition.Set(0, 
            (GameConfig.Player.BatteryGap * GameConfig.Player.BatteryRowCount) * (int)(BatteryList.Count / (GameConfig.Player.BatteryRowCount * GameConfig.Player.BatteryColumnCount)) + (GameConfig.Player.BatteryGap * (BatteryList.Count % GameConfig.Player.BatteryRowCount)), 
            -GameConfig.Player.BatteryGap * ((int)(BatteryList.Count / GameConfig.Player.BatteryRowCount) % GameConfig.Player.BatteryColumnCount));
        BatteryList.Add(i_Battery);
        DOTween.Sequence()
            .Append(i_Battery.transform.DOLocalMove(_batteryPosition, GameConfig.Battery.GetBatteryTween.Duration)
                .SetEase(GameConfig.Battery.GetBatteryTween.Ease))
            .Join(i_Battery.transform.DOLocalRotate(new Vector3(0,0,270), GameConfig.Battery.GetBatteryTween.Duration)
                .SetEase(GameConfig.Battery.GetBatteryTween.Ease))
            .Join(i_Battery.transform.DOScale(.5f, GameConfig.Battery.GetBatteryTween.Duration)
                .SetEase(GameConfig.Battery.GetBatteryTween.Ease))
            .OnComplete(()=> {
                _batteryCollectBounce.Kill();
                _batteryCollectBounce = DOTween.Sequence()
                    .Append(Model.DOScaleY(StartScale.y - GameConfig.Player.ScaleDown.Value, GameConfig.Player.ScaleDown.Duration)
                        .SetEase(GameConfig.Player.ScaleDown.Ease))
                    .Append(Model.DOScaleY(StartScale.y, GameConfig.Player.ScaleUp.Duration)
                        .SetEase(GameConfig.Player.ScaleUp.Ease));
            });
    }
    protected virtual void Move(){
    }
    protected void MovementTransition(float i_To){
        if (i_To == 0 || i_To == 1)
        {
            AnimationSlowDown = 
                DOTween.To(() => Velocity, x => Velocity = x, i_To, GameConfig.Player.AnimationSlowDownTween.Duration)
                    .SetEase(GameConfig.Player.AnimationSlowDownTween.Ease)
                    .OnUpdate(()=>Animator.SetFloat("velocity", Velocity));  
        }
    }
    protected void Rotate(Vector2 i_Direction)
    {
        if (i_Direction == Vector2.zero) return;

        transform.rotation = Quaternion.Lerp(transform.rotation, 
            Quaternion.LookRotation(new Vector3(i_Direction.x, 0, i_Direction.y)),
            GameConfig.Player.RotateLerp);

    }
    protected virtual void ResetStaff(){
        _batteryCollectBounce?.Kill();
        Model.localScale = StartScale;
        Speed = GameConfig.Player.MovementSpeed;
        transform.position = StartPosition;
        transform.rotation = Quaternion.Euler(Vector3.zero);
        IsRunning = false;
        Animator.SetBool("isRunning", IsRunning);
        Animator.SetTrigger("restart");
        BatteryList.Clear();
    }
    #endregion
}
