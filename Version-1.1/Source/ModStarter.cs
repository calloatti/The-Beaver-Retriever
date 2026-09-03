using HarmonyLib;
using Timberborn.Modding;
using Timberborn.ModManagerScene;

namespace Calloatti.TheBeaverRetriever
{
  public class ModStarter : IModStarter
  {
    public void StartMod(IModEnvironment modEnvironment)
    {
      new Harmony("Calloatti.TheBeaverRetriever").PatchAll();
    }
  }
}