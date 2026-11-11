import { ParkingType } from '../_models/parking.model';

/** Font Awesome icon for each parking type. */
export function parkingTypeIcon(type: ParkingType): string {
  switch (type) {
    case 'Disabled': return 'fas fa-wheelchair';
    case 'EV': return 'fas fa-charging-station';
    case 'Lot': return 'fas fa-square-parking';
    default: return 'fas fa-road';
  }
}
