using Bindito.Core;
using System;
using System.Collections.Generic;
using Timberborn.Characters;
using Timberborn.Common;
using Timberborn.CharacterModelSystem;
using Timberborn.GameDistricts;
using Timberborn.Navigation;
using Timberborn.WalkingSystem;
using UnityEngine;

namespace Calloatti.TheBeaverRetriever
{
  [Context("Game")]
  public class RetrieverConfigurator : Configurator
  {
    protected override void Configure()
    {
      Bind<RetrieverGameState>().AsSingleton();
    }
  }

  public class RetrieverGameState : IDisposable
  {
    public void Dispose()
    {
      UnstuckHelpers.ClearAll();
    }
  }

  public static class UnstuckHelpers
  {
    private static readonly Dictionary<Citizen, DistrictCenter> _previousDistricts = new Dictionary<Citizen, DistrictCenter>();

    public const int MaxZ = 32;

    public static void SetPreviousDistrict(Citizen citizen, DistrictCenter district)
    {
      _previousDistricts[citizen] = district;
    }

    public static bool TryGetPreviousDistrict(Citizen citizen, out DistrictCenter previousDistrict)
    {
      return _previousDistricts.TryGetValue(citizen, out previousDistrict);
    }

    public static bool HasPreviousDistrict(Citizen citizen)
    {
      return _previousDistricts.ContainsKey(citizen);
    }

    public static void ClearPreviousDistrict(Citizen citizen)
    {
      _previousDistricts.Remove(citizen);
    }

    public static void ClearAll()
    {
      _previousDistricts.Clear();
    }

    public static bool TryFindReachableTowardDistrict(Citizen citizen, ReadOnlyList<DistrictCenter> districtCenters, DistrictCenter preferredDistrict = null)
    {
      if (preferredDistrict != null && districtCenters.Contains(preferredDistrict))
      {
        if (TryFindReachableTowardSingleDistrict(citizen, preferredDistrict))
          return true;
      }

      if (districtCenters.Count == 0)
        return false;

      DistrictCenter nearest = null;
      float minDist = float.MaxValue;
      foreach (var dc in districtCenters)
      {
        float dist = dc.DistanceToCitizen(citizen);
        if (dist < minDist)
        {
          minDist = dist;
          nearest = dc;
        }
      }
      if (nearest == null)
        return false;

      return TryFindReachableTowardSingleDistrict(citizen, nearest);
    }

    private static bool TryFindReachableTowardSingleDistrict(Citizen citizen, DistrictCenter dc)
    {
      Vector3 beaverPos = citizen.Transform.position;
      Vector3 dcPos = NavigationCoordinateSystem.GridToWorld(dc.CenterCoordinates);
      Vector3 direction = (dcPos - beaverPos).normalized;
      Vector3Int gridPos = NavigationCoordinateSystem.WorldToGridInt(beaverPos);
      Vector3Int dcGrid = dc.CenterCoordinates;

      int maxSteps = Mathf.Max(Mathf.Abs(dcGrid.x - gridPos.x), Mathf.Abs(dcGrid.y - gridPos.y));
      maxSteps = Mathf.Max(maxSteps, Mathf.Abs(dcGrid.z - gridPos.z));
      maxSteps = Mathf.Max(maxSteps, 1);

      for (int dist = 1; dist <= maxSteps; dist++)
      {
        Vector3 targetWorld = beaverPos + direction * dist;
        Vector3Int targetGrid = NavigationCoordinateSystem.WorldToGridInt(targetWorld);

        for (int z = gridPos.z; z >= 0; z--)
        {
          var checkGrid = new Vector3Int(targetGrid.x, targetGrid.y, z);
          Vector3 checkWorld = NavigationCoordinateSystem.GridToWorld(checkGrid);
          if (dc.IsGloballyReachableFromPosition(checkWorld))
          {
            TeleportAndAssignCitizen(citizen, checkWorld, dc);
            return true;
          }
        }
        for (int z = gridPos.z + 1; z <= MaxZ; z++)
        {
          var checkGrid = new Vector3Int(targetGrid.x, targetGrid.y, z);
          Vector3 checkWorld = NavigationCoordinateSystem.GridToWorld(checkGrid);
          if (dc.IsGloballyReachableFromPosition(checkWorld))
          {
            TeleportAndAssignCitizen(citizen, checkWorld, dc);
            return true;
          }
        }
      }

      Vector3 dcWorld = NavigationCoordinateSystem.GridToWorld(dcGrid);
      if (dc.IsGloballyReachableFromPosition(dcWorld))
      {
        TeleportAndAssignCitizen(citizen, dcWorld, dc);
        return true;
      }
      return false;
    }

    private static void TeleportAndAssignCitizen(Citizen citizen, Vector3 worldPos, DistrictCenter district)
    {
      citizen.Transform.position = worldPos;
      citizen.GetComponent<CharacterModel>()!.Position = worldPos;
      citizen.GetComponent<Walker>()?.StopNextTick();

      citizen.AssignDistrict(district);
    }
  }

}
