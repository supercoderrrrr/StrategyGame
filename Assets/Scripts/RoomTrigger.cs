using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    [SerializeField] private Room roomToReveal;

    private readonly HashSet<MoveAction> pendingMoveActions = new HashSet<MoveAction>();

    private void OnTriggerEnter(Collider other)
    {
        Unit unit = other.GetComponentInParent<Unit>();

        if (unit == null || unit.IsEnemy() || roomToReveal == null || roomToReveal.IsRevealed())
        {
            return;
        }

        MoveAction moveAction = unit.GetAction<MoveAction>();
        if (moveAction == null || !moveAction.IsActive)
        {
            roomToReveal.RevealRoom();
            return;
        }

        if (pendingMoveActions.Add(moveAction))
        {
            moveAction.OnStopMoving += MoveAction_OnStopMoving;
        }
    }

    private void MoveAction_OnStopMoving(object sender, EventArgs eventArgs)
    {
        RevealRoomAndClearPendingMoves();
    }

    private void RevealRoomAndClearPendingMoves()
    {
        foreach (MoveAction moveAction in pendingMoveActions)
        {
            if (moveAction != null)
            {
                moveAction.OnStopMoving -= MoveAction_OnStopMoving;
            }
        }

        pendingMoveActions.Clear();

        if (roomToReveal != null && !roomToReveal.IsRevealed())
        {
            roomToReveal.RevealRoom();
        }
    }

    private void OnDisable()
    {
        foreach (MoveAction moveAction in pendingMoveActions)
        {
            if (moveAction != null)
            {
                moveAction.OnStopMoving -= MoveAction_OnStopMoving;
            }
        }

        pendingMoveActions.Clear();
    }
}
