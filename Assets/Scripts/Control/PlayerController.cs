using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.System;

public class PlayerController : Singleton<PlayerController>
{
    private GameControl _inputActions;

    protected override void Awake()
    {
        base.Awake();
        _inputActions = new GameControl();
    }

    private void OnEnable()
    {
        _inputActions.Enable();
    }

}
