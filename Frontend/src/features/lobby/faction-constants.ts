import type { ComponentType, SVGProps } from 'react';
import { SwordIcon, ManaIcon, CoinIcon, ShieldIcon } from '@/assets/icons';

export interface FactionMeta {
  name: string;
  color: string;          // Tailwind bg class for accent
  borderColor: string;    // Tailwind border-l class for player cards
  bonusSummary: string;   // Short display text
  Icon: ComponentType<SVGProps<SVGSVGElement> & { size?: number }>;
}

export const FACTION_METADATA: Record<string, FactionMeta> = {
  'eeeeeeee-0001-0000-0000-000000000001': {
    name: 'Iron Throne',
    color: 'bg-red-800',
    borderColor: 'border-l-red-800',
    bonusSummary: 'ATK +15% \u00b7 Chip +50%',
    Icon: SwordIcon,
  },
  'eeeeeeee-0001-0000-0000-000000000002': {
    name: 'Mage Council',
    color: 'bg-indigo-800',
    borderColor: 'border-l-indigo-800',
    bonusSummary: 'Initiative +20% \u00b7 +1 AP',
    Icon: ManaIcon,
  },
  'eeeeeeee-0001-0000-0000-000000000003': {
    name: 'Merchant Republic',
    color: 'bg-amber-800',
    borderColor: 'border-l-amber-800',
    bonusSummary: 'Buildings -20% \u00b7 Train -15%',
    Icon: CoinIcon,
  },
  'eeeeeeee-0001-0000-0000-000000000004': {
    name: 'Forest Elves',
    color: 'bg-green-800',
    borderColor: 'border-l-green-800',
    bonusSummary: 'HP +15% \u00b7 Heal +50%',
    Icon: ShieldIcon,
  },
};
