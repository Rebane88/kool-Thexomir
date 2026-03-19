import { describe, it, expect, vi, beforeEach } from 'vitest';

const mockFetch = vi.fn();
vi.mock('@/lib/api-client', () => ({
  apiFetch: (...args: unknown[]) => mockFetch(...args),
}));

import {
  endTurn,
  fetchBuildingTypes,
  placeBuilding,
  fetchArmyTypes,
  trainArmy,
  spinSlotMachine,
  declareAttack,
  selectArmies,
  revealArmies,
  setLineup,
} from './game-api';

function mockOk(body?: unknown): Response {
  return { ok: true, json: () => Promise.resolve(body) } as Response;
}

function mockError(detail: string): Response {
  return {
    ok: false,
    json: () => Promise.resolve({ status: 400, title: 'Bad Request', detail }),
  } as Response;
}

describe('game-api', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // --- endTurn ---
  describe('endTurn', () => {
    it('POSTs to /game/{gameId}/end-turn', async () => {
      mockFetch.mockResolvedValue(mockOk());
      await endTurn('g1');
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/end-turn', {
        method: 'POST',
        body: JSON.stringify({}),
      });
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('Not your turn'));
      await expect(endTurn('g1')).rejects.toThrow('Not your turn');
    });
  });

  // --- fetchBuildingTypes ---
  describe('fetchBuildingTypes', () => {
    it('GETs /game/{gameId}/building-types', async () => {
      const types = [{ id: 'bt1', name: 'Farm' }];
      mockFetch.mockResolvedValue(mockOk(types));
      const result = await fetchBuildingTypes('g1');
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/building-types');
      expect(result).toEqual(types);
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('Game not found'));
      await expect(fetchBuildingTypes('g1')).rejects.toThrow('Game not found');
    });
  });

  // --- placeBuilding ---
  describe('placeBuilding', () => {
    it('POSTs to /game/{gameId}/build', async () => {
      mockFetch.mockResolvedValue(mockOk());
      await placeBuilding('g1', { tileId: 't1', buildingTypeId: 'bt1' });
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/build', {
        method: 'POST',
        body: JSON.stringify({ tileId: 't1', buildingTypeId: 'bt1' }),
      });
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('Insufficient resources'));
      await expect(placeBuilding('g1', { tileId: 't1', buildingTypeId: 'bt1' })).rejects.toThrow(
        'Insufficient resources',
      );
    });
  });

  // --- fetchArmyTypes ---
  describe('fetchArmyTypes', () => {
    it('GETs /game/{gameId}/army-types', async () => {
      const types = [{ id: 'at1', name: 'Swordsman' }];
      mockFetch.mockResolvedValue(mockOk(types));
      const result = await fetchArmyTypes('g1');
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/army-types');
      expect(result).toEqual(types);
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('No army types'));
      await expect(fetchArmyTypes('g1')).rejects.toThrow('No army types');
    });
  });

  // --- trainArmy ---
  describe('trainArmy', () => {
    it('POSTs to /game/{gameId}/train', async () => {
      mockFetch.mockResolvedValue(mockOk());
      await trainArmy('g1', { buildingId: 'b1', armyTypeId: 'at1' });
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/train', {
        method: 'POST',
        body: JSON.stringify({ buildingId: 'b1', armyTypeId: 'at1' }),
      });
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('Building full'));
      await expect(trainArmy('g1', { buildingId: 'b1', armyTypeId: 'at1' })).rejects.toThrow(
        'Building full',
      );
    });
  });

  // --- spinSlotMachine ---
  describe('spinSlotMachine', () => {
    it('POSTs to /game/{gameId}/spin', async () => {
      mockFetch.mockResolvedValue(mockOk());
      await spinSlotMachine('g1');
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/spin', {
        method: 'POST',
        body: JSON.stringify({}),
      });
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('No action points'));
      await expect(spinSlotMachine('g1')).rejects.toThrow('No action points');
    });
  });

  // --- declareAttack ---
  describe('declareAttack', () => {
    it('POSTs to /game/{gameId}/declare-attack', async () => {
      mockFetch.mockResolvedValue(mockOk());
      await declareAttack('g1', { targetTileId: 't1', riskedTileId: 't2' });
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/declare-attack', {
        method: 'POST',
        body: JSON.stringify({ targetTileId: 't1', riskedTileId: 't2' }),
      });
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('Invalid target'));
      await expect(
        declareAttack('g1', { targetTileId: 't1', riskedTileId: 't2' }),
      ).rejects.toThrow('Invalid target');
    });
  });

  // --- selectArmies ---
  describe('selectArmies', () => {
    it('POSTs to /game/{gameId}/select-armies', async () => {
      mockFetch.mockResolvedValue(mockOk());
      await selectArmies('g1', { declaredAttackId: 'da1', armyIds: ['a1', 'a2'] });
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/select-armies', {
        method: 'POST',
        body: JSON.stringify({ declaredAttackId: 'da1', armyIds: ['a1', 'a2'] }),
      });
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('Too many armies'));
      await expect(
        selectArmies('g1', { declaredAttackId: 'da1', armyIds: ['a1'] }),
      ).rejects.toThrow('Too many armies');
    });
  });

  // --- revealArmies ---
  describe('revealArmies', () => {
    it('GETs /game/{gameId}/reveal-armies/{declaredAttackId}', async () => {
      const reveal = { declaredAttackId: 'da1', attackerArmies: [], defenderArmies: [] };
      mockFetch.mockResolvedValue(mockOk(reveal));
      const result = await revealArmies('g1', 'da1');
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/reveal-armies/da1');
      expect(result).toEqual(reveal);
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('Not ready'));
      await expect(revealArmies('g1', 'da1')).rejects.toThrow('Not ready');
    });
  });

  // --- setLineup ---
  describe('setLineup', () => {
    it('POSTs to /game/{gameId}/set-lineup', async () => {
      mockFetch.mockResolvedValue(mockOk());
      await setLineup('g1', { declaredAttackId: 'da1', armyIdsInOrder: ['a1', 'a2'] });
      expect(mockFetch).toHaveBeenCalledWith('/game/g1/set-lineup', {
        method: 'POST',
        body: JSON.stringify({ declaredAttackId: 'da1', armyIdsInOrder: ['a1', 'a2'] }),
      });
    });

    it('throws on error response', async () => {
      mockFetch.mockResolvedValue(mockError('Wrong armies'));
      await expect(
        setLineup('g1', { declaredAttackId: 'da1', armyIdsInOrder: ['a1'] }),
      ).rejects.toThrow('Wrong armies');
    });
  });
});
