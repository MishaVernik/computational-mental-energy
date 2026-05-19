import React from 'react';

const card: React.CSSProperties = {
  background: '#111827', borderRadius: 16, padding: 32,
  border: '1px solid #1f2937', textAlign: 'center',
};

const problems = [
  { icon: '\u{1F50B}', title: 'Burnout is invisible', desc: 'You push through "one more hour" without knowing your cognitive reserves are already depleted. By the time you feel it, the damage is done.' },
  { icon: '\u{1F4CA}', title: 'No objective metric exists', desc: 'Steps track your body. Calories track your food. But nothing tracks your brain\'s energy expenditure \u2014 until now.' },
  { icon: '\u{1F3AF}', title: 'Productivity is guesswork', desc: 'Is deep coding or meetings draining you more? Without data, you\'re scheduling your day blind.' },
];

export const ProblemSection: React.FC = () => (
  <section style={{ padding: '60px 0' }}>
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px' }}>
      <h2 style={{ fontSize: 36, fontWeight: 800, textAlign: 'center', marginBottom: 48, color: '#eee' }}>
        You Can Feel Burnout. But Can You{' '}
        <span style={{
          background: 'linear-gradient(135deg, #64B5F6, #7C4DFF)',
          WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent',
        }}>Measure</span>{' '}It?
      </h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 24 }}>
        {problems.map(p => (
          <div key={p.title} style={card}>
            <div style={{ fontSize: 40, marginBottom: 16 }}>{p.icon}</div>
            <h3 style={{ fontSize: 18, fontWeight: 700, color: '#eee', marginBottom: 12 }}>{p.title}</h3>
            <p style={{ color: '#999', fontSize: 14, lineHeight: 1.6 }}>{p.desc}</p>
          </div>
        ))}
      </div>
    </div>
  </section>
);
