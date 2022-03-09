using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using CommonSDK;
using System;
public class Player : Staff
{
    [SerializeField, ReadOnly] private CharacterController _characterController;
    [SerializeField, HideInInspector] private InputManager _inputManager;

    private Sequence _fallingSequence;
    private bool _isFalling;
    private Vector3 _joystickDirection;
    private Vector3 _gravityForce;
    public int BatteryCapacity => Managers.PlayerPrefManager.PlayerCapacity;

    #if UNITY_EDITOR
    protected override void SetRef(){
        base.SetRef();
        _characterController = GetComponent<CharacterController>();
        _inputManager = Managers.InputManager;
    }
    #endif
    #region UNITY_INIT
    protected override void OnEnable() {
        base.OnEnable();
        InputManager.OnDrag += OnDrag;
        InputManager.OnInputUp += OnInputUp;
        CommonSDK.GameManager.OnLevelLoaded += OnLevelLoaded;
    }
    protected override void OnDisable() {
        base.OnDisable();
        InputManager.OnDrag -= OnDrag;
        InputManager.OnInputUp -= OnInputUp;
        CommonSDK.GameManager.OnLevelLoaded -= OnLevelLoaded;
    }
    #endregion
    #region Events
    private void OnDrag(Vector2 i_Pos){
        _joystickDirection.Set(_inputManager.JoystickDirection.x, _joystickDirection.y, _inputManager.JoystickDirection.y);
        Move();
        Rotate(_inputManager.JoystickDirection);
    }
    private void OnInputUp()
    {
        Velocity = _joystickDirection.magnitude;
        _joystickDirection.Set(0,_joystickDirection.y,0);
        
        MovementTransition(0);

        if (IsRunning && !_isFalling)
        {
            IsRunning = false;
            Animator.SetBool("isRunning", IsRunning);
        }
    }
    private void OnLevelLoaded()
    {
        resetPlayer();
    }
    #endregion
    private void Update() {
        gravityForce();
    }
    #region Physics
    protected override void OnTriggerEnter(Collider i_Collider) {
        base.OnTriggerEnter(i_Collider);
        switch (i_Collider.tag)
        {
            case nameof(eTags.BatterySection):
                break;
            case nameof(eTags.CollectMoney):
                break;
            default:
                break;
        }
    }
    #endregion
    #region Class Functions
    private void gravityForce(){
        if (Managers.GameManager.GameState == eGameState.Playing)
        {
            if (_characterController.isGrounded)
            {
                _gravityForce.y = -.05f;
            }
            else
            {
                if (_gravityForce.y > GameConfig.Gravity.MaxForce)
                {
                    _gravityForce.y -= GameConfig.Gravity.Force;
                    if (!_isFalling)
                    {                        
                        _gravityForce.Set(0, _gravityForce.y, 0);
                        _isFalling = true;
                        //slopedGravityForce();
                    }
                }
                _characterController.Move(_gravityForce * Time.fixedDeltaTime * GameConfig.Player.MovementSpeed);
            }
        }
    }
    private void slopedGravityForce(){
        _fallingSequence?.Kill();
        _gravityForce.Set(_joystickDirection.x / GameConfig.Gravity.Divider, _gravityForce.y, _joystickDirection.z / GameConfig.Gravity.Divider);
        _fallingSequence = DOTween.Sequence()
            .Append(DOTween.To(() => _gravityForce.x, x => _gravityForce.x = x, 0, GameConfig.Gravity.DecreaseTween.Duration))
            .Join(DOTween.To(() => _gravityForce.z, x => _gravityForce.z = x, 0, GameConfig.Gravity.DecreaseTween.Duration))
            .SetEase(GameConfig.Gravity.DecreaseTween.Ease)
            .SetUpdate(UpdateType.Fixed);
    }
    protected override void Move(){
        base.Move();
        if (_characterController.isGrounded)
        {
            _characterController.Move(new Vector3(_joystickDirection.x, _gravityForce.y, _joystickDirection.z) * Time.fixedDeltaTime * GameConfig.Player.MovementSpeed);
            Animator.SetFloat("velocity", _joystickDirection.magnitude);
            if (_isFalling)
            {
                _isFalling = false;
                _fallingSequence?.Kill();
                AnimationSlowDown?.Kill();
            }
        }
             
        if (!IsRunning && _joystickDirection.magnitude > 0)
        {
            IsRunning = true;
            Animator.SetBool("isRunning", IsRunning);
        }
    }
    private void resetPlayer(){
        _inputManager.InputUp();
        _gravityForce = Vector3.zero;
        Speed = GameConfig.Player.MovementSpeed;     
        _isFalling = false;
    }
    #endregion
}
