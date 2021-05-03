﻿using System;
using UnityEngine;

public sealed class DoorManager : MonoBehaviour, IChangeable// класс реализует взаимодействие, а так же движение двери
{
    [SerializeField] private State currentState;
    private State nextState = State.nullable;
    public bool IsOpen { get; private set; }// открытость двери
    private bool canInteract { get; set; } = true;// возможность взаимодействия

    [SerializeField] private Vector3 openState = new Vector3(0, -90, 0);// открытое состояние

    [SerializeField] private Vector3 lockState = new Vector3(0, 0, 0);// закрытое состояние

    private float lerpRate { get; set; } = 1;// скорость обычного движения двери
    private float ExtrimLerpRate { get; set; } = 100;// скорость экстремального движения двери
    private DoorMesh lastDoorMesh;

    private bool isExtrimSituation;

    private void Awake()
    {
        //  ExtrimRotate(openState);
    }
    public void Interact(DoorMesh doorMesh)
    {
        if (!canInteract)
            return;

        lastDoorMesh = doorMesh;
        if (currentState == State.locked)
        {
            SetDescription();

            return;
        }

        canInteract = !canInteract;
        IsOpen = !IsOpen;
        lastDoorMesh.SetType("None");
    }
    public void SetStateAfterNextInteract(State state)
    {
        nextState = state;
    }
    private void FixedUpdate()
    {
        if (IsOpen && !canInteract)
        {
            if (canInteract = Rotate(openState))
                SetDescription();
        }
        else if (!canInteract)
        {
            if (canInteract = Rotate(lockState))
                SetDescription();
        }
    }
    private bool Rotate(Vector3 state)
    {
        return isExtrimSituation ? ExtrimRotate(state) : UsuallyRotate(state);
    }
    private bool UsuallyRotate(Vector3 state)
    {

        Quaternion rotation = Quaternion.Euler(state);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, rotation, lerpRate * Time.deltaTime);

        return Math.Round(transform.localRotation.y, 5) == Math.Round(rotation.y, 5);
    }

    private bool ExtrimRotate(Vector3 state)
    {
        Quaternion needRotation = Quaternion.Euler(0.0f, state.y, 0.0f);
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, needRotation, ExtrimLerpRate * Time.deltaTime);

        return Quaternion.Angle(transform.localRotation, needRotation) < 0.01f;
    }
    private void SetDescription()
    {
        string output;
        if (currentState == State.locked)
        {
            output = InteractiveObject.Types.LockedDoor;
        }
        else if (IsOpen)
        {
            output = InteractiveObject.Types.OpenedDoor;
        }
        else
        {
            output = InteractiveObject.Types.ClosedDoor;
        }
        lastDoorMesh.SetType(output);
        ChangeState();
    }
    public void SetType(DoorMesh d)
    {
        lastDoorMesh = d;
        SetDescription();
    }
    public void SetExtrimSituation(bool value)
    {
        isExtrimSituation = value;
    }

    public void ChangeState()
    {
        if (nextState != State.nullable)
        {
            currentState = nextState;
            nextState = State.nullable;
        }
    }
    public State GetState()
    {
        return currentState;
    }
}
