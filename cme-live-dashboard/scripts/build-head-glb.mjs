// Procedural GLB generator for the Digital Twin head + Muse Athena headband.
// Output: public/head_with_muse.glb with named nodes TP9, AF7, AF8, TP10
// (matches Muse Athena 4-channel layout) for both Three.js and Azure DT 3D Scenes Studio.

import { Document, NodeIO } from '@gltf-transform/core';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const OUT = resolve(__dirname, '..', 'public', 'head_with_muse.glb');

function uvSphere(radius, latBands = 16, lonBands = 24) {
  const positions = [], normals = [], indices = [];
  for (let lat = 0; lat <= latBands; lat++) {
    const theta = (lat * Math.PI) / latBands;
    const sinT = Math.sin(theta), cosT = Math.cos(theta);
    for (let lon = 0; lon <= lonBands; lon++) {
      const phi = (lon * 2 * Math.PI) / lonBands;
      const x = Math.cos(phi) * sinT;
      const y = cosT;
      const z = Math.sin(phi) * sinT;
      positions.push(x * radius, y * radius, z * radius);
      normals.push(x, y, z);
    }
  }
  for (let lat = 0; lat < latBands; lat++) {
    for (let lon = 0; lon < lonBands; lon++) {
      const a = lat * (lonBands + 1) + lon;
      const b = a + lonBands + 1;
      indices.push(a, b, a + 1, b, b + 1, a + 1);
    }
  }
  return { positions: new Float32Array(positions), normals: new Float32Array(normals), indices: new Uint16Array(indices) };
}

function cylinder(rTop, rBot, height, segments = 24) {
  const positions = [], normals = [], indices = [];
  const half = height / 2;
  for (let i = 0; i <= segments; i++) {
    const t = (i / segments) * Math.PI * 2;
    const cx = Math.cos(t), cz = Math.sin(t);
    positions.push(cx * rTop, half, cz * rTop);
    normals.push(cx, 0, cz);
    positions.push(cx * rBot, -half, cz * rBot);
    normals.push(cx, 0, cz);
  }
  for (let i = 0; i < segments; i++) {
    const a = i * 2, b = i * 2 + 1, c = i * 2 + 2, d = i * 2 + 3;
    indices.push(a, b, c, c, b, d);
  }
  // Caps
  const topCenter = positions.length / 3;
  positions.push(0, half, 0); normals.push(0, 1, 0);
  const botCenter = positions.length / 3;
  positions.push(0, -half, 0); normals.push(0, -1, 0);
  for (let i = 0; i < segments; i++) {
    const a = i * 2, c = ((i + 1) % (segments + 1)) * 2;
    indices.push(topCenter, c, a);
    indices.push(botCenter, a + 1, c + 1);
  }
  return { positions: new Float32Array(positions), normals: new Float32Array(normals), indices: new Uint16Array(indices) };
}

function torus(R, r, rad = 24, tub = 16) {
  const positions = [], normals = [], indices = [];
  for (let i = 0; i <= rad; i++) {
    const u = (i / rad) * Math.PI * 2;
    const cu = Math.cos(u), su = Math.sin(u);
    for (let j = 0; j <= tub; j++) {
      const v = (j / tub) * Math.PI * 2;
      const cv = Math.cos(v), sv = Math.sin(v);
      const x = (R + r * cv) * cu;
      const y = r * sv;
      const z = (R + r * cv) * su;
      positions.push(x, y, z);
      normals.push(cv * cu, sv, cv * su);
    }
  }
  for (let i = 0; i < rad; i++) {
    for (let j = 0; j < tub; j++) {
      const a = (tub + 1) * i + j;
      const b = (tub + 1) * (i + 1) + j;
      indices.push(a, b, a + 1, b, b + 1, a + 1);
    }
  }
  return { positions: new Float32Array(positions), normals: new Float32Array(normals), indices: new Uint16Array(indices) };
}

function quatFromUpToVec(vec) {
  // Rotation that maps (0,1,0) onto vec (assumes vec is unit).
  const ux = 0, uy = 1, uz = 0;
  const [vx, vy, vz] = vec;
  const dot = ux * vx + uy * vy + uz * vz;
  if (dot > 0.99999) return [0, 0, 0, 1];
  if (dot < -0.99999) return [1, 0, 0, 0];
  const cx = uy * vz - uz * vy;
  const cy = uz * vx - ux * vz;
  const cz = ux * vy - uy * vx;
  const s = Math.sqrt((1 + dot) * 2);
  const invs = 1 / s;
  return [cx * invs, cy * invs, cz * invs, s * 0.5];
}

function normalize(v) {
  const l = Math.hypot(v[0], v[1], v[2]);
  return [v[0] / l, v[1] / l, v[2] / l];
}

function addPrimitive(doc, name, geom, color, metallic = 0.05, roughness = 0.75) {
  const buf = doc.getRoot().listBuffers()[0];
  const pos = doc.createAccessor(`${name}_pos`).setType('VEC3').setArray(geom.positions).setBuffer(buf);
  const nor = doc.createAccessor(`${name}_nor`).setType('VEC3').setArray(geom.normals).setBuffer(buf);
  const idx = doc.createAccessor(`${name}_idx`).setType('SCALAR').setArray(geom.indices).setBuffer(buf);
  const mat = doc.createMaterial(`${name}_mat`)
    .setBaseColorFactor(color)
    .setMetallicFactor(metallic)
    .setRoughnessFactor(roughness);
  const prim = doc.createPrimitive()
    .setAttribute('POSITION', pos)
    .setAttribute('NORMAL', nor)
    .setIndices(idx)
    .setMaterial(mat);
  const mesh = doc.createMesh(`${name}_mesh`).addPrimitive(prim);
  return doc.createNode(name).setMesh(mesh);
}

const doc = new Document();
doc.createBuffer();
const scene = doc.createScene('HeadTwinScene');

const SKIN = [0.91, 0.78, 0.63, 1.0];
const SKIN_DARK = [0.83, 0.67, 0.52, 1.0];

const cranium = addPrimitive(doc, 'Head', uvSphere(0.95, 32, 48), SKIN);
cranium.setScale([0.85, 1.0, 0.95]);
scene.addChild(cranium);

const jaw = addPrimitive(doc, 'Jaw', uvSphere(0.85, 24, 32), SKIN_DARK);
jaw.setTranslation([0, -0.55, 0.05]).setScale([0.72, 0.45, 0.78]);
scene.addChild(jaw);

const chin = addPrimitive(doc, 'Chin', uvSphere(0.55, 20, 24), SKIN_DARK);
chin.setTranslation([0, -0.85, 0.18]).setScale([0.45, 0.3, 0.5]);
scene.addChild(chin);

const nose = addPrimitive(doc, 'Nose', uvSphere(0.3, 16, 20), SKIN_DARK);
nose.setTranslation([0, 0.0, 0.85]).setScale([0.35, 0.55, 0.4]);
scene.addChild(nose);

// Eyes (whites + pupils), matches the local dashboard so the face is unambiguous.
const EYE_WHITE = [1.0, 1.0, 1.0, 1.0];
const PUPIL     = [0.16, 0.16, 0.16, 1.0];
for (const [name, x] of [['EyeL', -0.22], ['EyeR', 0.22]]) {
  const white = addPrimitive(doc, name, uvSphere(0.08, 20, 24), EYE_WHITE, 0.1, 0.3);
  white.setTranslation([x, 0.18, 0.78]);
  scene.addChild(white);
  const pupil = addPrimitive(doc, `${name}_Pupil`, uvSphere(0.04, 14, 18), PUPIL, 0.1, 0.4);
  pupil.setTranslation([x, 0.18, 0.85]);
  scene.addChild(pupil);
}

const earL = addPrimitive(doc, 'EarL', uvSphere(0.5, 16, 20), SKIN_DARK);
earL.setTranslation([-0.82, 0.0, -0.05]).setScale([0.12, 0.3, 0.22]);
scene.addChild(earL);

const earR = addPrimitive(doc, 'EarR', uvSphere(0.5, 16, 20), SKIN_DARK);
earR.setTranslation([0.82, 0.0, -0.05]).setScale([0.12, 0.3, 0.22]);
scene.addChild(earR);

const neck = addPrimitive(doc, 'Neck', cylinder(0.32, 0.40, 0.45, 24), [0.78, 0.64, 0.48, 1.0]);
neck.setTranslation([0, -1.15, -0.05]);
scene.addChild(neck);

// Headband: a horizontal torus that wraps around the forehead, like a real Muse.
// torus() outputs the ring in the XZ plane (axis along +Y) - already horizontal at rot=0.
// We apply a small forward tilt about X so the front edge dips ~6° (Muse silhouette).
// Position: y=0.38 puts the band on the brow line (eyes are at y=0.18, top of cranium at y≈0.95).
const HB_TILT = 0.10;
const sinH = Math.sin(HB_TILT / 2), cosH = Math.cos(HB_TILT / 2);
const HB_R = 0.92;
const HB_Y = 0.38;
const HB_Z = -0.04;
const headband = addPrimitive(doc, 'MuseHeadband', torus(HB_R, 0.035, 96, 16), [0.06, 0.06, 0.08, 1.0], 0.55, 0.4);
headband.setTranslation([0, HB_Y, HB_Z]).setRotation([sinH, 0, 0, cosH]);
scene.addChild(headband);

// Glowing Muse logo bump on the front of the band (rides with the same tilt).
function boxGeom(sx, sy, sz) {
  const x = sx / 2, y = sy / 2, z = sz / 2;
  const positions = new Float32Array([
    -x,-y, z,  x,-y, z,  x, y, z, -x, y, z,  // +Z
    -x,-y,-z, -x, y,-z,  x, y,-z,  x,-y,-z,  // -Z
    -x, y,-z, -x, y, z,  x, y, z,  x, y,-z,  // +Y
    -x,-y,-z,  x,-y,-z,  x,-y, z, -x,-y, z,  // -Y
     x,-y,-z,  x, y,-z,  x, y, z,  x,-y, z,  // +X
    -x,-y,-z, -x,-y, z, -x, y, z, -x, y,-z,  // -X
  ]);
  const normals = new Float32Array([
    0,0,1, 0,0,1, 0,0,1, 0,0,1,
    0,0,-1,0,0,-1,0,0,-1,0,0,-1,
    0,1,0, 0,1,0, 0,1,0, 0,1,0,
    0,-1,0,0,-1,0,0,-1,0,0,-1,0,
    1,0,0, 1,0,0, 1,0,0, 1,0,0,
    -1,0,0,-1,0,0,-1,0,0,-1,0,0,
  ]);
  const indices = new Uint16Array([
    0,1,2,0,2,3, 4,5,6,4,6,7, 8,9,10,8,10,11,
    12,13,14,12,14,15, 16,17,18,16,18,19, 20,21,22,20,22,23,
  ]);
  return { positions, normals, indices };
}
const logo = addPrimitive(doc, 'MuseLogo', boxGeom(0.1, 0.05, 0.035), [0.39, 0.71, 0.96, 1.0], 0.2, 0.35);
logo.setTranslation([0, HB_Y - Math.sin(HB_TILT) * HB_R, HB_Z + Math.cos(HB_TILT) * HB_R]).setRotation([sinH, 0, 0, cosH]);
scene.addChild(logo);

// Electrodes as flat metallic discs, oriented along the surface normal.
const ELECTRODES = [
  { id: 'AF7',  pos: [-0.30, 0.60, 0.72], normal: normalize([-0.30, 0.45, 0.85]) },
  { id: 'AF8',  pos: [ 0.30, 0.60, 0.72], normal: normalize([ 0.30, 0.45, 0.85]) },
  { id: 'TP9',  pos: [-0.78, 0.05, -0.10], normal: normalize([-1.00, 0.10, -0.05]) },
  { id: 'TP10', pos: [ 0.78, 0.05, -0.10], normal: normalize([ 1.00, 0.10, -0.05]) },
];

for (const e of ELECTRODES) {
  const quat = quatFromUpToVec(e.normal);
  // Black mount ring (always-on).
  const mount = addPrimitive(doc, `${e.id}_Mount`, cylinder(0.085, 0.085, 0.025, 32), [0.08, 0.08, 0.1, 1.0], 0.7, 0.35);
  mount.setTranslation(e.pos).setRotation(quat);
  scene.addChild(mount);
  // Active sensor pad (this is the node bound by 3D Scenes Studio behaviors).
  const padOffset = [
    e.pos[0] + e.normal[0] * 0.025,
    e.pos[1] + e.normal[1] * 0.025,
    e.pos[2] + e.normal[2] * 0.025,
  ];
  const pad = addPrimitive(doc, e.id, cylinder(0.062, 0.072, 0.022, 32), [0.4, 0.7, 1.0, 1.0], 0.2, 0.4);
  pad.setTranslation(padOffset).setRotation(quat);
  scene.addChild(pad);
}

await new NodeIO().write(OUT, doc);
console.log(`Wrote ${OUT}`);
