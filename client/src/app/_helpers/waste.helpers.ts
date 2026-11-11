import { WasteType } from '../_models/waste-bin.model';

/** Font Awesome icon for each bin type. */
export function wasteTypeIcon(type: WasteType): string {
  switch (type) {
    case 'Recycling': return 'fas fa-recycle';
    case 'Glass': return 'fas fa-wine-bottle';
    case 'Organic': return 'fas fa-leaf';
    case 'Paper': return 'fas fa-newspaper';
    default: return 'fas fa-trash';
  }
}

/** Map marker colour for a fill level: green below 50%, orange below 80%, red from 80%. */
export function fillColor(percent: number): string {
  if (percent >= 80) return '#dc3545';
  if (percent >= 50) return '#fd7e14';
  return '#198754';
}

/** Bootstrap background classes for a fill level. The amber badge needs dark text. */
export function fillBadgeClass(percent: number): string {
  if (percent >= 80) return 'bg-danger';
  if (percent >= 50) return 'bg-warning text-on-warning';
  return 'bg-success';
}
