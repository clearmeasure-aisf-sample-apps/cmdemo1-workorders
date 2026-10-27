# Note 26

A changed work-order room number cannot exceed 50 characters. WorkOrder.RoomNumberEntryMaxLength is 50. SaveDraftCommandValidator applies that limit only when the room number differs from PreviousRoomNumber, and WorkOrderManageModel reports "Room number cannot exceed 50 characters." for the same change. Persistence accepts up to 900 characters (WorkOrder.RoomNumberMaxLength), mapped with HasMaxLength on the room-number column.
