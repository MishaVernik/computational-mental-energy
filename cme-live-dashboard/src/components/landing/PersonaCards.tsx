import React from 'react';

const personas = [
  {
    emoji: '\u{1F4BB}',
    title: 'Developers & Knowledge Workers',
    desc: 'See which tasks drain you fastest. Schedule deep work when your energy is high. Know when to stop before burnout hits.',
    features: ['Per-task CME rates in real time', 'Daily energy budget tracking', 'Adaptive scheduling suggestions'],
  },
  {
    emoji: '\u{1F9D8}',
    title: 'Meditators & Wellness Seekers',
    desc: 'Does your meditation actually recover mental energy? See it in real numbers. Track your brain\'s recovery over weeks and months.',
    features: ['Before/after energy comparison', 'Flow-state detection during practice', 'Recovery rate tracking over time'],
  },
  {
    emoji: '\u{1F465}',
    title: 'Team Leads & Managers',
    desc: 'Prevent team burnout with objective data. Balance cognitive load across sprints. Build evidence-based wellness programs.',
    features: ['Anonymized team dashboards', 'Burnout risk alerts', 'Activity pattern analysis'],
  },
];

export const PersonaCards: React.FC = () => (
  <section style={{ padding: '80px 0', background: '#0a0a18' }}>
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px' }}>
      <h2 style={{ fontSize: 36, fontWeight: 800, textAlign: 'center', marginBottom: 48, color: '#eee' }}>
        Built for How{' '}
        <span style={{
          background: 'linear-gradient(135deg, #64B5F6, #7C4DFF)',
          WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent',
        }}>You</span>{' '}Think
      </h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 24 }}>
        {personas.map(p => (
          <div key={p.title} style={{
            background: '#111827', borderRadius: 16, padding: 28,
            border: '1px solid #1f2937',
          }}>
            <div style={{ fontSize: 36, marginBottom: 12 }}>{p.emoji}</div>
            <h3 style={{ fontSize: 18, fontWeight: 700, color: '#eee', marginBottom: 10 }}>{p.title}</h3>
            <p style={{ color: '#999', fontSize: 14, lineHeight: 1.6, marginBottom: 16 }}>{p.desc}</p>
            <ul style={{ listStyle: 'none', padding: 0 }}>
              {p.features.map(f => (
                <li key={f} style={{
                  color: '#aaa', fontSize: 13, padding: '4px 0', paddingLeft: 16,
                  position: 'relative',
                }}>
                  <span style={{
                    position: 'absolute', left: 0, color: '#64B5F6',
                  }}>&bull;</span>
                  {f}
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
    </div>
  </section>
);
