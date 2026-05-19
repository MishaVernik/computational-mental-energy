import React, { useState, useEffect, useRef } from 'react';

const gradientText: React.CSSProperties = {
  background: 'linear-gradient(135deg, #64B5F6, #42A5F5, #7C4DFF)',
  WebkitBackgroundClip: 'text',
  WebkitTextFillColor: 'transparent',
};

const proofItems = [
  { target: 9.15, suffix: 'x', label: 'energy difference between coding and resting' },
  { target: 93.8, suffix: '%', label: 'flow-state detection accuracy' },
  { target: 24, suffix: ' min', label: 'to predict your full-day energy budget' },
];

const activities = [
  { name: 'Coding', rate: 339.9, pct: 100, color: '#ef4444' },
  { name: 'Math', rate: 316.7, pct: 93, color: '#f97316' },
  { name: 'Debugging', rate: 277.0, pct: 82, color: '#f59e0b' },
  { name: 'Tech Reading', rate: 190.5, pct: 56, color: '#3b82f6' },
  { name: 'Email', rate: 132.5, pct: 39, color: '#8b5cf6' },
  { name: 'Resting', rate: 37.1, pct: 11, color: '#10b981' },
];

function useCountUp(target: number, duration = 1500, delay = 300) {
  const [value, setValue] = useState(0);
  useEffect(() => {
    const timeout = setTimeout(() => {
      const start = performance.now();
      const tick = () => {
        const elapsed = performance.now() - start;
        const progress = Math.min(elapsed / duration, 1);
        const eased = 1 - Math.pow(1 - progress, 3);
        setValue(eased * target);
        if (progress < 1) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    }, delay);
    return () => clearTimeout(timeout);
  }, [target, duration, delay]);
  return value;
}

export const HeroSection: React.FC = () => {
  const [barsVisible, setBarsVisible] = useState(false);
  const [energyWidth, setEnergyWidth] = useState(0);
  const energyTarget = 62;

  useEffect(() => {
    const t1 = setTimeout(() => setBarsVisible(true), 600);
    const t2 = setTimeout(() => setEnergyWidth(energyTarget), 400);
    return () => { clearTimeout(t1); clearTimeout(t2); };
  }, []);

  const counters = proofItems.map((p, i) => useCountUp(p.target, 1200, 200 + i * 200));

  return (
    <section className="landing-hero" style={{ paddingTop: 100, paddingBottom: 60 }}>
      <div className="landing-hero-grid" style={{
        maxWidth: 1200, margin: '0 auto', padding: '0 24px',
        display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 60, alignItems: 'center',
      }}>
        <div>
          <h1 style={{ fontSize: 48, fontWeight: 900, lineHeight: 1.1, marginBottom: 20, color: '#eee' }}>
            Your Brain Has a Battery.<br />
            <span style={gradientText}>Start Measuring It.</span>
          </h1>
          <p className="hero-sub" style={{ fontSize: 18, color: '#999', lineHeight: 1.6, marginBottom: 32, maxWidth: 500 }}>
            CMEflow turns your EEG headband into a cognitive energy meter.
            See exactly how much mental energy each task costs – in real, comparable units
            called <strong style={{ color: '#ccc' }}>Verniks (Vn)</strong>.
          </p>
          <div className="landing-cta-row" style={{ display: 'flex', gap: 12, marginBottom: 40 }}>
            <a href="#waitlist" style={{
              padding: '14px 32px', fontSize: 15, fontWeight: 600,
              background: 'linear-gradient(135deg, #64B5F6, #42A5F5)',
              color: '#fff', borderRadius: 10, textDecoration: 'none',
            }}>Join the Waitlist</a>
            <a href="#how-it-works" style={{
              padding: '14px 32px', fontSize: 15, fontWeight: 600,
              background: 'transparent', color: '#64B5F6',
              border: '1px solid #64B5F6', borderRadius: 10, textDecoration: 'none',
            }}>See How It Works</a>
          </div>
          <div className="landing-proof" style={{ display: 'flex', gap: 32 }}>
            {proofItems.map((p, i) => (
              <div key={p.suffix + p.label}>
                <div style={{ fontSize: 28, fontWeight: 800, color: '#64B5F6', fontVariantNumeric: 'tabular-nums' }}>
                  {p.target % 1 === 0 ? Math.round(counters[i]) : counters[i].toFixed(1)}{p.suffix}
                </div>
                <div style={{ fontSize: 12, color: '#888', maxWidth: 120 }}>{p.label}</div>
              </div>
            ))}
          </div>
        </div>

        {/* Animated dashboard mock */}
        <div style={{
          background: '#111827', borderRadius: 16, border: '1px solid #1f2937',
          overflow: 'hidden', boxShadow: '0 20px 60px rgba(0,0,0,0.5)',
        }}>
          <div style={{
            padding: '10px 16px', background: '#0d1117',
            display: 'flex', alignItems: 'center', gap: 8,
          }}>
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: '#ef4444' }} />
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: '#f59e0b' }} />
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: '#10b981' }} />
            <span style={{ color: '#666', fontSize: 12, marginLeft: 8 }}>CMEflow Dashboard</span>
          </div>
          <div style={{ padding: 20 }}>
            {/* Animated energy bar */}
            <div style={{ marginBottom: 20 }}>
              <div style={{ color: '#888', fontSize: 12, marginBottom: 6 }}>Today's Energy</div>
              <div style={{
                height: 8, background: '#1f2937', borderRadius: 4, overflow: 'hidden',
              }}>
                <div style={{
                  height: '100%', width: `${energyWidth}%`, borderRadius: 4,
                  background: 'linear-gradient(90deg, #10b981, #64B5F6)',
                  transition: 'width 1.5s cubic-bezier(0.16, 1, 0.3, 1)',
                }} />
              </div>
              <div style={{
                display: 'flex', justifyContent: 'space-between',
                fontSize: 11, color: '#666', marginTop: 4,
              }}>
                <span>4,712K Vn used</span>
                <span>7,618K Vn budget</span>
              </div>
            </div>

            {/* Animated activity bars */}
            {activities.map((a, i) => (
              <div key={a.name} style={{
                display: 'grid', gridTemplateColumns: '90px 1fr 80px',
                alignItems: 'center', gap: 10, marginBottom: 8,
              }}>
                <span style={{ color: '#ccc', fontSize: 12 }}>{a.name}</span>
                <div style={{ height: 6, background: '#1f2937', borderRadius: 3, overflow: 'hidden' }}>
                  <div style={{
                    height: '100%',
                    width: barsVisible ? `${a.pct}%` : '0%',
                    background: a.color,
                    borderRadius: 3,
                    transition: `width 1s cubic-bezier(0.16, 1, 0.3, 1) ${i * 120}ms`,
                  }} />
                </div>
                <span style={{
                  color: '#888', fontSize: 11, textAlign: 'right',
                  opacity: barsVisible ? 1 : 0,
                  transition: `opacity 0.5s ease ${600 + i * 120}ms`,
                }}>
                  {a.rate.toFixed(1)} Vn/s
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
};
