﻿using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Records original prop data.
	/// </summary>
	public class NetPropReference
	{
		public NetInfo network;
		public int laneIndex;
		public int propIndex;
		public float angle;
		public Vector3 postion;
	}


	/// <summary>
	/// Static class to manage all-building prop and tree replacements.
	/// </summary>
	internal static class AllNetworkReplacement
	{
		// Master dictionary of replaced prop references.
		internal static Dictionary<PrefabInfo, BOBAllNetworkElement> replacements;



		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal static void Setup()
		{
			replacements = new Dictionary<PrefabInfo, BOBAllNetworkElement>();
		}

		
		/// <summary>
		/// Reverts all active all-network replacements and re-initialises the master dictionaries.
		/// </summary>
		internal static void RevertAll()
		{
			// Iterate through each entry in the master prop dictionary.
			foreach (PrefabInfo prop in replacements.Keys)
			{
				// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
				Revert(prop, removeEntries: false);
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts an all-network replacement.
		/// </summary>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="replacement">Applied replacment tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire network record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal static void Revert(PrefabInfo target, bool removeEntries = true)
		{
			// List of reverted entries.
			List<NetInfo> revertedList = new List<NetInfo>();

			// Iterate through each entry in our dictionary.
			foreach (NetPropReference propReference in replacements[target].references)
			{
				// Revert entry.
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalProp = (PropInfo)target;
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_angle = propReference.angle;
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_position = propReference.postion;
			}

			// Remove entry from dictionary, if we're doing so.
			if (removeEntries)
            {
				replacements.Remove(target);
            }
		}


		/// <summary>
		/// Removes an entry from the master dictionary of all-network replacements currently applied to networks.
		/// </summary>
		/// <param name="netPrefab">Network prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal static void RemoveEntry(NetInfo netPrefab, PrefabInfo target, int laneIndex, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			if (replacements.ContainsKey(target))
			{
				// Yes - iterate through each recorded prop reference.
				for (int i = 0; i < replacements[target].references.Count; ++i)
				{
					// Look for a lane and index match.
					if (replacements[target].references[i].laneIndex == laneIndex && replacements[target].references[i].propIndex == propIndex)
                    {
						// Got a match!  Remove this reference and return.
						replacements[target].references.Remove(replacements[target].references[i]);
						return;
                    }
                }
			}
		}


		/// <summary>
		/// Applies a new (or updated) all-network replacement.
		/// </summary>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		internal static void Apply(PrefabInfo target, PrefabInfo replacement, float angle, float offsetX, float offsetY, float offsetZ)
		{
			// Set our initial targeted prefab to the provided target. 
			PrefabInfo targetedPrefab = target;
			
			// Make sure that target and replacement are the same type before doing anything.
			if (target == null || replacement == null || (target is TreeInfo && !(replacement is TreeInfo)) || (target is PropInfo) && !(replacement is PropInfo))
			{
				return;
			}

			// Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
			if (replacements.ContainsKey(target))
            {
				Revert(target, true);
            }

			// Create new dictionary entry if none already exists.
			if (!replacements.ContainsKey(target))
			{
				replacements.Add(target, new BOBAllNetworkElement());
			}

			// Add/replace dictionary replacement data.
			replacements[target].references = new List<NetPropReference>();
			replacements[target].targetInfo = target;
			replacements[target].target = target.name;
			replacements[target].angle = angle;
			replacements[target].offsetX = offsetX;
			replacements[target].offsetY = offsetY;
			replacements[target].offsetZ = offsetZ;

			// Record replacement prop.
			replacements[target].replacementInfo = replacement;
			replacements[target].replacement = replacement.name;

			// Iterate through each loaded network and record props to be replaced.
			for (int i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
			{
				// Get local reference.
				NetInfo network = PrefabCollection<NetInfo>.GetLoaded((uint)i);

				// Skip any netorks without lanes.
				if (network.m_lanes == null)
				{
					continue;
				}

				// Iterate through each lane.
				for (int laneIndex = 0; laneIndex < network.m_lanes.Length; ++laneIndex)
				{
					// If no props in this lane, skip it and go to the next one.
					if (network.m_lanes[laneIndex].m_laneProps?.m_props == null)
					{
						continue;
					}

					// Iterate through each prop in lane.
					for (int propIndex = 0; propIndex < network.m_lanes[laneIndex].m_laneProps.m_props.Length; ++propIndex)
					{
						// Check for any currently active network replacement.
						if (NetworkReplacement.GetOriginal(network, laneIndex, propIndex) != null)
						{
							// Active network replacement; skip this one.
							Debugging.Message("Found active replacement in network " + network.name + " at lane " + laneIndex + " and position " + propIndex + "; skipping");
							continue;
						}

						// See if this prop matches our replacement.
						if (network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalProp != null && network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalProp == targetedPrefab)
						{
							// Match!  Add reference data to the list.
							replacements[target].references.Add(new NetPropReference
							{
								network = network,
								laneIndex = laneIndex,
								propIndex = propIndex,
								angle = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle,
								postion = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position
							});

							Debugging.Message("Adding to dictionary " + network.name + " at lane " + laneIndex + " and position " + propIndex);
						}
					}
				}
			}

			// Now, iterate through each entry found and apply the replacement to each one.
			foreach (NetPropReference propReference in replacements[target].references)
			{
				ReplaceProp(replacements[target], propReference);
			}
		}


		/// <summary>
		/// Checks if there's a currently active all-network replacement applied to the given network prop index.
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Original prefab if a all-building replacement is currently applied, null if no all-building replacement is currently applied</returns>
		internal static PrefabInfo ActiveReplacement(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Iterate through each entry in master dictionary.
			foreach (var target in replacements.Keys)
			{

				BOBAllNetworkElement reference = replacements[target];
				// Iterate through each network in this entry.
				foreach (NetPropReference propRef in reference.references)
				{
					// Check for a network, lane, and prop index match.
					if (propRef.network == netPrefab && propRef.laneIndex == laneIndex && propRef.propIndex == propIndex)
					{
						// Match!  Return the original prefab.
						return target;
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Restores a all-network replacement, if any (e.g. after a network replacement has been reverted).
		/// </summary>
		/// <param name="netPrefab">Network prefab</param>
		/// <param name="target">Target prop info</param>
		/// <param name="laneIndex">Lane index</param>
		/// <param name="propIndex">Prop index</param>
		internal static void Restore(NetInfo netPrefab, PrefabInfo target, int laneIndex, int propIndex)
		{
			// Check to see if we have an entry for this prefab.
			if (replacements.ContainsKey(target))
			{
				// Yes - add reference data to the list.
				NetPropReference newReference = new NetPropReference
				{
					network = netPrefab,
					laneIndex = laneIndex,
					propIndex = propIndex,
					angle = netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle,
					postion = netPrefab.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position
				};

				replacements[target].references.Add(newReference);

				// Apply replacement.
				ReplaceProp(replacements[target], newReference);
			}
		}


		/// <summary>
		/// Replaces a prop using an all-network replacement.
		/// </summary>
		/// <param name="netElement">All-network replacement element to apply</param>
		/// <param name="propReference">Individual prop reference to apply to</param>
		private static void ReplaceProp(BOBAllNetworkElement netElement, NetPropReference propReference)
        {
			// Convert offset to Vector3.
			Vector3 offset = new Vector3
			{
				x = netElement.offsetX,
				y = netElement.offsetY,
				z = netElement.offsetZ
			};

			// Apply replacement.
			propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalProp = (PropInfo)netElement.replacementInfo;
			propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_angle = propReference.angle + netElement.angle;
			propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_position = propReference.postion + offset;
		}
	}
}
