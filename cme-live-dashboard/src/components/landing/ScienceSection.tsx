import React from 'react';

const cards = [
  { icon: '\u{1F9EC}', title: 'Quantum-Enhanced AI', desc: 'A 4-qubit variational quantum classifier detects flow states with 0.800 AUROC using only 24 trainable parameters. The hybrid quantum-classical mode reaches 0.967 AUROC.' },
  { icon: '\u{1F4DD}', title: 'Patented CME Formula', desc: 'CME = \u03BA \u00B7 E_band \u00B7 \u0394 \u00B7 g(c, p) \u2014 a window-level function of spectral energy, task complexity, and flow probability, measured in Vernik units.' },
  { icon: '\u2699\uFE0F', title: 'Real Quantum Hardware', desc: 'Validated on IBM Marrakesh (156-qubit Heron r2 processor). Simulator-to-hardware correlation: r = 0.940, MAE = 0.041.' },
  { icon: '\u{1F4C8}', title: 'Real EEG Data', desc: '288 windows across 8 cognitive activities recorded with Muse Athena. A 9.15x rate difference between coding and resting \u2014 measured, not simulated.' },
];

export const ScienceSection: React.FC = () => (
  <section id="science" style={{ padding: '80px 0' }}>
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px' }}>
      <h2 style={{ fontSize: 36, fontWeight: 800, textAlign: 'center', marginBottom: 48, color: '#eee' }}>
        Built on{' '}
        <span style={{
          background: 'linear-gradient(135deg, #64B5F6, #7C4DFF)',
          WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent',
        }}>Real Science</span>
      </h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 24 }}>
        {cards.map(c => (
          <div key={c.title} style={{
            background: '#111827', borderRadius: 16, padding: 28,
            border: '1px solid #1f2937',
          }}>
            <div style={{ fontSize: 32, marginBottom: 12 }}>{c.icon}</div>
            <h3 style={{ fontSize: 18, fontWeight: 700, color: '#eee', marginBottom: 10 }}>{c.title}</h3>
            <p style={{ color: '#999', fontSize: 14, lineHeight: 1.6 }}>{c.desc}</p>
          </div>
        ))}
      </div>
    </div>
  </section>
);
