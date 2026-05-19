import React from 'react';

const steps = [
  { num: '1', title: 'Wear your EEG headband', desc: 'Works with Muse 2, Muse S, and Muse Athena. Put it on and connect via Bluetooth.' },
  { num: '2', title: 'Work, meditate, or study normally', desc: 'CMEflow streams your brainwaves in real time \u2014 5-second windows, 5 frequency bands, 4 channels.' },
  { num: '3', title: 'See your energy in Verniks', desc: 'Every task gets a CME rate in Vn/s. Your daily budget accumulates like calories \u2014 see exactly where it went.' },
];

const vernikExamples = [
  { val: '37 Vn/s', act: 'Resting' },
  { val: '190 Vn/s', act: 'Reading' },
  { val: '340 Vn/s', act: 'Coding' },
];

export const HowItWorks: React.FC = () => (
  <section id="how-it-works" style={{ padding: '80px 0', background: '#0a0a18' }}>
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px' }}>
      <h2 style={{ fontSize: 36, fontWeight: 800, textAlign: 'center', marginBottom: 48, color: '#eee' }}>
        How CMEflow Works
      </h2>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 24, marginBottom: 48 }}>
        {steps.map(s => (
          <div key={s.num} style={{
            background: '#111827', borderRadius: 16, padding: 32,
            border: '1px solid #1f2937', textAlign: 'center',
          }}>
            <div style={{
              width: 48, height: 48, borderRadius: '50%', margin: '0 auto 16px',
              background: 'linear-gradient(135deg, #64B5F6, #42A5F5)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: 20, fontWeight: 800, color: '#fff',
            }}>{s.num}</div>
            <h3 style={{ fontSize: 18, fontWeight: 700, color: '#eee', marginBottom: 12 }}>{s.title}</h3>
            <p style={{ color: '#999', fontSize: 14, lineHeight: 1.6 }}>{s.desc}</p>
          </div>
        ))}
      </div>

      <div style={{
        background: '#111827', borderRadius: 16, padding: 32,
        border: '1px solid #1f2937', maxWidth: 700, margin: '0 auto',
      }}>
        <h3 style={{ fontSize: 20, fontWeight: 700, color: '#eee', marginBottom: 12, textAlign: 'center' }}>
          What is a Vernik?
        </h3>
        <p style={{ color: '#999', fontSize: 14, lineHeight: 1.7, textAlign: 'center', marginBottom: 24 }}>
          The <strong style={{ color: '#ccc' }}>Vernik (Vn)</strong> is a patented unit for measuring
          computational mental energy. 1 Vn = 1 &mu;V&sup2;&middot;s &mdash; it's the EEG spectral energy
          integrated over time, modulated by task complexity and your flow state.
          Think of it as <em>calories for your brain</em>.
        </p>
        <div style={{
          display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 24,
        }}>
          {vernikExamples.map((v, i) => (
            <React.Fragment key={v.act}>
              <div style={{ textAlign: 'center' }}>
                <div style={{ fontSize: 24, fontWeight: 800, color: '#64B5F6' }}>{v.val}</div>
                <div style={{ fontSize: 12, color: '#888' }}>{v.act}</div>
              </div>
              {i < vernikExamples.length - 1 && (
                <span style={{ color: '#444', fontSize: 24 }}>&rarr;</span>
              )}
            </React.Fragment>
          ))}
        </div>
      </div>
    </div>
  </section>
);
