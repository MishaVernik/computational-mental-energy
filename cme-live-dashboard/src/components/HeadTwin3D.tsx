import React, { Suspense, useMemo, useRef } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { OrbitControls, Html } from '@react-three/drei';
import * as THREE from 'three';
import type { CmeResult, EegWindowData } from '../types';
import { useElectrodeIntensities, type ElectrodeState } from '../hooks/useElectrodeIntensities';

interface Props {
  latestEeg: EegWindowData | null;
  latestCme: CmeResult | null;
}

// Position + outward surface normal per electrode. Coordinates assume a head
// ellipsoid centered at origin with the +Z axis pointing forward (face) and
// +Y up. AF7/AF8 sit on the forehead, TP9/TP10 behind the ears.
interface ElectrodeAnchor {
  pos: [number, number, number];
  normal: [number, number, number];
}

const ELECTRODE_ANCHORS: Record<ElectrodeState['id'], ElectrodeAnchor> = {
  AF7:  { pos: [-0.30, 0.60, 0.72], normal: [-0.30, 0.45, 0.85] },
  AF8:  { pos: [ 0.30, 0.60, 0.72], normal: [ 0.30, 0.45, 0.85] },
  TP9:  { pos: [-0.78, 0.05, -0.10], normal: [-1.00, 0.10, -0.05] },
  TP10: { pos: [ 0.78, 0.05, -0.10], normal: [ 1.00, 0.10, -0.05] },
};

function hslString(h: number, s: number, l: number): string {
  return `hsl(${h.toFixed(0)}, ${(s * 100).toFixed(0)}%, ${(l * 100).toFixed(0)}%)`;
}

function quatFromNormal(normal: [number, number, number]): THREE.Quaternion {
  const target = new THREE.Vector3(...normal).normalize();
  return new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 1, 0), target);
}

function Electrode({ state, anchor }: { state: ElectrodeState; anchor: ElectrodeAnchor }) {
  const padRef = useRef<THREE.Mesh>(null);
  const quat = useMemo(() => quatFromNormal(anchor.normal), [anchor.normal]);
  const color = hslString(state.hue, 0.85, 0.4 + 0.3 * state.intensity);
  const emissive = hslString(state.hue, 1.0, 0.25 + 0.4 * state.intensity);

  useFrame((_, dt) => {
    if (!padRef.current) return;
    // Subtle "breathing" along the surface normal: 1 - 1.15× height as intensity rises.
    const target = 1 + 0.15 * state.intensity;
    padRef.current.scale.y = THREE.MathUtils.lerp(padRef.current.scale.y, target, Math.min(dt * 4, 1));
  });

  return (
    <group position={anchor.pos} quaternion={quat}>
      {/* Flat metallic mount (the part visible against the band). */}
      <mesh position={[0, 0.005, 0]} scale={[1, 1, 1]}>
        <cylinderGeometry args={[0.085, 0.085, 0.025, 32]} />
        <meshStandardMaterial color="#15161c" metalness={0.7} roughness={0.35} />
      </mesh>
      {/* Active sensor pad: colors track engagement, height pulses with intensity. */}
      <mesh ref={padRef} position={[0, 0.025, 0]}>
        <cylinderGeometry args={[0.062, 0.072, 0.022, 32]} />
        <meshStandardMaterial
          color={color}
          emissive={emissive}
          emissiveIntensity={0.4 + state.intensity * 1.2}
          metalness={0.15}
          roughness={0.35}
        />
      </mesh>
      <Html
        distanceFactor={8}
        position={[0, 0.12, 0]}
        center
        zIndexRange={[80, 0]}
        occlude={false}
      >
        <div style={{
          color: '#eee', fontSize: 9, fontFamily: 'monospace',
          background: 'rgba(0,0,0,0.55)', padding: '1px 5px', borderRadius: 3,
          whiteSpace: 'nowrap', pointerEvents: 'none', userSelect: 'none',
        }}>
          {state.id}
        </div>
      </Html>
    </group>
  );
}

function Head() {
  return (
    <group>
      {/* Cranium: ellipsoid (narrower side-to-side than front-to-back) */}
      <mesh scale={[0.85, 1.0, 0.95]}>
        <sphereGeometry args={[0.95, 48, 48]} />
        <meshStandardMaterial color="#e8c7a0" roughness={0.75} metalness={0.05} />
      </mesh>
      {/* Lower face / jaw */}
      <mesh position={[0, -0.55, 0.05]} scale={[0.72, 0.45, 0.78]}>
        <sphereGeometry args={[0.85, 32, 32]} />
        <meshStandardMaterial color="#dcb48c" roughness={0.78} metalness={0.05} />
      </mesh>
      {/* Chin point */}
      <mesh position={[0, -0.85, 0.18]} scale={[0.45, 0.3, 0.5]}>
        <sphereGeometry args={[0.55, 24, 24]} />
        <meshStandardMaterial color="#d4ac84" roughness={0.78} metalness={0.05} />
      </mesh>
      {/* Nose */}
      <mesh position={[0, 0.0, 0.85]} scale={[0.35, 0.55, 0.4]}>
        <sphereGeometry args={[0.3, 20, 20]} />
        <meshStandardMaterial color="#d4ac84" roughness={0.78} metalness={0.05} />
      </mesh>
      {/* Eyes (so the front is unambiguous) */}
      <mesh position={[-0.22, 0.18, 0.78]}>
        <sphereGeometry args={[0.08, 24, 24]} />
        <meshStandardMaterial color="#ffffff" roughness={0.3} metalness={0.1} />
      </mesh>
      <mesh position={[0.22, 0.18, 0.78]}>
        <sphereGeometry args={[0.08, 24, 24]} />
        <meshStandardMaterial color="#ffffff" roughness={0.3} metalness={0.1} />
      </mesh>
      <mesh position={[-0.22, 0.18, 0.85]}>
        <sphereGeometry args={[0.04, 16, 16]} />
        <meshStandardMaterial color="#2a2a2a" roughness={0.4} metalness={0.1} />
      </mesh>
      <mesh position={[0.22, 0.18, 0.85]}>
        <sphereGeometry args={[0.04, 16, 16]} />
        <meshStandardMaterial color="#2a2a2a" roughness={0.4} metalness={0.1} />
      </mesh>
      {/* Ears */}
      <mesh position={[-0.82, 0.0, -0.05]} scale={[0.12, 0.3, 0.22]}>
        <sphereGeometry args={[0.5, 20, 20]} />
        <meshStandardMaterial color="#d4ac84" roughness={0.78} metalness={0.05} />
      </mesh>
      <mesh position={[0.82, 0.0, -0.05]} scale={[0.12, 0.3, 0.22]}>
        <sphereGeometry args={[0.5, 20, 20]} />
        <meshStandardMaterial color="#d4ac84" roughness={0.78} metalness={0.05} />
      </mesh>
      {/* Neck */}
      <mesh position={[0, -1.15, -0.05]}>
        <cylinderGeometry args={[0.32, 0.40, 0.45, 24]} />
        <meshStandardMaterial color="#c8a47a" roughness={0.8} metalness={0.05} />
      </mesh>
      {/* Muse Athena headband: torus around the forehead. THREE.TorusGeometry lives in
          the XY plane (axis along +Z), so we rotate by π/2 about X to lay it flat in
          the XZ plane (horizontal), then subtract a small forward tilt (~6°). */}
      <group position={[0, 0.38, -0.04]} rotation={[Math.PI / 2 - 0.10, 0, 0]}>
        <mesh>
          <torusGeometry args={[0.92, 0.035, 16, 96]} />
          <meshStandardMaterial color="#101015" metalness={0.55} roughness={0.4} />
        </mesh>
        {/* Tiny Muse logo bump on the front center of the band (rotated so it sticks straight forward) */}
        <mesh position={[0, 0, 0.92]} scale={[0.1, 0.05, 0.035]}>
          <boxGeometry args={[1, 1, 1]} />
          <meshStandardMaterial color="#64B5F6" emissive="#64B5F6" emissiveIntensity={0.4} />
        </mesh>
      </group>
    </group>
  );
}

function FlowHalo({ pFlow, isFlow }: { pFlow: number; isFlow: boolean }) {
  const ref = useRef<THREE.Mesh>(null);
  const targetScale = 1.35 + pFlow * 0.45;
  const hue = isFlow ? 130 : 200;

  useFrame((_, dt) => {
    if (!ref.current) return;
    const k = Math.min(dt * 3, 1);
    ref.current.scale.lerp(new THREE.Vector3(targetScale, targetScale, targetScale), k);
    const mat = ref.current.material as THREE.MeshBasicMaterial;
    mat.opacity = 0.06 + 0.18 * pFlow;
  });

  return (
    <mesh ref={ref}>
      <sphereGeometry args={[1.0, 32, 32]} />
      <meshBasicMaterial
        color={hslString(hue, 0.9, 0.55)}
        transparent
        opacity={0.12}
        side={THREE.BackSide}
      />
    </mesh>
  );
}

function Scene({ state }: { state: ReturnType<typeof useElectrodeIntensities> }) {
  return (
    <>
      <ambientLight intensity={0.5} />
      <directionalLight position={[3, 4, 5]} intensity={0.9} />
      <directionalLight position={[-3, 2, -4]} intensity={0.35} color="#8090ff" />
      <Head />
      {state.electrodes.map(e => (
        <Electrode key={e.id} state={e} anchor={ELECTRODE_ANCHORS[e.id]} />
      ))}
      <FlowHalo pFlow={state.pFlow} isFlow={state.isFlow} />
    </>
  );
}

export const HeadTwin3D: React.FC<Props> = ({ latestEeg, latestCme }) => {
  const state = useElectrodeIntensities(latestEeg, latestCme);
  const status = state.isFresh ? 'live' : 'idle';

  return (
    <div style={{
      background: '#1a1a2e', borderRadius: 12, padding: 16, border: '1px solid #333',
      position: 'relative',
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <div style={{ color: '#aaa', fontSize: 13 }}>3D Twin · Head + Muse Athena</div>
        <div style={{ display: 'flex', gap: 12, fontSize: 11, color: '#888', fontFamily: 'monospace' }}>
          <span>pFlow {state.pFlow.toFixed(3)}</span>
          <span>CMEi {state.cmeIndex.toFixed(1)}</span>
          <span style={{ color: status === 'live' ? '#4caf50' : '#777' }}>
            {status === 'live' ? '● live' : '○ idle'}
          </span>
        </div>
      </div>

      <div style={{ width: '100%', height: 360, borderRadius: 8, overflow: 'hidden', background: '#0a0a14' }}>
        <Canvas camera={{ position: [2.2, 0.4, 2.8], fov: 38 }} dpr={[1, 2]}>
          <Suspense fallback={null}>
            <Scene state={state} />
            <OrbitControls
              enablePan={false}
              minDistance={2.2}
              maxDistance={6}
              target={[0, 0.1, 0]}
              autoRotate={!state.isFresh}
              autoRotateSpeed={0.6}
            />
          </Suspense>
        </Canvas>
      </div>

      <div style={{
        marginTop: 8, color: '#777', fontSize: 10, fontFamily: 'monospace',
        display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 4,
      }}>
        {state.electrodes.map(e => (
          <div key={e.id} style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span style={{ color: '#aaa' }}>{e.id}</span>
            <span>{(e.intensity * 100).toFixed(0)}%</span>
          </div>
        ))}
      </div>
    </div>
  );
};
