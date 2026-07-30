using HarmonyLib;
using Timberborn.Characters;
using Timberborn.GameDistricts;
using Timberborn.Wandering;
using UnityEngine;

namespace Calloatti.TheBeaverRetriever
{
  [HarmonyPatch(typeof(Citizen), nameof(Citizen.AssignDistrict))]
  public static class CitizenAssignPatch
  {
    [HarmonyPostfix]
    public static void Postfix(Citizen __instance)
    {
      if (__instance.AssignedDistrict != null)
      {
        UnstuckHelpers.SetPreviousDistrict(__instance, __instance.AssignedDistrict);
      }
    }
  }

  [HarmonyPatch(typeof(Citizen), nameof(Citizen.UnassignDistrictIfCutOff))]
  public static class CitizenUnassignPatch
  {
    [HarmonyPrefix]
    public static void Prefix(Citizen __instance)
    {
      if (__instance.HasAssignedDistrict)
      {
        UnstuckHelpers.SetPreviousDistrict(__instance, __instance.AssignedDistrict);
      }
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
      var districtCenters = unstucker._districtCenterRegistry.FinishedDistrictCenters;

      Vector3 beaverPos = __instance._citizen.Transform.position;
      foreach (var dc in districtCenters)
      {
        if (dc.IsGloballyReachableFromPosition(beaverPos))
          return;
      }

      DistrictCenter previousDistrict = null;
      UnstuckHelpers.TryGetPreviousDistrict(__instance._citizen, out previousDistrict);

      UnstuckHelpers.TryFindReachableTowardDistrict(__instance._citizen, districtCenters, previousDistrict);
    }
  }

  [HarmonyPatch(typeof(Character), nameof(Character.KillCharacter))]
  public static class CharacterKillPatch
  {
    [HarmonyPostfix]
    public static void Postfix(Character __instance)
    {
      var citizen = __instance.GetComponent<Citizen>();
      if (citizen != null && UnstuckHelpers.HasPreviousDistrict(citizen))
      {
        UnstuckHelpers.ClearPreviousDistrict(citizen);
      }
    }
  }
}
