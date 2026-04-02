import type { ComponentType, SVGProps } from 'react';
import { SwordIcon, ManaIcon, CoinIcon, ShieldIcon } from '@/assets/icons';
import factionIronThronePng from '@/assets/images/faction-iron-throne.png';
import factionMageCouncilPng from '@/assets/images/faction-mage-council.png';
import factionMerchantRepublicPng from '@/assets/images/faction-merchant-republic.png';
import factionForestElvesPng from '@/assets/images/faction-forest-elves.png';

export interface FactionVisuals {
  color: string;          // Tailwind bg class for accent
  borderColor: string;    // Tailwind border-l class for player cards
  hexColor: string;       // Raw hex color for canvas use
  Icon: ComponentType<SVGProps<SVGSVGElement> & { size?: number }>;
  crestImage: string;
}

export const FACTION_VISUALS: Record<string, FactionVisuals> = {
  'eeeeeeee-0001-0000-0000-000000000001': {
    color: 'bg-red-800',
    borderColor: 'border-l-red-800',
    hexColor: '#991b1b',
    Icon: SwordIcon,
    crestImage: factionIronThronePng,
  },
  'eeeeeeee-0001-0000-0000-000000000002': {
    color: 'bg-indigo-800',
    borderColor: 'border-l-indigo-800',
    hexColor: '#3730a3',
    Icon: ManaIcon,
    crestImage: factionMageCouncilPng,
  },
  'eeeeeeee-0001-0000-0000-000000000003': {
    color: 'bg-amber-800',
    borderColor: 'border-l-amber-800',
    hexColor: '#92400e',
    Icon: CoinIcon,
    crestImage: factionMerchantRepublicPng,
  },
  'eeeeeeee-0001-0000-0000-000000000004': {
    color: 'bg-green-800',
    borderColor: 'border-l-green-800',
    hexColor: '#166534',
    Icon: ShieldIcon,
    crestImage: factionForestElvesPng,
  },
};
