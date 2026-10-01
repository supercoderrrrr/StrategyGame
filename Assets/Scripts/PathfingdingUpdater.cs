using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfingdingUpdater : MonoBehaviour
{
    private void Start()
    {
        DestructibleCrate.OnAnyDestroyed += DestructibleCrate_OnAnyDestroyed;
    }

    private void DestructibleCrate_OnAnyDestroyed(object sender, EventArgs e)
    {
        DestructibleCrate destructibleCrate = sender as DestructibleCrate;
        PathFinding.Instance.SetIsWalkableGridPosition(destructibleCrate.GetGridPosition(), true);
    }

    private void OnDestroy()
    {
        DestructibleCrate.OnAnyDestroyed -= DestructibleCrate_OnAnyDestroyed;
    }
}
