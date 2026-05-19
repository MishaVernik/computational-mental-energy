import React from 'react';

const tiers = [
  {
    name: 'Free',
    price: '0',
    period: 'forever',
    annual: null,
    popular: false,
    features: ['3 sessions per week', 'Basic CME dashboard', '7-day history', 'Works with any Muse headband'],
  },
  {
    name: 'Personal',
    price: '9.99',
    period: 'per month',
    annual: '$79/year (save 34%)',
    popular: true,
    features: ['Unlimited sessions', 'Full activity tracking', 'Daily & weekly CME reports', 'Energy budget alerts', 'Unlimited history'],
  },
  {
    name: 'Pro',
    price: '19.99',
    period: 'per month',
    annual: '$149/year (save 38%)',
    popular: false,
    features: ['Everything in Personal', 'Quantum-enhanced flow detection', 'API access & data export', 'Personalized calibration', 'Adaptive scheduling AI'],
  },
];

export const PricingSection: React.FC = () => (
  <section id="pricing" style={{ padding: '80px 0' }}>
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px' }}>
      <h2 style={{ fontSize: 36, fontWeight: 800, textAlign: 'center', marginBottom: 8, color: '#eee' }}>
        Simple, Transparent Pricing
      </h2>
      <p style={{ textAlign: 'center', color: '#888', fontSize: 16, marginBottom: 48 }}>
        Start free. Upgrade when you're ready.
      </p>

      <div className="landing-pricing-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 24, marginBottom: 32 }}>
        {tiers.map(t => (
          <div key={t.name} style={{
            background: '#111827', borderRadius: 16, padding: 32,
            border: t.popular ? '2px solid #64B5F6' : '1px solid #1f2937',
            position: 'relative',
          }}>
            {t.popular && (
              <div style={{
                position: 'absolute', top: -12, left: '50%', transform: 'translateX(-50%)',
                background: 'linear-gradient(135deg, #64B5F6, #42A5F5)',
                color: '#fff', fontSize: 11, fontWeight: 700, padding: '4px 16px',
                borderRadius: 20,
              }}>Most Popular</div>
            )}
            <div style={{ color: '#888', fontSize: 14, fontWeight: 600, marginBottom: 8 }}>{t.name}</div>
            <div style={{ marginBottom: 4 }}>
              <span style={{ color: '#666', fontSize: 18 }}>$</span>
              <span style={{ color: '#eee', fontSize: 48, fontWeight: 800 }}>{t.price.split('.')[0]}</span>
              {t.price.includes('.') && (
                <span style={{ color: '#aaa', fontSize: 20 }}>.{t.price.split('.')[1]}</span>
              )}
            </div>
            <div style={{ color: '#888', fontSize: 13, marginBottom: 4 }}>{t.period}</div>
            {t.annual && <div style={{ color: '#666', fontSize: 12, marginBottom: 16 }}>{t.annual}</div>}
            {!t.annual && <div style={{ marginBottom: 16 }} />}
            <ul style={{ listStyle: 'none', padding: 0, marginBottom: 24 }}>
              {t.features.map(f => (
                <li key={f} style={{
                  color: '#ccc', fontSize: 13, padding: '6px 0', paddingLeft: 20,
                  position: 'relative',
                }}>
                  <span style={{ position: 'absolute', left: 0, color: '#10b981' }}>{'\u2713'}</span>
                  {f}
                </li>
              ))}
            </ul>
            <a href="#waitlist" style={{
              display: 'block', textAlign: 'center', padding: '12px 0',
              borderRadius: 8, fontSize: 14, fontWeight: 600, textDecoration: 'none',
              ...(t.popular
                ? { background: 'linear-gradient(135deg, #64B5F6, #42A5F5)', color: '#fff' }
                : { background: 'transparent', color: '#64B5F6', border: '1px solid #64B5F6' }),
            }}>Join Waitlist</a>
          </div>
        ))}
      </div>

      <p style={{ textAlign: 'center', color: '#888', fontSize: 14 }}>
        <strong>Teams?</strong> $49/month for up to 10 users – anonymized dashboards, burnout alerts, SSO.{' '}
        <a href="#waitlist" style={{ color: '#64B5F6' }}>Contact us</a>.
      </p>
    </div>
  </section>
);
