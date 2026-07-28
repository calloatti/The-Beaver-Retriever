using HarmonyLib;
using System.Collections.Generic;
using Timberborn.CharacterModelSystem;
using Timberborn.GameDistricts;
using Timberborn.Navigation;
using Timberborn.WalkingSystem;
using Timberborn.Wandering;
using UnityEngine;

namespace Calloatti.TheBeaverRetriever
{
  public static class UnstuckHelpers
  {
    public const int MaxRadius = 8;
    public const int MaxZ = 32;

    public static bool TrySpiralSearch(Citizen citizen, DistrictCenter district)
    {
      Vector3Int gridPos = NavigationCoordinateSystem.WorldToGridInt(citizen.Transform.position);

      for (int radius = 1; radius <= MaxRadius; radius++)
      {
        foreach (Vector2Int offset in GetRingOffsets(radius))
        {
          int baseX = gridPos.x + offset.x;
          int baseY = gridPos.y + offset.y;

          for (int z = gridPos.z; z >= 0; z--)
          {
            if (TryTeleport(citizen, district, baseX, baseY, z))
              return true;
          }

          for (int z = gridPos.z + 1; z <= MaxZ; z++)
          {
            if (TryTeleport(citizen, district, baseX, baseY, z))
              return true;
          }
        }
      }

      return false;
    }

    private static bool TryTeleport(Citizen citizen, DistrictCenter district, int x, int y, int z)
    {
      Vector3Int checkGrid = new Vector3Int(x, y, z);
      Vector3 checkWorld = NavigationCoordinateSystem.GridToWorld(checkGrid);

      if (!district.IsGloballyReachableFromPosition(checkWorld))
        return false;

      citizen.Transform.position = checkWorld;
      citizen.GetComponent<CharacterModel>().Position = checkWorld;
      citizen.GetComponent<Walker>()?.StopNextTick();
      return true;
    }

    public static IEnumerable<Vector2Int> GetRingOffsets(int radius)
    {
      for (int x = -radius; x <= radius; x++)
        yield return new Vector2Int(x, -radius);
      for (int x = -radius; x <= radius; x++)
        yield return new Vector2Int(x, radius);
      for (int y = -radius + 1; y <= radius - 1; y++)
        yield return new Vector2Int(-radius, y);
      for (int y = -radius + 1; y <= radius - 1; y++)
        yield return new Vector2Int(radius, y);
    }
  }

  [HarmonyPatch(typeof(StrandedRootBehavior), nameof(StrandedRootBehavior.Decide))]
  public static class StrandedRootBehaviorPatch
  {
    [HarmonyPrefix]
    public static void Prefix(StrandedRootBehavior __instance)
    {
      if (__instance._citizen == null || __instance._citizen.HasAssignedDistrict)
        return;

      var unstucker = __instance._citizen._citizenUnstucker;
      foreach (var districtCenter in unstucker._districtCenterRegistry.FinishedDistrictCenters)
      {
        if (UnstuckHelpers.TrySpiralSearch(__instance._citizen, districtCenter))
          return;
      }
    }
  }
}
