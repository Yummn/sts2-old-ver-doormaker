using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace OldVerDoormakerMod;

[ModInitializer("Init")]
public static class Bootstrap
{
	private static bool _initialized;

	public static void Init()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		Harmony instance = new Harmony("boninall.oldverdoormaker");
		int num = 0;
		int num2 = 0;
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in types)
		{
			try
			{
				if (type.GetCustomAttributes(typeof(HarmonyPatch), inherit: true).Length != 0)
				{
					new PatchClassProcessor(instance, type).Patch();
					num++;
				}
			}
			catch (Exception ex)
			{
				num2++;
				Log.Error("[OldVerDoormaker] Patch failed for " + type.Name + ": " + (ex.InnerException?.Message ?? ex.Message));
				RuntimeLog.Write("Patch failed for " + type.Name + ": " + (ex.InnerException?.Message ?? ex.Message));
			}
		}
		Log.Info($"[OldVerDoormaker] Patch summary: {num} applied, {num2} failed.");
		RuntimeLog.Write($"Patch summary: {num} applied, {num2} failed.");
	}
}
