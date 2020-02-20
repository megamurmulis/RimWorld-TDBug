﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using HarmonyLib;
using RimWorld;

namespace TDBug
{
	public static class MoreDebugActions
	{
		//	[DebugAction("General", null, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		//	[DebugAction("Pawns", null, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		/*
		 * I would like to insert these things in specific places, but the ease of DebugAction makes that not so possible
		 * 
			{ "T: Try place near stacks of 75...", new DA() {label = "T: Try place near full stacks...", action = "fullStackAction"} },
			{ "Destroy all things", new DA() {label = "Destroy all selected", action = "destroySelectedAction"} },
			{ "T: Heal random injury (10)", new DA() {label = "T: Full Heal", action = "healFullAction", tool="DebugToolMapForPawns" } },
			{ "T: Make roof", new DA() {label = "T: Make roof (by def)", action = "makeRoofByDef"} },
			{ "T: Damage apparel", new DA() {label = "T: Add selected things to inventory", action = "addSeltoInv", tool="DebugToolMapForPawns"} },
			{ "T: Joy -20%", new DA() {label = "T: Need -20%", action = "addNeed"} },
			{ "T: Chemical -20%", new DA() {label = "Fulfill all needs", action = "fulfillAllNeeds"} },
			{ "T: Delete roof", new DA() {label = "T: Set Deep Resource", action = "addDeepResource"} },
			{ "T: Destroy trees 21x21", new DA() {label = "T: Move selection to...", action = "moveSelection", tool="DebugToolMap"} }
		};
		*/

		//TryPlaceOptionsForStackCount with -1 almost works but I want def.stackLimit >= 2
		[DebugAction(DebugActionCategories.Spawning, "Try place near full stack...", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void FullStack()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs
				.Where(def => DebugThingPlaceHelper.IsDebugSpawnable(def) && def.stackLimit >= 2))
			{
				ThingDef localDef = current;
				list.Add(new DebugMenuOption(localDef.LabelCap, DebugMenuOptionMode.Tool, delegate
				{
					DebugThingPlaceHelper.DebugSpawn(localDef, UI.MouseCell());
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		[DebugAction(DebugActionCategories.General, null, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		public static void DestroyAllSelected()
		{
			foreach (Thing current in Find.Selector.SelectedObjectsListForReading.Where(s => s is Thing).ToList())
			{
				current.Destroy(DestroyMode.Vanish);
			}
		}
		/*
		public static Action<Pawn> healFullAction = delegate (Pawn p)
		{
			
			foreach(Hediff_Injury hediff_Injury in (from x in p.health.hediffSet.GetHediffs<Hediff_Injury>()
					 where x.CanHealNaturally() || x.CanHealFromTending()
					 select x))
			{
				hediff_Injury.Heal(10000f);//probably enough

			}
		};
		public static Action makeRoofByDef = delegate ()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (RoofDef current in DefDatabase<RoofDef>.AllDefs)
			{
				RoofDef localDef = current;
				list.Add(new DebugMenuOption(localDef.LabelCap, DebugMenuOptionMode.Tool, delegate
				{
					foreach(var pos in CellRect.CenteredOn(UI.MouseCell(), 1))
						Find.CurrentMap.roofGrid.SetRoof(pos, localDef);
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		};
		public static Action<Pawn> addSeltoInv = delegate (Pawn p)
		{
			foreach (Thing t in Find.Selector.SelectedObjectsListForReading.
				Where(o => o is Thing t && t.def.EverHaulable).Cast<Thing>().ToList())//ToList to copy since SelectedObjects changed when despawned
			{
				p.inventory.GetDirectlyHeldThings().TryAdd(t.SplitOff(t.stackCount));
			}
		};

		//private void OffsetNeed(NeedDef nd, float offsetPct)
		public static MethodInfo OffsetNeedInfo = AccessTools.Method(typeof(Dialog_DebugActionsMenu), "OffsetNeed");
		public static Action addNeed = delegate ()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();

			list.Add(new DebugMenuOption("All Needs", DebugMenuOptionMode.Tool, delegate
			{
				foreach (NeedDef current in DefDatabase<NeedDef>.AllDefs)
					OffsetNeedInfo.Invoke(null, new object[] { current, -0.2f });
			}));

			foreach (NeedDef current in DefDatabase<NeedDef>.AllDefs)
			{
				NeedDef localDef = current;
				list.Add(new DebugMenuOption(localDef.LabelCap, DebugMenuOptionMode.Tool, delegate
				{
					OffsetNeedInfo.Invoke(null, new object[] { localDef, -0.2f });
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		};
		public static Action fulfillAllNeeds = delegate ()
		{
			foreach(Pawn pawn in Find.CurrentMap.mapPawns.AllPawnsSpawned)
			{
				foreach(Need need in pawn.needs.AllNeeds)
				{
					need.CurLevelPercentage = 1f;
				}
			}
		};
		public static Action addDeepResource = delegate ()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			for (int size = 0; size < 5; size++)
			{
				int localSize = size;
				list.Add(new DebugMenuOption($"{ size * 2 + 1 }x{ size * 2 + 1 }", DebugMenuOptionMode.Action, delegate
				{
					AddDeepResourceSize(localSize);
				}));
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		};

		public static void AddDeepResourceSize(int size)
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			list.Add(new DebugMenuOption($"Deplete deep resource", DebugMenuOptionMode.Tool,
				() => AddDeepResources(size, null)));
			foreach (var current in DefDatabase<ThingDef>.AllDefs.Where(def => def.deepCommonality > 0))
			{
				ThingDef localDef = current;
				list.Add(new DebugMenuOption($"Spawn deep resource: {localDef.LabelCap}", DebugMenuOptionMode.Tool,
					() => AddDeepResources(size, localDef))) ;
			}

			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		public static void AddDeepResources(int size, ThingDef def)
		{
			Map map = Find.CurrentMap;
			foreach(var pos in CellRect.CenteredOn(UI.MouseCell(), size))
				if(pos.InBounds(map))
					Find.CurrentMap.deepResourceGrid.SetAt(pos, def, def?.deepCountPerCell ?? 0);
		}

		public static Action moveSelection = delegate ()
		{
			if (UI.MouseCell().InBounds(Find.CurrentMap))
			{
				IntVec3 pos = UI.MouseCell();
				foreach (Thing current in Find.Selector.SelectedObjectsListForReading.Where(s => s is Thing).ToList())
				{
					current.Position = pos;
					if (current is Pawn pawn)
					{
						pawn.Notify_Teleported(false, true);
					}
				}
			}
		};
		*/
	}
}
