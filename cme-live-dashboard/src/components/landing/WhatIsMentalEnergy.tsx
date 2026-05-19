import React from 'react';

const gradientText: React.CSSProperties = {
  background: 'linear-gradient(135deg, #64B5F6, #7C4DFF)',
  WebkitBackgroundClip: 'text',
  WebkitTextFillColor: 'transparent',
};

const comparison = [
  { left: 'Food', right: 'Thinking', leftUnit: 'calories', rightUnit: 'Verniks (Vn)' },
  { left: 'Daily intake', right: 'Daily brain budget', leftUnit: '~2,000 kcal', rightUnit: '~7,600K Vn' },
  { left: 'Running burns more than walking', right: 'Coding burns more than browsing', leftUnit: '600 vs 300 kcal/hr', rightUnit: '340 vs 37 Vn/s' },
];

const activities = [
  { name: 'Resting', rate: 37, max: 340, color: '#10b981', label: 'With closed eyes' },
  { name: 'Browsing', rate: 113, max: 340, color: '#3b82f6', label: 'Social media, news' },
  { name: 'Email', rate: 133, max: 340, color: '#8b5cf6', label: 'Reading and writing messages' },
  { name: 'Reading', rate: 149, max: 340, color: '#6366f1', label: 'Novel or articles' },
  { name: 'Technical reading', rate: 191, max: 340, color: '#f59e0b', label: 'Documentation, papers' },
  { name: 'Debugging', rate: 277, max: 340, color: '#f97316', label: 'Tracing bugs, reading logs' },
  { name: 'Math', rate: 317, max: 340, color: '#ef4444', label: 'Problem solving, algorithms' },
  { name: 'Coding', rate: 340, max: 340, color: '#dc2626', label: 'Writing new code' },
];

export const WhatIsMentalEnergy: React.FC = () => (
  <section id="mental-energy" style={{ padding: '80px 0' }}>
    <div style={{ maxWidth: 1100, margin: '0 auto', padding: '0 24px' }}>

      <h2 style={{ fontSize: 36, fontWeight: 800, textAlign: 'center', marginBottom: 16, color: '#eee' }}>
        What is <span style={gradientText}>Mental Energy</span>?
      </h2>

      <p style={{
        maxWidth: 700, margin: '0 auto 48px', textAlign: 'center',
        fontSize: 17, color: '#999', lineHeight: 1.7,
      }}>
        Your body burns calories. Your brain burns something too &mdash; but nobody measures it.
        Every thought, every line of code, every email drains a finite cognitive resource.
        Psychologists call it &ldquo;cognitive load.&rdquo; Neuroscientists see it in your brainwaves.
        <br />
        <strong style={{ color: '#ccc' }}> We gave it a number.</strong>
      </p>

      {/* Calorie vs Vernik comparison */}
      <div className="landing-grid-2" style={{
        display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 48,
        marginBottom: 56, alignItems: 'start',
      }}>
        <div>
          <h3 style={{ fontSize: 22, fontWeight: 700, color: '#eee', marginBottom: 20 }}>
            Calories measure food energy.<br />
            <span style={gradientText}>Verniks measure brain energy.</span>
          </h3>

          <p style={{ color: '#999', fontSize: 15, lineHeight: 1.7, marginBottom: 24 }}>
            Just like your body has a daily calorie budget, your brain has a daily
            <strong style={{ color: '#ccc' }}> Vernik budget</strong>. Different activities
            drain it at different rates. A day of intense coding can cost 9 times more mental
            energy than a day of light browsing &mdash; and now you can see exactly where it goes.
          </p>

          <div style={{
            background: '#111827', borderRadius: 12, padding: 20,
            border: '1px solid #1f2937',
          }}>
            {comparison.map((c, i) => (
              <div key={i} style={{
                display: 'grid', gridTemplateColumns: '1fr 24px 1fr',
                gap: 8, alignItems: 'center',
                padding: '10px 0',
                borderBottom: i < comparison.length - 1 ? '1px solid #1f2937' : 'none',
              }}>
                <div>
                  <div style={{ color: '#ccc', fontSize: 14, fontWeight: 600 }}>{c.left}</div>
                  <div style={{ color: '#666', fontSize: 12 }}>{c.leftUnit}</div>
                </div>
                <div style={{ color: '#444', textAlign: 'center', fontSize: 16 }}>=</div>
                <div>
                  <div style={{ color: '#64B5F6', fontSize: 14, fontWeight: 600 }}>{c.right}</div>
                  <div style={{ color: '#666', fontSize: 12 }}>{c.rightUnit}</div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Activity rates chart */}
        <div style={{
          background: '#111827', borderRadius: 16, padding: 24,
          border: '1px solid #1f2937',
        }}>
          <div style={{ color: '#888', fontSize: 13, marginBottom: 4 }}>
            Energy drain by activity
          </div>
          <div style={{ color: '#666', fontSize: 11, marginBottom: 16 }}>
            Measured from real EEG recordings (Verniks per second)
          </div>
          {activities.map(a => (
            <div key={a.name} style={{ marginBottom: 10 }}>
              <div style={{
                display: 'flex', justifyContent: 'space-between', marginBottom: 3,
              }}>
                <span style={{ color: '#ccc', fontSize: 13 }}>
                  {a.name}
                  <span style={{ color: '#666', fontSize: 11, marginLeft: 8 }}>{a.label}</span>
                </span>
                <span style={{ color: a.color, fontSize: 13, fontWeight: 700 }}>{a.rate} Vn/s</span>
              </div>
              <div style={{
                height: 6, background: '#1f2937', borderRadius: 3, overflow: 'hidden',
              }}>
                <div style={{
                  height: '100%', width: `${(a.rate / a.max) * 100}%`,
                  background: a.color, borderRadius: 3,
                }} />
              </div>
            </div>
          ))}
          <div style={{
            marginTop: 16, padding: '10px 12px', background: '#0d0d1a',
            borderRadius: 8, textAlign: 'center',
          }}>
            <span style={{ color: '#64B5F6', fontSize: 24, fontWeight: 800 }}>9.15x</span>
            <span style={{ color: '#888', fontSize: 13, marginLeft: 8 }}>
              difference between coding and resting
            </span>
          </div>
        </div>
      </div>
    </div>
  </section>
);
