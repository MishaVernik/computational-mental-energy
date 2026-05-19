import { useMemo } from 'react';
import type { EegWindowData, CmeResult, BandPowers } from '../types';

export interface ElectrodeState {
  id: 'TP9' | 'AF7' | 'AF8' | 'TP10';
  intensity: number;
  hue: number;
  quality: number;
  bandPowers: BandPowers;
}

export interface HeadTwinState {
  electrodes: ElectrodeState[];
  pFlow: number;
  cmeIndex: number;
  isFlow: boolean;
  isFresh: boolean;
}

const ELECTRODES: ElectrodeState['id'][] = ['TP9', 'AF7', 'AF8', 'TP10'];
const EMPTY_BANDS: BandPowers = { delta: 0, theta: 0, alpha: 0, beta: 0, gamma: 0 };
const STALE_MS = 8000;

// Maps engagement ratio beta/(alpha+theta) to a hue in degrees:
// low engagement -> blue (220 deg), high engagement -> red/orange (10 deg).
function engagementHue(b: BandPowers): number {
  const denom = b.alpha + b.theta;
  if (denom <= 1e-6) return 220;
  const ratio = b.beta / denom;
  const t = Math.min(Math.max(ratio / 1.5, 0), 1);
  return 220 - 210 * t;
}

function powerIntensity(b: BandPowers): number {
  const sum = b.beta + b.theta + b.alpha;
  return Math.min(Math.log1p(sum * 8) / Math.log(50), 1);
}

export function useElectrodeIntensities(
  latestEeg: EegWindowData | null,
  latestCme: CmeResult | null,
): HeadTwinState {
  return useMemo(() => {
    const channels = latestEeg?.channels ?? {};
    const quality = latestEeg?.channelQuality ?? {};
    const ts = latestEeg ? Date.parse(latestEeg.timestamp) : 0;
    const isFresh = ts > 0 && Date.now() - ts < STALE_MS;

    const electrodes: ElectrodeState[] = ELECTRODES.map(id => {
      const bp = channels[id] ?? EMPTY_BANDS;
      return {
        id,
        intensity: powerIntensity(bp),
        hue: engagementHue(bp),
        quality: quality[id] ?? latestEeg?.quality ?? 0,
        bandPowers: bp,
      };
    });

    return {
      electrodes,
      pFlow: latestCme?.pFlow ?? 0,
      cmeIndex: latestCme?.cmeIndex ?? 0,
      isFlow: latestCme?.isFlow ?? false,
      isFresh,
    };
  }, [latestEeg, latestCme]);
}
