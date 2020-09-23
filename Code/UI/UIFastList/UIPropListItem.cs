﻿using System.Collections.Generic;


namespace BOB
{
	/// <summary>
	/// Data record for UI prop list line items.
	/// </summary>
	public class PropListItem
	{
		// Original prefab.
		public PrefabInfo originalPrefab;

		// Current replacement (if any).
		public PrefabInfo currentPrefab;

		// Current global replacement (if any).
		public PrefabInfo globalPrefab;

		// Single index.
		public int index;

		// Prop angle.
		public float angle;

		// Current probability.
		public int probability;

		// Original probability.
		public int originalProb;

		// List of indexes.
		public List<int> indexes = new List<int>();
	}


	/// <summary>
	/// Data record for UI prop list item for network props.
	/// </summary>
	public class NetPropListItem : PropListItem
    {
		// Lane reference.
		public int lane;

		// Lane list.
		public List<int> lanes = new List<int>();
    }
}