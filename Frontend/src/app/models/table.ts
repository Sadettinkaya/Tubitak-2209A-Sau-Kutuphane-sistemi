export interface Table {
  id: number;
  tableNumber: string;
  floorId: number;
  isReserved: boolean;
  isAvailable?: boolean; // Optional property for UI
}
