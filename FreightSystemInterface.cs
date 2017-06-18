using System.Collections.Generic;
using System;
public interface FreightSystemInterface
{
	/// <summary>
	/// List of all items that the interface has available to provide to the freight system
	/// </summary>
	List<FreightListing> FreightOfferings
	{
		get;
	}

	/// <summary>
	/// List of all items that the interface wants delivered to it by the freight system
	/// </summary>
	List<FreightListing> FreightRequests
	{
		get;
	}

	/// <summary>
	/// The freight system calls this when it has an item to offer to the interface 
	/// </summary>
	/// <param name="item">The item being offered to the interface</param>
	/// <returns>True if the item is accepted</returns>
	bool ReceiveFreight(ItemBase item);

	/// <summary>
	/// The freight system calls this when requesting an item from the interface
	/// </summary>
	/// <param name="item">The item the freight system wants the interface to provide</param>
	/// <returns>True if the interface successfully provides the item</returns>
	bool ProvideFreight(ItemBase item = null);

	/// <summary>
	/// Determines whether this instance is full.
	/// </summary>
	/// <returns><c>true</c> if this instance is full; otherwise, <c>false</c>.</returns>
	bool IsFull ();
}

/// <summary>
/// Class representing a freight item and offered/requested quantity
/// </summary>
public class FreightListing
{
	public ItemBase Item { get; }
	public int Quantity { get; set; }

	public FreightListing(ItemBase item, int quantity)
	{
		Item = item;
		Quantity = quantity;
	}

	//Some operator and comparison overrides to make them directly comparable to ItemBase
}